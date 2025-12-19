using Erode;
using Erode.Tests.Helpers;
using FluentAssertions;

namespace Erode.Tests.Unit;

public class EventStructureTests : TestBase
{
    [Fact]
    public void EventType_MustBeStruct_ShouldPassGenericConstraint()
    {
        // Arrange & Act
        var eventType = typeof(TestEvent);
        
        // Assert - 验证是 struct
        eventType.IsValueType.Should().BeTrue();
        eventType.IsClass.Should().BeFalse();
    }

    [Fact]
    public void EventType_MustImplementIEvent_ShouldPassConstraint()
    {
        // Arrange & Act
        var eventType = typeof(TestEvent);
        
        // Assert
        typeof(IEvent).IsAssignableFrom(eventType).Should().BeTrue();
    }

    [Fact]
    public void ReadonlyRecordStruct_ValueEquality_ShouldWorkCorrectly()
    {
        // Arrange
        var event1 = new TestEventWithData("test", 42);
        var event2 = new TestEventWithData("test", 42);
        var event3 = new TestEventWithData("different", 42);
        
        // Act & Assert - Equals
        event1.Equals(event2).Should().BeTrue();
        event1.Equals(event3).Should().BeFalse();
        
        // Act & Assert - GetHashCode
        event1.GetHashCode().Should().Be(event2.GetHashCode());
        event1.GetHashCode().Should().NotBe(event3.GetHashCode());
    }

    [Fact]
    public void Events_WithSameFieldValues_ShouldBeEqual()
    {
        // Arrange
        var event1 = new EventWithValueTypes(10, 20, 30.5f);
        var event2 = new EventWithValueTypes(10, 20, 30.5f);
        var event3 = new EventWithValueTypes(10, 20, 30.6f);
        
        // Act & Assert
        event1.Should().Be(event2);
        event1.Should().NotBe(event3);
    }

    [Fact]
    public void Event_AsInParameter_ShouldNotCauseCopy()
    {
        // Arrange
        var originalEvent = new LargeStructEvent(
            1, 2, 3, 4, 5, 6, 7, 8, 9, 10,
            11, 12, 13, 14, 15, 16, 17, 18, 19, 20
        );
        var receivedEvent = default(LargeStructEvent);
        var handler = new InAction<LargeStructEvent>((in LargeStructEvent evt) =>
        {
            receivedEvent = evt;
        });
        var token = EventDispatcher<LargeStructEvent>.Subscribe(handler);
        
        // Act
        EventDispatcher<LargeStructEvent>.Publish(in originalEvent);
        
        // Assert - 验证数据完整性（如果发生拷贝，数据应该仍然正确）
        receivedEvent.Should().Be(originalEvent);
        
        // Cleanup
        token.Dispose();
    }

    [Fact]
    public void Event_WithoutReferenceFields_ShouldWorkNormally()
    {
        // Arrange
        var eventData = new EventWithValueTypes(1, 2, 3.0f);
        var receivedEvent = default(EventWithValueTypes);
        var handler = new InAction<EventWithValueTypes>((in EventWithValueTypes evt) =>
        {
            receivedEvent = evt;
        });
        var token = EventDispatcher<EventWithValueTypes>.Subscribe(handler);
        
        // Act
        EventDispatcher<EventWithValueTypes>.Publish(eventData);
        
        // Assert
        receivedEvent.Should().Be(eventData);
        
        // Cleanup
        token.Dispose();
    }

    [Fact]
    public void Event_WithReadonlyStructField_ShouldWorkNormally()
    {
        // Arrange
        var readonlyField = new ReadonlyStructField(42);
        var eventData = new EventWithReadonlyStructField(readonlyField);
        var receivedEvent = default(EventWithReadonlyStructField);
        var handler = new InAction<EventWithReadonlyStructField>((in EventWithReadonlyStructField evt) =>
        {
            receivedEvent = evt;
        });
        var token = EventDispatcher<EventWithReadonlyStructField>.Subscribe(handler);
        
        // Act
        EventDispatcher<EventWithReadonlyStructField>.Publish(eventData);
        
        // Assert
        receivedEvent.Should().Be(eventData);
        receivedEvent.Field.Value.Should().Be(42);
        
        // Cleanup
        token.Dispose();
    }
}
