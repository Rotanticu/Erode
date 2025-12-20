using FluentAssertions;

namespace Erode.Tests.Unit;

public class EdgeCaseTests : TestBase
{
    [Fact]
    public void Publish_NoSubscribers_DoesNotThrow()
    {
        // Arrange
        var evt = new TestEvent();

        // Act & Assert
        var exception = Record.Exception(() => EventDispatcher<TestEvent>.Publish(evt));
        exception.Should().BeNull();
    }

    [Fact]
    public void Publish_ZeroSubscribers_ShouldNotThrow()
    {
        // Arrange
        var evt = new TestEvent();

        // Act & Assert
        var exception = Record.Exception(() => EventDispatcher<TestEvent>.Publish(evt));
        exception.Should().BeNull();
    }

    [Fact]
    public void Publish_SingleSubscriber_ShouldWork()
    {
        // Arrange
        var invoked = false;
        var handler = new InAction<TestEvent>((in TestEvent evt) => { invoked = true; });
        var token = EventDispatcher<TestEvent>.Subscribe(handler);

        // Act
        EventDispatcher<TestEvent>.Publish(new TestEvent());

        // Assert
        invoked.Should().BeTrue();

        // Cleanup
        token.Dispose();
    }

    [Fact]
    public void Subscribe_ThenImmediatelyUnsubscribe_HandlerNotInvoked()
    {
        // Arrange
        var invoked = false;
        var handler = new InAction<TestEvent>((in TestEvent evt) => { invoked = true; });

        // Act
        var token = EventDispatcher<TestEvent>.Subscribe(handler);
        token.Dispose();
        EventDispatcher<TestEvent>.Publish(new TestEvent());

        // Assert
        invoked.Should().BeFalse();
    }

    [Fact]
    public void Unsubscribe_FromWithinHandler_DoesNotThrow()
    {
        // Arrange
        SubscriptionToken? tokenToUnsubscribe = null;
        var handler = new InAction<TestEvent>((in TestEvent evt) =>
        {
            if (tokenToUnsubscribe.HasValue)
            {
                tokenToUnsubscribe.Value.Dispose();
            }
        });

        var token = EventDispatcher<TestEvent>.Subscribe(handler);
        tokenToUnsubscribe = token;

        // Act & Assert
        var exception = Record.Exception(() => EventDispatcher<TestEvent>.Publish(new TestEvent()));
        exception.Should().BeNull();

        // Cleanup
        token.Dispose();
    }

    [Fact]
    public void Subscribe_ManyHandlers_AllInvoked()
    {
        // Arrange
        const int handlerCount = 1000;
        var invocationCount = 0;
        var tokens = new List<SubscriptionToken>();
        var handler = new InAction<EdgeCaseTestEvent>((in EdgeCaseTestEvent evt) => { Interlocked.Increment(ref invocationCount); });

        // Act
        for (int i = 0; i < handlerCount; i++)
        {
            tokens.Add(EventDispatcher<EdgeCaseTestEvent>.Subscribe(handler));
        }

        EventDispatcher<EdgeCaseTestEvent>.Publish(new EdgeCaseTestEvent());

        // Assert
        invocationCount.Should().Be(handlerCount);

        // Cleanup
        foreach (var token in tokens)
        {
            token.Dispose();
        }
    }

    [Fact]
    public void Publish_MultipleTimes_AllHandlersInvokedEachTime()
    {
        // Arrange
        var invocationCount = 0;
        var handler = new InAction<EdgeCaseTestEvent>((in EdgeCaseTestEvent evt) => { invocationCount++; });
        var token = EventDispatcher<EdgeCaseTestEvent>.Subscribe(handler);

        // Act
        EventDispatcher<EdgeCaseTestEvent>.Publish(new EdgeCaseTestEvent());
        EventDispatcher<EdgeCaseTestEvent>.Publish(new EdgeCaseTestEvent());
        EventDispatcher<EdgeCaseTestEvent>.Publish(new EdgeCaseTestEvent());

        // Assert
        invocationCount.Should().Be(3);

        // Cleanup
        token.Dispose();
    }

    [Fact]
    public void Subscribe_SameHandlerMultipleTimes_AllInstancesInvoked()
    {
        // Arrange
        var invocationCount = 0;
        var handler = new InAction<TestEvent>((in TestEvent evt) => { invocationCount++; });

        // Act
        var token1 = EventDispatcher<TestEvent>.Subscribe(handler);
        var token2 = EventDispatcher<TestEvent>.Subscribe(handler);
        var token3 = EventDispatcher<TestEvent>.Subscribe(handler);

        EventDispatcher<TestEvent>.Publish(new TestEvent());

        // Assert
        invocationCount.Should().Be(3);

        // Cleanup
        token1.Dispose();
        token2.Dispose();
        token3.Dispose();
    }

    [Fact]
    public void Subscribe_Unsubscribe_RapidCycle_ShouldWork()
    {
        // Arrange
        var handler = new InAction<TestEvent>((in TestEvent evt) => { });

        // Act & Assert - 快速订阅/退订循环
        for (int i = 0; i < 100; i++)
        {
            var token = EventDispatcher<TestEvent>.Subscribe(handler);
            token.Dispose();
        }

        // 验证没有订阅者
        var invoked = false;
        var testHandler = new InAction<TestEvent>((in TestEvent evt) => { invoked = true; });
        var testToken = EventDispatcher<TestEvent>.Subscribe(testHandler);
        EventDispatcher<TestEvent>.Publish(new TestEvent());
        invoked.Should().BeTrue();
        testToken.Dispose();
    }

