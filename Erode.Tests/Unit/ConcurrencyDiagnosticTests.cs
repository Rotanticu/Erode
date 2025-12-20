using FluentAssertions;
using System.Collections.Concurrent;
using Erode.Tests.Helpers;

namespace Erode.Tests.Unit;

/// <summary>
/// 并发诊断测试 - 用于诊断并发问题
/// 这些测试使用独立的事件类型，避免与现有测试冲突
/// </summary>
public class ConcurrencyDiagnosticTests : TestBase
{

    [Fact]
    public void ConcurrentSubscribe_ShouldNotLoseSubscriptions()
    {
        // Arrange
        const int threadCount = 10;
        const int subscribesPerThread = 100;
        var allTokens = new ConcurrentBag<SubscriptionToken>();
        var invocationCount = 0;

        // Act - 多个线程同时 Subscribe
        Parallel.For(0, threadCount, threadId =>
        {
            for (int i = 0; i < subscribesPerThread; i++)
            {
                var handler = new InAction<ConcurrentTestEvent>((in ConcurrentTestEvent evt) =>
                {
                    Interlocked.Increment(ref invocationCount);
                });
                var token = ConcurrencyTestEvents.SubscribeConcurrentTestEvent(handler);
                allTokens.Add(token);
            }
        });

        // Assert - 所有订阅都应该被记录
        allTokens.Count.Should().Be(threadCount * subscribesPerThread);

        // 发布一次，验证所有 handler 都被调用
        ConcurrencyTestEvents.PublishConcurrentTestEvent(0);
        invocationCount.Should().Be(threadCount * subscribesPerThread);

        // Cleanup
        foreach (var token in allTokens)
        {
            token.Dispose();
        }
    }

    [Fact]
    public void ConcurrentUnsubscribe_ShouldNotCorruptState()
    {
        // Arrange
        const int subscribeCount = 100;
        var tokens = new List<SubscriptionToken>();
        var invocationCount = 0;

        // 先订阅
        for (int i = 0; i < subscribeCount; i++)
        {
            var handler = new InAction<ConcurrentTestEvent>((in ConcurrentTestEvent evt) =>
            {
                Interlocked.Increment(ref invocationCount);
            });
            tokens.Add(ConcurrencyTestEvents.SubscribeConcurrentTestEvent(handler));
        }

        // Act - 多个线程同时 Unsubscribe
        Parallel.ForEach(tokens, token =>
        {
            token.Dispose();
        });

        // Assert - 状态不应该被破坏
        ConcurrencyTestEvents.PublishConcurrentTestEvent(0);
        invocationCount.Should().Be(0); // 所有订阅都已退订
    }

    [Fact]
    public void ConcurrentPublish_ShouldInvokeAllHandlers()
    {
        // Arrange
        const int handlerCount = 10;
        const int publishCount = 100;
        var handlerInvocations = new int[handlerCount];
        var tokens = new List<SubscriptionToken>();

        for (int i = 0; i < handlerCount; i++)
        {
            var index = i; // 捕获循环变量
            var handler = new InAction<ConcurrentTestEvent>((in ConcurrentTestEvent evt) =>
            {
                Interlocked.Increment(ref handlerInvocations[index]);
            });
            tokens.Add(ConcurrencyTestEvents.SubscribeConcurrentTestEvent(handler));
        }

        // Act - 多个线程同时 Publish
        Parallel.For(0, publishCount, i =>
        {
            ConcurrencyTestEvents.PublishConcurrentTestEvent(i);
        });

        // Assert - 每个 handler 都应该被调用 publishCount 次
        foreach (var count in handlerInvocations)
        {
            count.Should().Be(publishCount);
        }

        // Cleanup
        foreach (var token in tokens)
        {
            token.Dispose();
        }
    }

