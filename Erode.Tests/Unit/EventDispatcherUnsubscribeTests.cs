using FluentAssertions;
using Erode.Tests.Helpers;

namespace Erode.Tests.Unit;

public class EventDispatcherUnsubscribeTests : TestBase
{
    [Fact]
    public void Unsubscribe_AfterUnsubscribe_ShouldNotReceiveEvent()
    {
        // Arrange
        var invoked = false;
        var handler = new InAction<SimpleTestEvent>((in SimpleTestEvent evt) => { invoked = true; });
        var token = BasicTestEvents.SubscribeSimpleTestEvent(handler);

        // Act
        token.Dispose();
        BasicTestEvents.PublishSimpleTestEvent();

        // Assert
        invoked.Should().BeFalse();
    }

    [Fact]
    public void Unsubscribe_OneSubscriber_ShouldNotAffectOthers()
    {
        // Arrange
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
        token2.Dispose();
        BasicTestEvents.PublishSimpleTestEvent();

        // Assert
        handler1Invoked.Should().BeTrue();
        handler2Invoked.Should().BeFalse();
        handler3Invoked.Should().BeTrue();

        // Cleanup
        token1.Dispose();
        token3.Dispose();
    }

    [Fact]
    public void Unsubscribe_NonExistentId_ShouldNotThrow()
    {
        // Arrange
        var handler = new InAction<SimpleTestEvent>((in SimpleTestEvent evt) => { });
        var realToken = BasicTestEvents.SubscribeSimpleTestEvent(handler);
        realToken.Dispose();

        // 创建一个不存在的 token（使用无效的 ID）
        var nonExistentToken = new SubscriptionToken(999999, realToken.Dispatcher);

        // Act & Assert
        var exception = Record.Exception(() => nonExistentToken.Dispose());
        exception.Should().BeNull();
    }

    [Fact]
    public void Unsubscribe_FromWithinHandler_ShouldSucceed()
    {
        // Arrange
        SubscriptionToken? tokenToUnsubscribe = null;
        var handlerInvoked = false;
        var handler = new InAction<SimpleTestEvent>((in SimpleTestEvent evt) =>
        {
            handlerInvoked = true;
            if (tokenToUnsubscribe.HasValue)
            {
                tokenToUnsubscribe.Value.Dispose();
            }
        });

        var token = BasicTestEvents.SubscribeSimpleTestEvent(handler);
        tokenToUnsubscribe = token;

        // Act
        BasicTestEvents.PublishSimpleTestEvent();

        // Assert - 应该能成功退订
        handlerInvoked.Should().BeTrue();

        // 再次发布，应该不再触发（因为已经退订）
        handlerInvoked = false;
        BasicTestEvents.PublishSimpleTestEvent();
        handlerInvoked.Should().BeFalse();
    }

    [Fact]
    public void Unsubscribe_FromWithinHandler_ShouldNotAffectCurrentPublish()
    {
        // Arrange
        var handler1Invoked = false;
        var handler2Invoked = false;
        SubscriptionToken? token2 = null;

        var handler1 = new InAction<SimpleTestEvent>((in SimpleTestEvent evt) =>
        {
            handler1Invoked = true;
            // 在 handler1 中退订 handler2
            if (token2.HasValue)
            {
                token2.Value.Dispose();
            }
        });

        var handler2 = new InAction<SimpleTestEvent>((in SimpleTestEvent evt) =>
        {
            handler2Invoked = true;
        });

        var token1 = BasicTestEvents.SubscribeSimpleTestEvent(handler1);
        token2 = BasicTestEvents.SubscribeSimpleTestEvent(handler2);

        // Act
        BasicTestEvents.PublishSimpleTestEvent();

        // Assert - handler2 应该仍然被调用（因为当前发布使用的是快照）
        handler1Invoked.Should().BeTrue();
        handler2Invoked.Should().BeTrue();

        // 下次发布时，handler2 不应该被调用
        handler1Invoked = false;
        handler2Invoked = false;
        BasicTestEvents.PublishSimpleTestEvent();
        handler1Invoked.Should().BeTrue();
        handler2Invoked.Should().BeFalse();

        // Cleanup
        token1.Dispose();
    }

