using FluentAssertions;

namespace Erode.Tests.Unit;

public class StaticSingletonTests
{
    [Fact]
    public void EventDispatcher_Instance_SameTEvent_ShouldBeSameInstance()
    {
        // Arrange & Act
        var instance1 = EventDispatcher<TestEvent>.Instance;
        var instance2 = EventDispatcher<TestEvent>.Instance;

        // Assert
        instance1.Should().BeSameAs(instance2);
    }

    [Fact]
    public void EventDispatcher_Instance_DifferentTEvent_ShouldBeDifferentInstances()
    {
        // Arrange & Act
        var instance1 = EventDispatcher<TestEvent>.Instance;
        var instance2 = EventDispatcher<AnotherTestEvent>.Instance;

        // Assert
        instance1.Should().NotBeSameAs(instance2);
    }

    [Fact]
    public void EventDispatcher_Subscribe_WithoutExplicitInitialization_ShouldWork()
    {
        // Arrange
        var invoked = false;
        var handler = new InAction<SimpleEvent>((in SimpleEvent evt) => { invoked = true; });

        // Act - 直接使用 Subscribe，不需要先访问 Instance
        var token = EventDispatcher<SimpleEvent>.Subscribe(handler);
        EventDispatcher<SimpleEvent>.Publish(new SimpleEvent());

        // Assert
        invoked.Should().BeTrue();

        // Cleanup
        token.Dispose();
    }

    [Fact]
    public void EventDispatcher_FirstReference_ShouldAutoRegister()
    {
        // Arrange
        // 使用一个全新的、之前未使用过的事件类型
        var invoked = false;
        var handler = new InAction<EventWithValueTypes>((in EventWithValueTypes evt) => { invoked = true; });

        // Act - 第一次引用时应该自动注册
        var token = EventDispatcher<EventWithValueTypes>.Subscribe(handler);
        var instance = EventDispatcher<EventWithValueTypes>.Instance;
        EventDispatcher<EventWithValueTypes>.Publish(new EventWithValueTypes(1, 2, 3.0f));

        // Assert
        invoked.Should().BeTrue();
        instance.Should().NotBeNull();

        // Cleanup
        token.Dispose();
    }
}
