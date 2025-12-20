using FluentAssertions;
using Erode.Tests.Helpers;

namespace Erode.Tests.Unit;

public class InActionTests : TestBase
{
    [Fact]
    public void InAction_ShouldReceiveInParameter_Correctly()
    {
        // Arrange
        var originalEvent = new TestEventWithData("test", 42);
        var receivedEvent = default(TestEventWithData);
        var handler = new InAction<TestEventWithData>((in TestEventWithData evt) =>
        {
            receivedEvent = evt;
        });
        var token = EventDispatcher<TestEventWithData>.Subscribe(handler);

        // Act
        EventDispatcher<TestEventWithData>.Publish(in originalEvent);

        // Assert
        receivedEvent.Should().Be(originalEvent);

        // Cleanup
        token.Dispose();
    }

    [Fact]
    public void InAction_ShouldCaptureExternalVariables()
    {
        // Arrange
        var externalValue = 100;
        var capturedValue = 0;
        var handler = new InAction<TestEvent>((in TestEvent evt) =>
        {
            capturedValue = externalValue;
        });
        var token = EventDispatcher<TestEvent>.Subscribe(handler);

        // Act
        EventDispatcher<TestEvent>.Publish(new TestEvent());

        // Assert
        capturedValue.Should().Be(externalValue);

        // Cleanup
        token.Dispose();
    }

    [Fact]
    public void MultipleInActions_ShouldBeCalledInOrder()
    {
        // Arrange
        var callOrder = new List<int>();
        var handler1 = new InAction<OrderTestEvent>((in OrderTestEvent evt) =>
        {
            callOrder.Add(1);
        });
        var handler2 = new InAction<OrderTestEvent>((in OrderTestEvent evt) =>
        {
            callOrder.Add(2);
        });
        var handler3 = new InAction<OrderTestEvent>((in OrderTestEvent evt) =>
        {
            callOrder.Add(3);
        });

        var token1 = EventDispatcher<OrderTestEvent>.Subscribe(handler1);
        var token2 = EventDispatcher<OrderTestEvent>.Subscribe(handler2);
        var token3 = EventDispatcher<OrderTestEvent>.Subscribe(handler3);

        // Act
        EventDispatcher<OrderTestEvent>.Publish(new OrderTestEvent(0));

        // Assert - 订阅顺序应该与调用顺序一致
        callOrder.Should().Equal(1, 2, 3);

        // Cleanup
        token1.Dispose();
        token2.Dispose();
        token3.Dispose();
    }

    [Fact]
    public void InAction_ThrowingException_ShouldNotPreventOtherSubscribers()
    {
        // Arrange - 使用独立的事件类型避免测试间干扰
        var handler1Invoked = false;
        var handler2Invoked = false;
        var handler3Invoked = false;

        var handler1 = new InAction<IsolatedTestEvent>((in IsolatedTestEvent evt) =>
        {
            handler1Invoked = true;
            throw new InvalidOperationException("Test exception");
        });
        var handler2 = new InAction<IsolatedTestEvent>((in IsolatedTestEvent evt) =>
        {
            handler2Invoked = true;
        });
        var handler3 = new InAction<IsolatedTestEvent>((in IsolatedTestEvent evt) =>
        {
            handler3Invoked = true;
        });

        var token1 = EventDispatcher<IsolatedTestEvent>.Subscribe(handler1);
        var token2 = EventDispatcher<IsolatedTestEvent>.Subscribe(handler2);
        var token3 = EventDispatcher<IsolatedTestEvent>.Subscribe(handler3);

        // Act - 异常不再抛出，而是通过 OnException 转发
        var exception = Record.Exception(() =>
        {
            EventDispatcher<IsolatedTestEvent>.Publish(new IsolatedTestEvent(1));
        });

        // Assert - 所有处理器都应该被调用，即使第一个抛异常
        // 异常不会抛出，发布者逻辑不受影响
        handler1Invoked.Should().BeTrue();
        handler2Invoked.Should().BeTrue();
        handler3Invoked.Should().BeTrue();
        exception.Should().BeNull(); // 异常不再抛出

        // Cleanup
        token1.Dispose();
        token2.Dispose();
        token3.Dispose();
    }

    [Fact]
    public void InAction_ExceptionPropagation_ShouldNotBubbleUp()
    {
        // Arrange - 使用独立的事件类型避免测试间干扰
        var expectedException = new InvalidOperationException("Expected exception");
        var handler = new InAction<SingleExceptionRobustnessTestEvent>((in SingleExceptionRobustnessTestEvent evt) =>
        {
            throw expectedException;
        });
        var token = ExceptionTestEvents.SubscribeSingleExceptionRobustnessTestEvent(handler);

        // Act - 异常不再抛出，而是通过 OnException 转发
        var exception = Record.Exception(() =>
        {
            ExceptionTestEvents.PublishSingleExceptionRobustnessTestEvent(1);
        });

        // Assert - 异常不再抛出，发布者逻辑不受影响
        exception.Should().BeNull();

        // Cleanup
        token.Dispose();
    }
}
