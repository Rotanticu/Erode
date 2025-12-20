using FluentAssertions;

namespace Erode.Tests.Unit;

public class EventDispatcherSubscribePublishTests : TestBase
{
    [Fact]
    public void Subscribe_SingleSubscriber_ShouldReceiveEvent()
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
    public void Subscribe_MultipleSubscribers_ShouldAllReceiveEvent()
    {
        // Arrange
        var handler1Invoked = false;
        var handler2Invoked = false;
        var handler3Invoked = false;

        var handler1 = new InAction<TestEvent>((in TestEvent evt) => { handler1Invoked = true; });
        var handler2 = new InAction<TestEvent>((in TestEvent evt) => { handler2Invoked = true; });
        var handler3 = new InAction<TestEvent>((in TestEvent evt) => { handler3Invoked = true; });

        var token1 = EventDispatcher<TestEvent>.Subscribe(handler1);
        var token2 = EventDispatcher<TestEvent>.Subscribe(handler2);
        var token3 = EventDispatcher<TestEvent>.Subscribe(handler3);

        // Act
        EventDispatcher<TestEvent>.Publish(new TestEvent());

        // Assert
        handler1Invoked.Should().BeTrue();
        handler2Invoked.Should().BeTrue();
        handler3Invoked.Should().BeTrue();

        // Cleanup
        token1.Dispose();
        token2.Dispose();
        token3.Dispose();
    }

    [Fact]
    public void Subscribe_Order_ShouldMatchInvocationOrder()
    {
        // Arrange
        var invocationOrder = new List<int>();
        var handler1 = new InAction<OrderTestEventIsolated>((in OrderTestEventIsolated evt) => { invocationOrder.Add(1); });
        var handler2 = new InAction<OrderTestEventIsolated>((in OrderTestEventIsolated evt) => { invocationOrder.Add(2); });
        var handler3 = new InAction<OrderTestEventIsolated>((in OrderTestEventIsolated evt) => { invocationOrder.Add(3); });

        var token1 = EventDispatcher<OrderTestEventIsolated>.Subscribe(handler1);
        var token2 = EventDispatcher<OrderTestEventIsolated>.Subscribe(handler2);
        var token3 = EventDispatcher<OrderTestEventIsolated>.Subscribe(handler3);

        // Act
        EventDispatcher<OrderTestEventIsolated>.Publish(new OrderTestEventIsolated(0));

        // Assert - 订阅顺序应该与调用顺序一致
        invocationOrder.Should().Equal(1, 2, 3);

        // Cleanup
        token1.Dispose();
        token2.Dispose();
        token3.Dispose();
    }

    [Fact]
    public void Publish_EmptySubscriptionList_ShouldNotThrow()
    {
        // Arrange
        var evt = new TestEvent();

        // Act & Assert
        var exception = Record.Exception(() =>
        {
            EventDispatcher<TestEvent>.Publish(evt);
        });

        exception.Should().BeNull();
    }

    [Fact]
    public void Subscribe_ThenImmediatelyPublish_ShouldReceiveEvent()
    {
        // Arrange
        var invoked = false;
        var handler = new InAction<TestEvent>((in TestEvent evt) => { invoked = true; });

        // Act
        var token = EventDispatcher<TestEvent>.Subscribe(handler);
        EventDispatcher<TestEvent>.Publish(new TestEvent());

        // Assert
        invoked.Should().BeTrue();

        // Cleanup
        token.Dispose();
    }

    [Fact]
    public void Publish_MultipleTimes_ShouldTriggerSubscribersEachTime()
    {
        // Arrange
        var invocationCount = 0;
        var handler = new InAction<OrderTestEventIsolated>((in OrderTestEventIsolated evt) => { invocationCount++; });
        var token = EventDispatcher<OrderTestEventIsolated>.Subscribe(handler);

        // Act
        EventDispatcher<OrderTestEventIsolated>.Publish(new OrderTestEventIsolated(0));
        EventDispatcher<OrderTestEventIsolated>.Publish(new OrderTestEventIsolated(1));
        EventDispatcher<OrderTestEventIsolated>.Publish(new OrderTestEventIsolated(2));

        // Assert
        invocationCount.Should().Be(3);

        // Cleanup
        token.Dispose();
    }

    [Fact]
    public void Subscribe_DifferentEventTypes_ShouldNotInterfere()
    {
        // Arrange
        var testEventInvoked = false;
        var anotherEventInvoked = false;

        var handler1 = new InAction<TestEvent>((in TestEvent evt) => { testEventInvoked = true; });
        var handler2 = new InAction<AnotherTestEvent>((in AnotherTestEvent evt) => { anotherEventInvoked = true; });

        var token1 = EventDispatcher<TestEvent>.Subscribe(handler1);
        var token2 = EventDispatcher<AnotherTestEvent>.Subscribe(handler2);

        // Act
        EventDispatcher<TestEvent>.Publish(new TestEvent());
        EventDispatcher<AnotherTestEvent>.Publish(new AnotherTestEvent("data"));

        // Assert
        testEventInvoked.Should().BeTrue();
        anotherEventInvoked.Should().BeTrue();

        // Cleanup
        token1.Dispose();
        token2.Dispose();
    }
}