    [Fact]
    public void Subscribe_MultipleTimes_UnsubscribeSeparately_ShouldWorkCorrectly()
    {
        // Arrange
        var invocationCount = 0;
        var handler = new InAction<UnsubscribeTestEvent>((in UnsubscribeTestEvent evt) => { invocationCount++; });

        // Act
        var token1 = UnsubscribeTestEvents.SubscribeUnsubscribeTestEvent(handler);
        var token2 = UnsubscribeTestEvents.SubscribeUnsubscribeTestEvent(handler);
        var token3 = UnsubscribeTestEvents.SubscribeUnsubscribeTestEvent(handler);

        UnsubscribeTestEvents.PublishUnsubscribeTestEvent(0);
        invocationCount.Should().Be(3);

        // 退订 token2
        token2.Dispose();
        invocationCount = 0;
        UnsubscribeTestEvents.PublishUnsubscribeTestEvent(0);
        invocationCount.Should().Be(2);

        // 退订 token1
        token1.Dispose();
        invocationCount = 0;
        UnsubscribeTestEvents.PublishUnsubscribeTestEvent(0);
        invocationCount.Should().Be(1);

        // 退订 token3
        token3.Dispose();
        invocationCount = 0;
        UnsubscribeTestEvents.PublishUnsubscribeTestEvent(0);
        invocationCount.Should().Be(0);
    }

    [Fact]
    public void Dispose_MultipleTimes_ShouldBeIdempotent()
    {
        // Arrange
        var handler = new InAction<SimpleTestEvent>((in SimpleTestEvent evt) => { });
        var token = BasicTestEvents.SubscribeSimpleTestEvent(handler);

        // Act
        token.Dispose();
        var exception1 = Record.Exception(() => token.Dispose());
        var exception2 = Record.Exception(() => token.Dispose());

        // Assert
        exception1.Should().BeNull();
        exception2.Should().BeNull();
    }

    [Fact]
    public void Unsubscribe_UsingBlock_ShouldAutoUnsubscribe()
    {
        // Arrange
        var invoked = false;
        var handler = new InAction<SimpleTestEvent>((in SimpleTestEvent evt) => { invoked = true; });

        // Act
        using (var token = BasicTestEvents.SubscribeSimpleTestEvent(handler))
        {
            BasicTestEvents.PublishSimpleTestEvent();
            invoked.Should().BeTrue();
        }

        // Assert - using 块结束后应该自动退订
        invoked = false;
        BasicTestEvents.PublishSimpleTestEvent();
        invoked.Should().BeFalse();
    }

    [Fact]
    public void Unsubscribe_CrossDispatcher_ShouldNotAffectOtherEvents()
    {
        // Arrange
        var testEventInvoked = false;
        var anotherEventInvoked = false;

        var handler1 = new InAction<UnsubscribeTestEvent>((in UnsubscribeTestEvent evt) => { testEventInvoked = true; });
        var handler2 = new InAction<UnsubscribeTest2Event>((in UnsubscribeTest2Event evt) => { anotherEventInvoked = true; });

        var token1 = UnsubscribeTestEvents.SubscribeUnsubscribeTestEvent(handler1);
        var token2 = UnsubscribeTestEvents.SubscribeUnsubscribeTest2Event(handler2);

        // Act - 使用 UnsubscribeTestEvent 的 token 尝试退订（实际上应该只影响 UnsubscribeTestEvent）
        token1.Dispose();

        UnsubscribeTestEvents.PublishUnsubscribeTestEvent(0);
        UnsubscribeTestEvents.PublishUnsubscribeTest2Event();

        // Assert
        testEventInvoked.Should().BeFalse();
        anotherEventInvoked.Should().BeTrue();

        // Cleanup
        token2.Dispose();
    }
}
