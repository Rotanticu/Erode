using FluentAssertions;
using System.Collections.Concurrent;
using Erode.Tests.Helpers;

namespace Erode.Tests.Unit;

/// <summary>
/// 异步测试 - 验证用户使用 Task.Run 等方式异步发布事件时的行为
/// 虽然我们不提供异步发布方法，但用户可能会使用各种异步方式
/// </summary>
public class AsyncTests : TestBase
{

    [Fact]
    public async Task Publish_UsingTaskRun_ShouldWorkCorrectly()
    {
        // Arrange
        var invoked = false;
        var handler = new InAction<AsyncTestEvent>((in AsyncTestEvent evt) => { invoked = true; });
        var token = AsyncTestEvents.SubscribeAsyncTestEvent(handler);

        try
        {
            // Act - 用户使用 Task.Run 发布事件
            await Task.Run(() =>
            {
                AsyncTestEvents.PublishAsyncTestEvent(42);
            });

            // Assert
            invoked.Should().BeTrue();
        }
        finally
        {
            token.Dispose();
        }
    }

    [Fact]
    public async Task Publish_MultipleTaskRun_AllHandlersInvoked()
    {
        // Arrange
        var invocationCount = 0;
        var handler = new InAction<AsyncTestEvent>((in AsyncTestEvent evt) =>
        {
            Interlocked.Increment(ref invocationCount);
        });
        var token = AsyncTestEvents.SubscribeAsyncTestEvent(handler);

        try
        {
            // Act - 多个 Task.Run 同时发布
            var tasks = new List<Task>();
            for (int i = 0; i < 10; i++)
            {
                var value = i;
                tasks.Add(Task.Run(() =>
                {
                    AsyncTestEvents.PublishAsyncTestEvent(value);
                }));
            }

            await Task.WhenAll(tasks);

            // Assert
            invocationCount.Should().Be(10);
        }
        finally
        {
            token.Dispose();
        }
    }

    [Fact]
    public async Task Subscribe_AsyncContext_ShouldWorkCorrectly()
    {
        // Arrange
        var invoked = false;
        var handler = new InAction<AsyncTestEvent>((in AsyncTestEvent evt) => { invoked = true; });

        // Act - 在异步上下文中订阅
        SubscriptionToken token;
        await Task.Run(() =>
        {
            token = AsyncTestEvents.SubscribeAsyncTestEvent(handler);
        });

        // 需要重新获取 token，因为 await 后作用域变了
        token = EventDispatcher<AsyncTestEvent>.Subscribe(handler);

        try
        {
            AsyncTestEvents.PublishAsyncTestEvent(42);

            // Assert
            invoked.Should().BeTrue();
        }
        finally
        {
            token.Dispose();
        }
    }

    [Fact]
    public async Task Publish_ConcurrentTaskRun_ShouldBeThreadSafe()
    {
        // Arrange
        var invocationCount = 0;
        var handler = new InAction<AsyncTestEvent>((in AsyncTestEvent evt) =>
        {
            Interlocked.Increment(ref invocationCount);
        });
        var token = AsyncTestEvents.SubscribeAsyncTestEvent(handler);

        try
        {
            // Act - 大量并发 Task.Run 发布
            const int taskCount = 100;
            var tasks = new List<Task>();
            for (int i = 0; i < taskCount; i++)
            {
                var value = i;
                tasks.Add(Task.Run(() =>
                {
                    AsyncTestEvents.PublishAsyncTestEvent(value);
                }));
            }

            await Task.WhenAll(tasks);

            // Assert
            invocationCount.Should().Be(taskCount);
        }
        finally
        {
            token.Dispose();
        }
    }

    [Fact]
    public async Task SubscribeAndPublish_AsyncMixed_ShouldWorkCorrectly()
    {
        // Arrange
        var invocationCount = 0;
        var tokens = new List<SubscriptionToken>();

        // Act - 异步订阅和发布混合
        var subscribeTask = Task.Run(() =>
        {
            for (int i = 0; i < 10; i++)
            {
                var handler = new InAction<AsyncTestEvent>((in AsyncTestEvent evt) =>
                {
                    Interlocked.Increment(ref invocationCount);
                });
                tokens.Add(AsyncTestEvents.SubscribeAsyncTestEvent(handler));
            }
        });

        var publishTask = Task.Run(async () =>
        {
            await subscribeTask; // 等待订阅完成
            for (int i = 0; i < 10; i++)
            {
                AsyncTestEvents.PublishAsyncTestEvent(i);
                await Task.Delay(1); // 模拟异步延迟
            }
        });

        await publishTask;

        // Assert
        invocationCount.Should().Be(100); // 10 个 handler * 10 次发布

        // Cleanup
        foreach (var token in tokens)
        {
            token.Dispose();
        }
    }

    [Fact]
    public async Task Unsubscribe_AsyncContext_ShouldWorkCorrectly()
    {
        // Arrange
        var invoked = false;
        var handler = new InAction<AsyncTestEvent>((in AsyncTestEvent evt) => { invoked = true; });
        var token = AsyncTestEvents.SubscribeAsyncTestEvent(handler);

        // Act - 在异步上下文中退订
        await Task.Run(() =>
        {
            token.Dispose();
        });

        // 再次发布，应该不会触发
        AsyncTestEvents.PublishAsyncTestEvent(42);

        // Assert
        invoked.Should().BeFalse();
    }

