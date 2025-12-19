using Erode;
using Erode.Tests.Helpers;
using FluentAssertions;

namespace Erode.Tests.Integration;

public class IntegrationTests : TestBase
{
    [Fact]
    public void CompleteFlow_SubscribePublishUnsubscribe_WorksCorrectly()
    {
        // Arrange
        var invocationLog = new List<string>();
        var handler = new InAction<IntegrationTestEvent>((in IntegrationTestEvent evt) => { invocationLog.Add("handler1"); });
        var handler2 = new InAction<IntegrationTestEvent>((in IntegrationTestEvent evt) => { invocationLog.Add("handler2"); });

        // Act
        var token1 = EventDispatcher<IntegrationTestEvent>.Subscribe(handler);
        var token2 = EventDispatcher<IntegrationTestEvent>.Subscribe(handler2);

        EventDispatcher<IntegrationTestEvent>.Publish(new IntegrationTestEvent());
        invocationLog.Should().HaveCount(2);

        token1.Dispose();
        EventDispatcher<IntegrationTestEvent>.Publish(new IntegrationTestEvent());
        invocationLog.Should().HaveCount(3); // Only handler2 should be invoked

        token2.Dispose();
        EventDispatcher<IntegrationTestEvent>.Publish(new IntegrationTestEvent());
        invocationLog.Should().HaveCount(3); // No more handlers

        // Assert
        invocationLog.Should().Equal(new[] { "handler1", "handler2", "handler2" });
    }

    [Fact]
    public void MultipleEventTypes_IndependentHandling()
    {
        // Arrange
        var testEventInvoked = false;
        var testEventWithDataInvoked = false;
        var testGeneratedEventInvoked = false;

        var handler1 = new InAction<TestEvent>((in TestEvent evt) => { testEventInvoked = true; });
        var handler2 = new InAction<TestEventWithData>((in TestEventWithData evt) => { testEventWithDataInvoked = true; });
        var handler3 = new InAction<TestGeneratedEvent>((in TestGeneratedEvent evt) => { testGeneratedEventInvoked = true; });

        // Act
        var token1 = EventDispatcher<TestEvent>.Subscribe(handler1);
        var token2 = EventDispatcher<TestEventWithData>.Subscribe(handler2);
        var token3 = TestEvents.SubscribeTestGeneratedEvent(handler3);

        EventDispatcher<TestEvent>.Publish(new TestEvent());
        EventDispatcher<TestEventWithData>.Publish(new TestEventWithData("test", 42));
        TestEvents.PublishTestGeneratedEvent("test", 42);

        // Assert
        testEventInvoked.Should().BeTrue();
        testEventWithDataInvoked.Should().BeTrue();
        testGeneratedEventInvoked.Should().BeTrue();

        // Cleanup
        token1.Dispose();
        token2.Dispose();
        token3.Dispose();
    }

    [Fact]
    public void MixedGeneratedAndGenericAPI_WorksTogether()
    {
        // Arrange
        var generatedInvoked = false;
        var genericInvoked = false;

        var handler1 = new InAction<TestGeneratedEvent>((in TestGeneratedEvent evt) => { generatedInvoked = true; });
        var handler2 = new InAction<TestEvent>((in TestEvent evt) => { genericInvoked = true; });

        // Act
        var token1 = TestEvents.SubscribeTestGeneratedEvent(handler1);
        var token2 = EventDispatcher<TestEvent>.Subscribe(handler2);

        TestEvents.PublishTestGeneratedEvent("test", 42);
        EventDispatcher<TestEvent>.Publish(new TestEvent());

        // Assert
        generatedInvoked.Should().BeTrue();
        genericInvoked.Should().BeTrue();

        // Cleanup
        token1.Dispose();
        token2.Dispose();
    }

    [Fact]
    public void ComplexScenario_MultipleEventsAndHandlers_AllWorkCorrectly()
    {
        // Arrange
        var results = new List<string>();

        var handler1 = new InAction<IntegrationTestEvent>((in IntegrationTestEvent evt) => { results.Add("IntegrationTestEvent-1"); });
        var handler2 = new InAction<IntegrationTestEvent>((in IntegrationTestEvent evt) => { results.Add("IntegrationTestEvent-2"); });
        var handler3 = new InAction<TestEventWithData>((in TestEventWithData evt) => { results.Add($"TestEventWithData-{evt.Message}"); });
        var handler4 = new InAction<TestGeneratedEvent>((in TestGeneratedEvent evt) => { results.Add($"TestGeneratedEvent-{evt.Message}"); });

        // Act
        var token1 = EventDispatcher<IntegrationTestEvent>.Subscribe(handler1);
        var token2 = EventDispatcher<IntegrationTestEvent>.Subscribe(handler2);
        var token3 = EventDispatcher<TestEventWithData>.Subscribe(handler3);
        var token4 = TestEvents.SubscribeTestGeneratedEvent(handler4);

        EventDispatcher<IntegrationTestEvent>.Publish(new IntegrationTestEvent());
        EventDispatcher<TestEventWithData>.Publish(new TestEventWithData("data1", 1));
        TestEvents.PublishTestGeneratedEvent("generated1", 1);

        token2.Dispose();
        EventDispatcher<IntegrationTestEvent>.Publish(new IntegrationTestEvent());
        EventDispatcher<TestEventWithData>.Publish(new TestEventWithData("data2", 3));

        // Assert
        results.Should().HaveCount(6);
        results.Should().Contain("IntegrationTestEvent-1");
        results.Should().Contain("IntegrationTestEvent-2");
        results.Should().Contain("TestEventWithData-data1");
        results.Should().Contain("TestGeneratedEvent-generated1");
        results.Skip(3).Should().NotContain("IntegrationTestEvent-2"); // After unsubscribe

        // Cleanup
        token1.Dispose();
        token3.Dispose();
        token4.Dispose();
    }
}

