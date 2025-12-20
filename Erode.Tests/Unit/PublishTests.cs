using FluentAssertions;
using Erode.Tests.Helpers;

namespace Erode.Tests.Unit;

public class PublishTests : TestBase
{
    [Fact]
    public void Publish_WithSubscribedHandler_InvokesHandler()
    {
        // Arrange
        var invoked = false;
        var handler = new InAction<SimpleTestEvent>((in SimpleTestEvent evt) => { invoked = true; });
        var token = BasicTestEvents.SubscribeSimpleTestEvent(handler);

        // Act
        BasicTestEvents.PublishSimpleTestEvent();

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
        var handler1 = new InAction<BasicTestEvent>((in BasicTestEvent evt) => { invocationCount++; });
        var handler2 = new InAction<BasicTestEvent>((in BasicTestEvent evt) => { invocationCount++; });
        var handler3 = new InAction<BasicTestEvent>((in BasicTestEvent evt) => { invocationCount++; });

        var token1 = PublishTestEvents.SubscribeBasicTestEvent(handler1);
        var token2 = PublishTestEvents.SubscribeBasicTestEvent(handler2);
        var token3 = PublishTestEvents.SubscribeBasicTestEvent(handler3);

        // Act
        PublishTestEvents.PublishBasicTestEvent(0);

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
        BasicTestWithDataEvent receivedEvent = default;
        var handler = new InAction<BasicTestWithDataEvent>((in BasicTestWithDataEvent evt) => { receivedEvent = evt; });
        var token = BasicTestEventsWithData.SubscribeBasicTestWithDataEvent(handler);

        // Act
        BasicTestEventsWithData.PublishBasicTestWithDataEvent("Test Message", 42);

        // Assert
        receivedEvent.Message.Should().Be("Test Message");
        receivedEvent.Value.Should().Be(42);

        // Cleanup
        token.Dispose();
    }

    [Fact]
    public void Publish_NoSubscribers_DoesNotThrow()
    {
        // Arrange
        // Act & Assert
        var exception = Record.Exception(() => BasicTestEvents.PublishSimpleTestEvent());
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
        var handler1 = new InAction<SimpleTestEvent>((in SimpleTestEvent evt) => { handler1Invoked = true; });
        var handler2 = new InAction<SimpleTestEvent>((in SimpleTestEvent evt) => { handler2Invoked = true; });
        var handler3 = new InAction<SimpleTestEvent>((in SimpleTestEvent evt) => { handler3Invoked = true; });

        var token1 = BasicTestEvents.SubscribeSimpleTestEvent(handler1);
        var token2 = BasicTestEvents.SubscribeSimpleTestEvent(handler2);
        var token3 = BasicTestEvents.SubscribeSimpleTestEvent(handler3);

        // Act
        BasicTestEvents.PublishSimpleTestEvent();

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

