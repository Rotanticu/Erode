using FluentAssertions;

namespace Erode.Tests.Unit;

public class UnsubscribeTests : TestBase
{
    [Fact]
    public void Unsubscribe_WithValidToken_HandlerNoLongerInvoked()
    {
        // Arrange
        var invoked = false;
        var handler = new InAction<TestEvent>((in TestEvent evt) => { invoked = true; });
        var token = EventDispatcher<TestEvent>.Subscribe(handler);

        // Act
        token.Dispose();
        EventDispatcher<TestEvent>.Publish(new TestEvent());

        // Assert
        invoked.Should().BeFalse();
    }

    [Fact]
    public void Unsubscribe_WithInvalidToken_DoesNotThrow()
    {
        // Arrange
        // 创建一个无效的 token（ID 为 0，使用有效的 Dispatcher）
        var invalidToken = new SubscriptionToken(0, EventDispatcher<TestEvent>.Instance);

        // Act & Assert
        var exception = Record.Exception(() => invalidToken.Dispose());
        exception.Should().BeNull();
    }

    [Fact]
    public void Unsubscribe_WithNonExistentToken_DoesNotThrow()
    {
        // Arrange
        // 创建一个不存在的 token（使用 TestEvent 的 Unsubscribe 方法作为 Action）
        var handler = new InAction<TestEvent>((in TestEvent evt) => { });
        var realToken = EventDispatcher<TestEvent>.Subscribe(handler);
        realToken.Dispose(); // 先退订，使 token 无效

        // 使用一个不存在的 ID，但使用有效的 Dispatcher
        var nonExistentToken = new SubscriptionToken(999999, realToken.Dispatcher);

        // Act & Assert
        var exception = Record.Exception(() => nonExistentToken.Dispose());
        exception.Should().BeNull();
    }

    [Fact]
    public void Unsubscribe_MultipleTimes_DoesNotThrow()
    {
        // Arrange
        var handler = new InAction<TestEvent>((in TestEvent evt) => { });
        var token = EventDispatcher<TestEvent>.Subscribe(handler);

        // Act & Assert
        token.Dispose();
        var exception1 = Record.Exception(() => token.Dispose());
        var exception2 = Record.Exception(() => token.Dispose());

        exception1.Should().BeNull();
        exception2.Should().BeNull();
    }

    [Fact]
    public void Unsubscribe_OneHandler_OtherHandlersStillInvoked()
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
        token2.Dispose();
        EventDispatcher<TestEvent>.Publish(new TestEvent());

        // Assert
        handler1Invoked.Should().BeTrue();
        handler2Invoked.Should().BeFalse();
        handler3Invoked.Should().BeTrue();

        // Cleanup
        token1.Dispose();
        token3.Dispose();
    }

    [Fact]
    public void Unsubscribe_DifferentEventTypes_AreIndependent()
    {
        // Arrange
        var testEventInvoked = false;
        var testEventWithDataInvoked = false;

        var handler1 = new InAction<TestEvent>((in TestEvent evt) => { testEventInvoked = true; });
        var handler2 = new InAction<TestEventWithData>((in TestEventWithData evt) => { testEventWithDataInvoked = true; });

        var token1 = EventDispatcher<TestEvent>.Subscribe(handler1);
        var token2 = EventDispatcher<TestEventWithData>.Subscribe(handler2);

        // Act
        token1.Dispose();
        EventDispatcher<TestEvent>.Publish(new TestEvent());
        EventDispatcher<TestEventWithData>.Publish(new TestEventWithData("test", 42));

        // Assert
        testEventInvoked.Should().BeFalse();
        testEventWithDataInvoked.Should().BeTrue();

        // Cleanup
        token2.Dispose();
    }
}