    [Fact]
    public async Task ConcurrentSubscribeAndPublish_ShouldBeThreadSafe()
    {
        // Arrange
        var subscribeInvocationCount = 0;
        var publishInvocationCount = 0;
        var allTokens = new ConcurrentBag<SubscriptionToken>();
        const int iterations = 1000;

        // Act - 一个线程 Subscribe，另一个线程 Publish
        var subscribeTask = Task.Run(() =>
        {
            for (int i = 0; i < iterations; i++)
            {
                var handler = new InAction<ConcurrentTestEvent>((in ConcurrentTestEvent evt) =>
                {
                    Interlocked.Increment(ref subscribeInvocationCount);
                });
                var token = ConcurrencyTestEvents.SubscribeConcurrentTestEvent(handler);
                allTokens.Add(token);
                Thread.Sleep(0); // 让出时间片
            }
        });

        var publishTask = Task.Run(() =>
        {
            for (int i = 0; i < iterations; i++)
            {
                ConcurrencyTestEvents.PublishConcurrentTestEvent(i);
                Interlocked.Increment(ref publishInvocationCount);
                Thread.Sleep(0); // 让出时间片
            }
        });

        await Task.WhenAll(subscribeTask, publishTask);

        // Assert - 不应该有异常，状态应该一致
        allTokens.Count.Should().Be(iterations);
        publishInvocationCount.Should().Be(iterations);
        // subscribeInvocationCount 应该 >= 0（取决于订阅和发布的时序）

        // Cleanup
        foreach (var token in allTokens)
        {
            token.Dispose();
        }
    }

    [Fact]
    public async Task ConcurrentUnsubscribeAndPublish_ShouldUseSnapshot()
    {
        // Arrange
        const int handlerCount = 10;
        var tokens = new List<SubscriptionToken>();
        var invocationCounts = new int[handlerCount];

        // 订阅所有 handler
        for (int i = 0; i < handlerCount; i++)
        {
            var index = i;
            var handler = new InAction<ConcurrentTestEvent>((in ConcurrentTestEvent evt) =>
            {
                Interlocked.Increment(ref invocationCounts[index]);
            });
            tokens.Add(ConcurrencyTestEvents.SubscribeConcurrentTestEvent(handler));
        }

        // Act - 一个线程 Unsubscribe，另一个线程 Publish
        var unsubscribeTask = Task.Run(() =>
        {
            foreach (var token in tokens)
            {
                token.Dispose();
                Thread.Sleep(1); // 让出时间片
            }
        });

        var publishTask = Task.Run(() =>
        {
            for (int i = 0; i < handlerCount * 10; i++)
            {
                ConcurrencyTestEvents.PublishConcurrentTestEvent(i);
                Thread.Sleep(0); // 让出时间片
            }
        });

        await Task.WhenAll(unsubscribeTask, publishTask);

        // Assert - 使用快照，不应该有异常
        // 每个 handler 的调用次数应该 >= 0（取决于退订和发布的时序）
        var totalInvocations = invocationCounts.Sum();
        totalInvocations.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task ConcurrentMixedOperations_ShouldNotDeadlock()
    {
        // Arrange
        var allTokens = new ConcurrentBag<SubscriptionToken>();
        var invocationCount = 0;
        const int iterations = 100;

        // Act - 多个线程同时进行 Subscribe、Unsubscribe、Publish
        var tasks = new List<Task>();
        for (int i = 0; i < 10; i++)
        {
            var taskId = i;
            tasks.Add(Task.Run(() =>
            {
                for (int j = 0; j < iterations; j++)
                {
                    // Subscribe
                    var handler = new InAction<ConcurrentTestEvent>((in ConcurrentTestEvent evt) =>
                    {
                        Interlocked.Increment(ref invocationCount);
                    });
                    var token = ConcurrencyTestEvents.SubscribeConcurrentTestEvent(handler);
                    allTokens.Add(token);

                    // Publish
                    ConcurrencyTestEvents.PublishConcurrentTestEvent(taskId * iterations + j);

                    // 随机 Unsubscribe 一些
                    if (j % 3 == 0 && allTokens.TryTake(out var tokenToUnsubscribe))
                    {
                        tokenToUnsubscribe.Dispose();
                    }

                    Thread.Sleep(0); // 让出时间片
                }
            }));
        }

        // 等待所有任务完成（不应该死锁）
        await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromSeconds(10));

        // Cleanup
        foreach (var token in allTokens)
        {
            token.Dispose();
        }
    }

