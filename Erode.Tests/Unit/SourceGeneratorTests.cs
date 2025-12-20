using FluentAssertions;

namespace Erode.Tests.Unit;

public class SourceGeneratorTests : TestBase
{
    [Fact]
    public void Generated_EventClass_Exists()
    {
        // Arrange & Act - 使用测试项目中生成的事件
        var eventType = typeof(TestGeneratedEvent);

        // Assert
        eventType.Should().NotBeNull();
        typeof(IEvent).IsAssignableFrom(eventType).Should().BeTrue();
    }

    [Fact]
    public void Generated_EventClass_ImplementsIEvent()
    {
        // Arrange & Act - 使用测试项目中生成的事件
        var eventType = typeof(TestGeneratedEvent);

        // Assert
        eventType.Should().NotBeNull();
        typeof(IEvent).IsAssignableFrom(eventType).Should().BeTrue();
    }

    [Fact]
    public void Generated_SubscribeMethod_WorksCorrectly()
    {
        // Arrange - 使用测试项目中生成的事件
        var invoked = false;
        var handler = new InAction<TestGeneratedEvent>((in TestGeneratedEvent evt) => { invoked = true; });

        // Act
        var token = TestEvents.SubscribeTestGeneratedEvent(handler);
        TestEvents.PublishTestGeneratedEvent("test", 42);

        // Assert
        invoked.Should().BeTrue();

        // Cleanup
        token.Dispose();
    }

    [Fact]
    public void Generated_PublishMethod_WorksCorrectly()
    {
        // Arrange - 使用测试项目中生成的事件
        TestGeneratedEvent? receivedEvent = null;
        var handler = new InAction<TestGeneratedEvent>((in TestGeneratedEvent evt) => { receivedEvent = evt; });
        var token = TestEvents.SubscribeTestGeneratedEvent(handler);

        // Act
        TestEvents.PublishTestGeneratedEvent("test message", 123);

        // Assert
        receivedEvent.Should().NotBeNull();
        receivedEvent!.Value.Message.Should().Be("test message");
        receivedEvent.Value.Value.Should().Be(123);

        // Cleanup
        token.Dispose();
    }

    [Fact]
    public async Task Generated_PublishMethod_CanBeWrappedInTaskRun()
    {
        // Arrange - 使用测试项目中生成的事件
        var invoked = false;
        var handler = new InAction<TestGeneratedEvent>((in TestGeneratedEvent evt) => { invoked = true; });
        var token = TestEvents.SubscribeTestGeneratedEvent(handler);

        // Act - 用户可以在外部自己包装 Task.Run
        await Task.Run(() => TestEvents.PublishTestGeneratedEvent("test", 42));

        // Assert
        invoked.Should().BeTrue();

        // Cleanup
        token.Dispose();
    }
}

