using System.Collections.Concurrent;
using Erode;
using Erode.Tests.Helpers;
using FluentAssertions;

namespace Erode.Tests.Unit;

public class ThreadSafetyTests : TestBase
{
    [Fact]
    public void Subscribe_ConcurrentSubscriptions_AllTokensAreValid()
    {
        // Arrange
        const int threadCount = 100;
        var tokens = new SubscriptionToken[threadCount];
        var handler = new InAction<TestEvent>((in TestEvent evt) => { });

        // Act
        Parallel.For(0, threadCount, i =>
        {
            tokens[i] = EventDispatcher<TestEvent>.Subscribe(handler);
        });

        // Assert
        var uniqueIds = tokens.Select(t => t.Id).Distinct().ToList();
        uniqueIds.Should().HaveCount(threadCount);

        // Cleanup
        foreach (var token in tokens)
        {
            token.Dispose();
        }
    }

    [Fact]
    public void Publish_ConcurrentPublishes_AllHandlersInvoked()
    {
        // Arrange
        const int publishCount = 100;
        var invocationCount = 0;
        var handler = new InAction<ThreadSafetyTestEvent>((in ThreadSafetyTestEvent evt) => { Interlocked.Increment(ref invocationCount); });
        var token = EventDispatcher<ThreadSafetyTestEvent>.Subscribe(handler);

        // Act
        Parallel.For(0, publishCount, i =>
        {
            EventDispatcher<ThreadSafetyTestEvent>.Publish(new ThreadSafetyTestEvent());
        });

        // Assert
        invocationCount.Should().Be(publishCount);

        // Cleanup
        token.Dispose();
    }

    [Fact]
    public void Unsubscribe_ConcurrentUnsubscribes_DoesNotThrow()
    {
        // Arrange
        const int threadCount = 50;
        var tokens = new SubscriptionToken[threadCount];
        var handler = new InAction<TestEvent>((in TestEvent evt) => { });

        for (int i = 0; i < threadCount; i++)
        {
            tokens[i] = EventDispatcher<TestEvent>.Subscribe(handler);
        }

        // Act & Assert
        var exception = Record.Exception(() =>
        {
            Parallel.For(0, threadCount, i =>
            {
                tokens[i].Dispose();
            });
        });

        exception.Should().BeNull();
    }

    [Fact]
    public void SubscribeAndPublish_ConcurrentOperations_NoDataLoss()
    {
        // Arrange
        const int operationCount = 1000;
        var invocationCount = 0;
        var handler = new InAction<TestEvent>((in TestEvent evt) => { Interlocked.Increment(ref invocationCount); });
        var tokens = new List<SubscriptionToken>();

        // Act
        Parallel.For(0, operationCount, i =>
        {
            if (i % 2 == 0)
            {
                // Subscribe
                var token = EventDispatcher<TestEvent>.Subscribe(handler);
                lock (tokens)
                {
                    tokens.Add(token);
                }
            }
            else
            {
                // Publish
                EventDispatcher<TestEvent>.Publish(new TestEvent());
            }
        });

        // Assert - 至少应该有一些调用
        invocationCount.Should().BeGreaterThan(0);

        // Cleanup
        foreach (var token in tokens)
        {
            token.Dispose();
        }
    }

    [Fact]
    public void SubscribeUnsubscribePublish_ConcurrentOperations_ThreadSafe()
    {
        // Arrange
        const int threadCount = 50;
        var exceptions = new ConcurrentBag<Exception>();
        var handler = new InAction<TestEvent>((in TestEvent evt) => { });

        // Act
        Parallel.For(0, threadCount, i =>
        {
            try
            {
                var token = EventDispatcher<TestEvent>.Subscribe(handler);
                EventDispatcher<TestEvent>.Publish(new TestEvent());
                token.Dispose();
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        // Assert
        exceptions.Should().BeEmpty();
    }
}