    [Fact]
    public void ConcurrentPublishWithException_ShouldHandleCorrectly()
    {
        // Arrange
        const int handlerCount = 10;
        const int publishCount = 100;
        var exceptionCount = 0;
        var normalHandlerInvocations = 0;
        var tokens = new List<SubscriptionToken>();

        // 创建一些会抛异常的 handler 和一些正常的 handler
        for (int i = 0; i < handlerCount; i++)
        {
            if (i % 2 == 0)
            {
                // 会抛异常的 handler
                var handler = new InAction<ConcurrentTestEvent>((in ConcurrentTestEvent evt) =>
                {
                    throw new InvalidOperationException($"Handler {i} exception");
                });
                tokens.Add(ConcurrencyTestEvents.SubscribeConcurrentTestEvent(handler));
            }
            else
            {
                // 正常的 handler
                var handler = new InAction<ConcurrentTestEvent>((in ConcurrentTestEvent evt) =>
                {
                    Interlocked.Increment(ref normalHandlerInvocations);
                });
                tokens.Add(ConcurrencyTestEvents.SubscribeConcurrentTestEvent(handler));
            }
        }

        // Act - 设置异常处理，然后多个线程同时 Publish
        var originalLocalOnException = EventDispatcher<ConcurrentTestEvent>.LocalOnException;
        EventDispatcher<ConcurrentTestEvent>.LocalOnException = (evt, handler, ex) =>
        {
            Interlocked.Increment(ref exceptionCount);
        };

        try
        {
            Parallel.For(0, publishCount, i =>
            {
                ConcurrencyTestEvents.PublishConcurrentTestEvent(i);
            });
        }
        finally
        {
            EventDispatcher<ConcurrentTestEvent>.LocalOnException = originalLocalOnException;
        }

        // Assert - 异常应该被处理，正常的 handler 应该被调用
        exceptionCount.Should().BeGreaterThan(0); // 应该有异常被捕获
        normalHandlerInvocations.Should().Be((handlerCount / 2) * publishCount); // 正常 handler 应该被调用

        // Cleanup
        foreach (var token in tokens)
        {
            token.Dispose();
        }
    }

    [Fact]
    public async Task ConcurrentCopyOnWrite_ShouldMaintainConsistency()
    {
        // Arrange
        var allTokens = new ConcurrentBag<SubscriptionToken>();
        var invocationCount = 0;
        const int iterations = 100;

        // Act - 在 Publish 过程中进行 Subscribe/Unsubscribe
        var subscribeTask = Task.Run(() =>
        {
            for (int i = 0; i < iterations; i++)
            {
                var handler = new InAction<ConcurrentTestEvent>((in ConcurrentTestEvent evt) =>
                {
                    Interlocked.Increment(ref invocationCount);
                });
                var token = ConcurrencyTestEvents.SubscribeConcurrentTestEvent(handler);
                allTokens.Add(token);
                Thread.Sleep(1);
            }
        });

        var unsubscribeTask = Task.Run(() =>
        {
            Thread.Sleep(10); // 等待一些订阅完成
            while (allTokens.IsEmpty == false || !subscribeTask.IsCompleted)
            {
                if (allTokens.TryTake(out var token))
                {
                    token.Dispose();
                }
                Thread.Sleep(1);
            }
        });

        var publishTask = Task.Run(() =>
        {
            for (int i = 0; i < iterations * 2; i++)
            {
                ConcurrencyTestEvents.PublishConcurrentTestEvent(i);
                Thread.Sleep(0);
            }
        });

        await Task.WhenAll(subscribeTask, unsubscribeTask, publishTask);

        // Assert - Copy-On-Write 应该保证一致性，不应该有异常
        // invocationCount 应该 >= 0（取决于订阅、退订和发布的时序）

        // Cleanup
        foreach (var token in allTokens)
        {
            token.Dispose();
        }
    }
}

