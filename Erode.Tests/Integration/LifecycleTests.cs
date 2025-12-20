using FluentAssertions;

namespace Erode.Tests.Integration;

public class LifecycleTests : TestBase
{
    [Fact]
    public void UsingBlock_AutoUnsubscribes_HandlerNotInvokedAfterBlock()
    {
        // Arrange
        var invoked = false;
        var handler = new InAction<TestEvent>((in TestEvent evt) => { invoked = true; });

        // Act
        using (EventDispatcher<TestEvent>.Subscribe(handler))
        {
            EventDispatcher<TestEvent>.Publish(new TestEvent());
            invoked.Should().BeTrue();
        }

        invoked = false;
        EventDispatcher<TestEvent>.Publish(new TestEvent());

        // Assert
        invoked.Should().BeFalse();
    }

    [Fact]
    public void Dispose_MultipleTimes_DoesNotThrow()
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
    public void Dispose_FromWithinHandler_DoesNotThrow()
    {
        // Arrange
        SubscriptionToken? tokenToDispose = null;
        var handler = new InAction<TestEvent>((in TestEvent evt) =>
        {
            tokenToDispose?.Dispose();
        });

        var token = EventDispatcher<TestEvent>.Subscribe(handler);
        tokenToDispose = token;

        // Act & Assert
        var exception = Record.Exception(() => EventDispatcher<TestEvent>.Publish(new TestEvent()));
        exception.Should().BeNull();
    }

    [Fact]
    public void UsingBlock_NestedUsingBlocks_AllWorkCorrectly()
    {
        // Arrange
        var handler1Invoked = false;
        var handler2Invoked = false;
        var handler3Invoked = false;

        var handler1 = new InAction<TestEvent>((in TestEvent evt) => { handler1Invoked = true; });
        var handler2 = new InAction<TestEvent>((in TestEvent evt) => { handler2Invoked = true; });
        var handler3 = new InAction<TestEvent>((in TestEvent evt) => { handler3Invoked = true; });

        // Act
        using (EventDispatcher<TestEvent>.Subscribe(handler1))
        {
            using (EventDispatcher<TestEvent>.Subscribe(handler2))
            {
                using (EventDispatcher<TestEvent>.Subscribe(handler3))
                {
                    EventDispatcher<TestEvent>.Publish(new TestEvent());
                }
                EventDispatcher<TestEvent>.Publish(new TestEvent());
            }
            EventDispatcher<TestEvent>.Publish(new TestEvent());
        }

        // Assert
        handler1Invoked.Should().BeTrue();
        handler2Invoked.Should().BeTrue();
        handler3Invoked.Should().BeTrue();
    }

    [Fact]
    public void Dispose_ThenPublish_HandlerNotInvoked()
    {
        // Arrange
        var invoked = false;
        var handler = new InAction<LifecycleTestEvent>((in LifecycleTestEvent evt) => { invoked = true; });
        var token = EventDispatcher<LifecycleTestEvent>.Subscribe(handler);

        // Act
        token.Dispose();
        EventDispatcher<LifecycleTestEvent>.Publish(new LifecycleTestEvent());

        // Assert
        invoked.Should().BeFalse();
    }
}