    [Fact]
    public void Publish_ConcurrentPublishes_ShouldBeThreadSafe()
    {
        // Arrange
        var invocationCount = 0;
        var handler = new InAction<EdgeCaseTestEvent>((in EdgeCaseTestEvent evt) => { Interlocked.Increment(ref invocationCount); });
        var token = EventDispatcher<EdgeCaseTestEvent>.Subscribe(handler);

        // Act
        Parallel.For(0, 100, i =>
        {
            EventDispatcher<EdgeCaseTestEvent>.Publish(new EdgeCaseTestEvent());
        });

        // Assert - Copy-On-Write 应该保证线程安全
        invocationCount.Should().Be(100);

        // Cleanup
        token.Dispose();
    }

    [Fact]
    public void Publish_SubscribeDuringPublish_IteratorShouldNotFail()
    {
        // Arrange
        var callOrder = new List<int>();
        SubscriptionToken? newToken = null;

        var handler1 = new InAction<EdgeCaseTestEvent>((in EdgeCaseTestEvent evt) =>
        {
            callOrder.Add(1);
            if (newToken == null)
            {
                var handler4 = new InAction<EdgeCaseTestEvent>((in EdgeCaseTestEvent e) => { callOrder.Add(4); });
                newToken = EventDispatcher<EdgeCaseTestEvent>.Subscribe(handler4);
            }
        });

        var handler2 = new InAction<EdgeCaseTestEvent>((in EdgeCaseTestEvent evt) => { callOrder.Add(2); });
        var handler3 = new InAction<EdgeCaseTestEvent>((in EdgeCaseTestEvent evt) => { callOrder.Add(3); });

        var token1 = EventDispatcher<EdgeCaseTestEvent>.Subscribe(handler1);
        var token2 = EventDispatcher<EdgeCaseTestEvent>.Subscribe(handler2);
        var token3 = EventDispatcher<EdgeCaseTestEvent>.Subscribe(handler3);

        // Act
        var exception = Record.Exception(() => EventDispatcher<EdgeCaseTestEvent>.Publish(new EdgeCaseTestEvent()));

        // Assert - 迭代器不应该失效
        exception.Should().BeNull();
        callOrder.Should().Equal(1, 2, 3);

        // Cleanup
        token1.Dispose();
        token2.Dispose();
        token3.Dispose();
        newToken?.Dispose();
    }

    [Fact]
    public void Publish_UnsubscribeDuringPublish_PublishChainShouldNotBreak()
    {
        // Arrange
        var callOrder = new List<int>();
        SubscriptionToken? tokenToUnsubscribe = null;

        var handler1 = new InAction<EdgeCaseTestEvent>((in EdgeCaseTestEvent evt) =>
        {
            callOrder.Add(1);
            if (tokenToUnsubscribe.HasValue)
            {
                tokenToUnsubscribe.Value.Dispose();
            }
        });

        var handler2 = new InAction<EdgeCaseTestEvent>((in EdgeCaseTestEvent evt) => { callOrder.Add(2); });
        var handler3 = new InAction<EdgeCaseTestEvent>((in EdgeCaseTestEvent evt) => { callOrder.Add(3); });

        var token1 = EventDispatcher<EdgeCaseTestEvent>.Subscribe(handler1);
        var token2 = EventDispatcher<EdgeCaseTestEvent>.Subscribe(handler2);
        var token3 = EventDispatcher<EdgeCaseTestEvent>.Subscribe(handler3);
        tokenToUnsubscribe = token2;

        // Act
        var exception = Record.Exception(() => EventDispatcher<EdgeCaseTestEvent>.Publish(new EdgeCaseTestEvent()));

        // Assert - 发布链路不应该崩坏
        exception.Should().BeNull();
        callOrder.Should().Equal(1, 2, 3);

        // Cleanup
        token1.Dispose();
        token3.Dispose();
    }

    [Fact]
    public void Publish_LargeStructEvent_DataIntegrityShouldBePreserved()
    {
        // Arrange
        var receivedEvent = default(LargeStructEvent);
        var handler = new InAction<LargeStructEvent>((in LargeStructEvent evt) =>
        {
            receivedEvent = evt;
        });

        var token = EventDispatcher<LargeStructEvent>.Subscribe(handler);
        var originalEvent = new LargeStructEvent(
            1, 2, 3, 4, 5, 6, 7, 8, 9, 10,
            11, 12, 13, 14, 15, 16, 17, 18, 19, 20
        );

        // Act
        EventDispatcher<LargeStructEvent>.Publish(originalEvent);

        // Assert - in 参数传递应该保持数据完整性
        receivedEvent.Should().Be(originalEvent);
        receivedEvent.Field1.Should().Be(1);
        receivedEvent.Field10.Should().Be(10);
        receivedEvent.Field20.Should().Be(20);

        // Cleanup
        token.Dispose();
    }
}