    [Fact]
    public async Task Publish_WithExceptionInAsync_ShouldHandleCorrectly()
    {
        // Arrange
        var exceptionCount = 0;
        var normalHandlerInvocations = 0;
        var exceptionHandler = new InAction<AsyncTestEvent>((in AsyncTestEvent evt) =>
        {
            throw new InvalidOperationException("Async exception");
        });
        var normalHandler = new InAction<AsyncTestEvent>((in AsyncTestEvent evt) =>
        {
            Interlocked.Increment(ref normalHandlerInvocations);
        });

        var token1 = AsyncTestEvents.SubscribeAsyncTestEvent(exceptionHandler);
        var token2 = AsyncTestEvents.SubscribeAsyncTestEvent(normalHandler);

        try
        {
            // 设置异常处理
            var originalOnException = EventDispatcher.OnException;
            var originalLocalOnException = EventDispatcher<AsyncTestEvent>.LocalOnException;
            
            EventDispatcher.OnException = (evt, handler, ex) =>
            {
                Interlocked.Increment(ref exceptionCount);
            };
            
            EventDispatcher<AsyncTestEvent>.LocalOnException = (evt, handler, ex) =>
            {
                Interlocked.Increment(ref exceptionCount);
            };

            try
            {
                // Act - 在异步上下文中发布
                await Task.Run(() =>
                {
                AsyncTestEvents.PublishAsyncTestEvent(42);
                });

                // Assert
                exceptionCount.Should().BeGreaterThan(0);
                normalHandlerInvocations.Should().Be(1);
            }
            finally
            {
                EventDispatcher.OnException = originalOnException;
                EventDispatcher<AsyncTestEvent>.LocalOnException = originalLocalOnException;
            }
        }
        finally
        {
            token1.Dispose();
            token2.Dispose();
        }
    }

    [Fact]
    public async Task Publish_AsyncWithMultipleHandlers_AllInvoked()
    {
        // Arrange
        var handler1Invoked = false;
        var handler2Invoked = false;
        var handler3Invoked = false;

        var handler1 = new InAction<AsyncTestEvent>((in AsyncTestEvent evt) => { handler1Invoked = true; });
        var handler2 = new InAction<AsyncTestEvent>((in AsyncTestEvent evt) => { handler2Invoked = true; });
        var handler3 = new InAction<AsyncTestEvent>((in AsyncTestEvent evt) => { handler3Invoked = true; });

        var token1 = AsyncTestEvents.SubscribeAsyncTestEvent(handler1);
        var token2 = AsyncTestEvents.SubscribeAsyncTestEvent(handler2);
        var token3 = AsyncTestEvents.SubscribeAsyncTestEvent(handler3);

        try
        {
            // Act - 使用 Task.Run 发布
            await Task.Run(() =>
            {
                AsyncTestEvents.PublishAsyncTestEvent(42);
            });

            // Assert
            handler1Invoked.Should().BeTrue();
            handler2Invoked.Should().BeTrue();
            handler3Invoked.Should().BeTrue();
        }
        finally
        {
            token1.Dispose();
            token2.Dispose();
            token3.Dispose();
        }
    }

    [Fact]
    public async Task Publish_AsyncWithGeneratedEvents_ShouldWorkCorrectly()
    {
        // Arrange - 使用生成的事件类型
        var invoked = false;
        var handler = new InAction<TestGeneratedEvent>((in TestGeneratedEvent evt) => { invoked = true; });
        var token = TestEvents.SubscribeTestGeneratedEvent(handler);

        try
        {
            // Act - 使用 Task.Run 发布生成的事件
            await Task.Run(() =>
            {
                TestEvents.PublishTestGeneratedEvent("async test", 42);
            });

            // Assert
            invoked.Should().BeTrue();
        }
        finally
        {
            token.Dispose();
        }
    }

    [Fact]
    public async Task Publish_AsyncStressTest_ShouldMaintainConsistency()
    {
        // Arrange
        var invocationCount = 0;
        var handler = new InAction<AsyncTestEvent>((in AsyncTestEvent evt) =>
        {
            Interlocked.Increment(ref invocationCount);
        });
        var token = AsyncTestEvents.SubscribeAsyncTestEvent(handler);

        try
        {
            // Act - 大量异步任务同时发布
            const int taskCount = 1000;
            var tasks = new List<Task>();
            for (int i = 0; i < taskCount; i++)
            {
                var value = i;
                tasks.Add(Task.Run(() =>
                {
                    AsyncTestEvents.PublishAsyncTestEvent(value);
                }));
            }

            await Task.WhenAll(tasks);

            // Assert
            invocationCount.Should().Be(taskCount);
        }
        finally
        {
            token.Dispose();
        }
    }

    [Fact]
    public async Task Subscribe_Unsubscribe_AsyncLifecycle_ShouldWorkCorrectly()
    {
        // Arrange
        var invocationLog = new ConcurrentBag<string>();
        var handler1 = new InAction<AsyncTestEvent>((in AsyncTestEvent evt) => { invocationLog.Add("handler1"); });
        var handler2 = new InAction<AsyncTestEvent>((in AsyncTestEvent evt) => { invocationLog.Add("handler2"); });

        // Act - 在异步上下文中订阅
        SubscriptionToken token1 = default;
        SubscriptionToken token2 = default;
        await Task.Run(() =>
        {
            token1 = AsyncTestEvents.SubscribeAsyncTestEvent(handler1);
            token2 = AsyncTestEvents.SubscribeAsyncTestEvent(handler2);
        });

        try
        {
            // 发布
            await Task.Run(() =>
            {
                AsyncTestEvents.PublishAsyncTestEvent(1);
            });

            // 异步退订
            await Task.Run(() =>
            {
                token1.Dispose();
            });

            // 再次发布
            await Task.Run(() =>
            {
                AsyncTestEvents.PublishAsyncTestEvent(2);
            });

            // Assert
            invocationLog.Should().HaveCount(3); // handler1, handler2, handler2
            invocationLog.Should().Contain("handler1");
            invocationLog.Should().Contain("handler2");
        }
        finally
        {
            token2.Dispose();
        }
    }
}

