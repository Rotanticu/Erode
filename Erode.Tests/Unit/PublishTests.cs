using Erode;
using Erode.Tests.Helpers;
using FluentAssertions;

namespace Erode.Tests.Unit;

public class PublishTests : TestBase
{
    [Fact]
    public void Publish_WithSubscribedHandler_InvokesHandler()
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
    public void Publish_WithMultipleHandlers_InvokesAllHandlers()
    {
        // Arrange
        var invocationCount = 0;
        var handler1 = new InAction<PublishTestEvent>((in PublishTestEvent evt) => { invocationCount++; });
        var handler2 = new InAction<PublishTestEvent>((in PublishTestEvent evt) => { invocationCount++; });
        var handler3 = new InAction<PublishTestEvent>((in PublishTestEvent evt) => { invocationCount++; });

        var token1 = EventDispatcher<PublishTestEvent>.Subscribe(handler1);
        var token2 = EventDispatcher<PublishTestEvent>.Subscribe(handler2);
        var token3 = EventDispatcher<PublishTestEvent>.Subscribe(handler3);

        // Act
        EventDispatcher<PublishTestEvent>.Publish(new PublishTestEvent());

        // Assert
        invocationCount.Should().Be(3);

        // Cleanup
        token1.Dispose();
        token2.Dispose();
        token3.Dispose();
    }

    [Fact]
    public void Publish_EventDataIsPassedCorrectly_HandlerReceivesCorrectData()
    {
        // Arrange
        TestEventWithData? receivedEvent = null;
        var expectedEvent = new TestEventWithData("Test Message", 42);
        var handler = new InAction<TestEventWithData>((in TestEventWithData evt) => { receivedEvent = evt; });
        var token = EventDispatcher<TestEventWithData>.Subscribe(handler);

        // Act
        EventDispatcher<TestEventWithData>.Publish(expectedEvent);

        // Assert
        receivedEvent.Should().NotBeNull();
        receivedEvent!.Value.Message.Should().Be(expectedEvent.Message);
        receivedEvent.Value.Value.Should().Be(expectedEvent.Value);

        // Cleanup
        token.Dispose();
    }

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
    public void Publish_AllHandlersInvoked()
    {
        // Arrange
        // 注意：ConcurrentDictionary 不保证遍历顺序，所以不测试调用顺序
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

        // Assert - 所有处理器都应该被调用（顺序不确定）
        handler1Invoked.Should().BeTrue();
        handler2Invoked.Should().BeTrue();
        handler3Invoked.Should().BeTrue();

        // Cleanup
        token1.Dispose();
        token2.Dispose();
        token3.Dispose();
    }
}

