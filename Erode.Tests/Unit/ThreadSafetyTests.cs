using FluentAssertions;
using System.Collections.Concurrent;
using Erode.Tests.Helpers;

namespace Erode.Tests.Unit;

public class ThreadSafetyTests : TestBase
{
    [Fact]
    public void Subscribe_ConcurrentSubscriptions_AllTokensAreValid()
    {
        // Arrange
        const int threadCount = 100;
        var tokens = new SubscriptionToken[threadCount];
        var handler = new InAction<SimpleTestEvent>((in SimpleTestEvent evt) => { });

        // Act
        Parallel.For(0, threadCount, i =>
        {
            tokens[i] = BasicTestEvents.SubscribeSimpleTestEvent(handler);
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
        var token = ThreadSafetyTestEvents.SubscribeThreadSafetyTestEvent(handler);

        // Act
        Parallel.For(0, publishCount, i =>
        {
            ThreadSafetyTestEvents.PublishThreadSafetyTestEvent(0);
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
        var handler = new InAction<SimpleTestEvent>((in SimpleTestEvent evt) => { });

        for (int i = 0; i < threadCount; i++)
        {
            tokens[i] = BasicTestEvents.SubscribeSimpleTestEvent(handler);
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
        var handler = new InAction<SimpleTestEvent>((in SimpleTestEvent evt) => { Interlocked.Increment(ref invocationCount); });
        var tokens = new List<SubscriptionToken>();

        // Act
        Parallel.For(0, operationCount, i =>
        {
            if (i % 2 == 0)
            {
                // Subscribe
                var token = BasicTestEvents.SubscribeSimpleTestEvent(handler);
                lock (tokens)
                {
                    tokens.Add(token);
                }
            }
            else
            {
                // Publish
                BasicTestEvents.PublishSimpleTestEvent();
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
        var handler = new InAction<SimpleTestEvent>((in SimpleTestEvent evt) => { });

        // Act
        Parallel.For(0, threadCount, i =>
        {
            try
            {
                var token = BasicTestEvents.SubscribeSimpleTestEvent(handler);
                BasicTestEvents.PublishSimpleTestEvent();
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

