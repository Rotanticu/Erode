using FluentAssertions;

namespace Erode.Tests.Unit;

public class SubscribeTests : TestBase
{
    [Fact]
    public void Subscribe_WithValidHandler_ReturnsValidToken()
    {
        // Arrange
        var handler = new InAction<TestEvent>((in TestEvent evt) => { });

        // Act
        var token = EventDispatcher<TestEvent>.Subscribe(handler);

        // Assert
        token.Should().NotBe(default(SubscriptionToken));
        token.Id.Should().NotBe(0);
        token.Dispatcher.Should().NotBeNull();

        // Cleanup
        token.Dispose();
    }

    [Fact]
    public void Subscribe_MultipleSubscriptions_ReturnsDifferentTokens()
    {
        // Arrange
        var handler = new InAction<TestEvent>((in TestEvent evt) => { });

        // Act
        var token1 = EventDispatcher<TestEvent>.Subscribe(handler);
        var token2 = EventDispatcher<TestEvent>.Subscribe(handler);
        var token3 = EventDispatcher<TestEvent>.Subscribe(handler);

        // Assert
        token1.Id.Should().NotBe(token2.Id);
        token2.Id.Should().NotBe(token3.Id);
        token1.Id.Should().NotBe(token3.Id);
        token1.Dispatcher.Should().NotBeNull();
        token2.Dispatcher.Should().NotBeNull();
        token3.Dispatcher.Should().NotBeNull();

        // Cleanup
        token1.Dispose();
        token2.Dispose();
        token3.Dispose();
    }

    [Fact]
    public void Subscribe_HandlerIsStored_HandlerCanBeInvoked()
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
    public void Subscribe_DifferentEventTypes_AreIndependent()
    {
        // Arrange
        var testEventInvoked = false;
        var testEventWithDataInvoked = false;

        var handler1 = new InAction<TestEvent>((in TestEvent evt) => { testEventInvoked = true; });
        var handler2 = new InAction<TestEventWithData>((in TestEventWithData evt) => { testEventWithDataInvoked = true; });

        // Act
        var token1 = EventDispatcher<TestEvent>.Subscribe(handler1);
        var token2 = EventDispatcher<TestEventWithData>.Subscribe(handler2);

        EventDispatcher<TestEvent>.Publish(new TestEvent());
        EventDispatcher<TestEventWithData>.Publish(new TestEventWithData("test", 42));

        // Assert
        testEventInvoked.Should().BeTrue();
        testEventWithDataInvoked.Should().BeTrue();

        // Cleanup
        token1.Dispose();
        token2.Dispose();
    }
}

