using FluentAssertions;

namespace Erode.Tests.Unit;

public class CopyOnWriteTests : TestBase
{
    [Fact]
    public void Publish_SubscribeDuringPublish_ShouldNotAffectCurrentPublish()
    {
        // Arrange
        var handler1Invoked = false;
        var handler2Invoked = false;
        SubscriptionToken? token2 = null;

        var handler1 = new InAction<TestEvent>((in TestEvent evt) =>
        {
            handler1Invoked = true;
            // 在发布过程中订阅新处理器
            if (token2 == null)
            {
                var handler2 = new InAction<TestEvent>((in TestEvent e) => { handler2Invoked = true; });
                token2 = EventDispatcher<TestEvent>.Subscribe(handler2);
            }
        });

        var token1 = EventDispatcher<TestEvent>.Subscribe(handler1);

        // Act
        EventDispatcher<TestEvent>.Publish(new TestEvent());

        // Assert - handler2 不应该在当前发布中被调用（因为使用的是快照）
        handler1Invoked.Should().BeTrue();
        handler2Invoked.Should().BeFalse();

        // 下次发布时，handler2 应该被调用
        handler1Invoked = false;
        EventDispatcher<TestEvent>.Publish(new TestEvent());
        handler1Invoked.Should().BeTrue();
        handler2Invoked.Should().BeTrue();

        // Cleanup
        token1.Dispose();
        if (token2.HasValue)
        {
            token2.Value.Dispose();
        }
    }

    [Fact]
    public void Publish_UnsubscribeDuringPublish_ShouldNotAffectCurrentPublish()
    {
        // Arrange
        var handler1Invoked = false;
        var handler2Invoked = false;
        SubscriptionToken? token2 = null;

        var handler1 = new InAction<TestEvent>((in TestEvent evt) =>
        {
            handler1Invoked = true;
            // 在发布过程中退订 handler2
            if (token2.HasValue)
            {
                token2.Value.Dispose();
            }
        });

        var handler2 = new InAction<TestEvent>((in TestEvent evt) =>
        {
            handler2Invoked = true;
        });

        var token1 = EventDispatcher<TestEvent>.Subscribe(handler1);
        token2 = EventDispatcher<TestEvent>.Subscribe(handler2);

        // Act
        EventDispatcher<TestEvent>.Publish(new TestEvent());

        // Assert - handler2 应该仍然被调用（因为当前发布使用的是快照）
        handler1Invoked.Should().BeTrue();
        handler2Invoked.Should().BeTrue();

        // 下次发布时，handler2 不应该被调用
        handler1Invoked = false;
        handler2Invoked = false;
        EventDispatcher<TestEvent>.Publish(new TestEvent());
        handler1Invoked.Should().BeTrue();
        handler2Invoked.Should().BeFalse();

        // Cleanup
        token1.Dispose();
    }

    [Fact]
    public void Publish_HandlerArray_ShouldBeSnapshot()
    {
        // Arrange
        var handler1Invoked = false;
        var handler2Invoked = false;
        var handler3Invoked = false;

        var handler1 = new InAction<TestEvent>((in TestEvent evt) =>
        {
            handler1Invoked = true;
        });

        var handler2 = new InAction<TestEvent>((in TestEvent evt) =>
        {
            handler2Invoked = true;
        });

        var handler3 = new InAction<TestEvent>((in TestEvent evt) =>
        {
            handler3Invoked = true;
        });

        var token1 = EventDispatcher<TestEvent>.Subscribe(handler1);
        var token2 = EventDispatcher<TestEvent>.Subscribe(handler2);

        // Act - 在发布前获取快照，然后在发布过程中修改订阅列表
        // 发布应该使用发布时的快照
        EventDispatcher<TestEvent>.Publish(new TestEvent());

        // 在发布后添加新订阅者
        var token3 = EventDispatcher<TestEvent>.Subscribe(handler3);

        // 再次发布
        handler1Invoked = false;
        handler2Invoked = false;
        handler3Invoked = false;
        EventDispatcher<TestEvent>.Publish(new TestEvent());

        // Assert - 所有三个处理器都应该被调用
        handler1Invoked.Should().BeTrue();
        handler2Invoked.Should().BeTrue();
        handler3Invoked.Should().BeTrue();

        // Cleanup
        token1.Dispose();
        token2.Dispose();
        token3.Dispose();
    }

    [Fact]
    public void Publish_ModifySubscriptionList_ShouldNotCauseException()
    {
        // Arrange
        var handler1Invoked = false;
        SubscriptionToken? token2 = null;

        var handler1 = new InAction<TestEvent>((in TestEvent evt) =>
        {
            handler1Invoked = true;
            // 在发布过程中修改订阅列表（订阅和退订）
            if (token2.HasValue)
            {
                token2.Value.Dispose();
            }
            else
            {
                var handler2 = new InAction<TestEvent>((in TestEvent e) => { });
                token2 = EventDispatcher<TestEvent>.Subscribe(handler2);
            }
        });

        var token1 = EventDispatcher<TestEvent>.Subscribe(handler1);

        // Act & Assert - 不应该抛异常
        var exception = Record.Exception(() =>
        {
            EventDispatcher<TestEvent>.Publish(new TestEvent());
            EventDispatcher<TestEvent>.Publish(new TestEvent());
        });

        exception.Should().BeNull();
        handler1Invoked.Should().BeTrue();

        // Cleanup
        token1.Dispose();
        if (token2.HasValue)
        {
            token2.Value.Dispose();
        }
    }
}
