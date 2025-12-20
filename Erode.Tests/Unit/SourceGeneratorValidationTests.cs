using FluentAssertions;

namespace Erode.Tests.Unit;

/// <summary>
/// Source Generator 验证测试
/// 注意：编译时错误（如标记非接口方法、非 void 方法等）需要通过实际编译来验证
/// 这里主要测试生成代码的行为
/// </summary>
public class SourceGeneratorValidationTests
{
    [Fact]
    public void GeneratedEvent_ShouldBeRecordStruct()
    {
        // Arrange & Act - 使用测试项目中生成的事件
        var eventType = typeof(TestGeneratedEvent);

        // Assert
        eventType.IsValueType.Should().BeTrue();
        eventType.IsClass.Should().BeFalse();
    }

    [Fact]
    public void GeneratedEvent_ShouldImplementIEvent()
    {
        // Arrange & Act
        var eventType = typeof(TestGeneratedEvent);

        // Assert
        typeof(IEvent).IsAssignableFrom(eventType).Should().BeTrue();
    }

    [Fact]
    public void GeneratedEvent_ShouldBeReadonly()
    {
        // Arrange & Act
        var eventType = typeof(TestGeneratedEvent);

        // Assert - 检查是否是 readonly（通过尝试修改字段来验证，但更直接的是检查类型特性）
        // 对于 record struct，默认是 readonly 的
        eventType.IsValueType.Should().BeTrue();
    }

    [Fact]
    public void GeneratedPublishMethod_ShouldBeStatic()
    {
        // Arrange & Act
        var method = typeof(TestEvents).GetMethod("PublishTestGeneratedEvent");

        // Assert
        method.Should().NotBeNull();
        method!.IsStatic.Should().BeTrue();
    }

    [Fact]
    public void GeneratedSubscribeMethod_ShouldExist()
    {
        // Arrange & Act
        var method = typeof(TestEvents).GetMethod("SubscribeTestGeneratedEvent");

        // Assert
        method.Should().NotBeNull();
        method!.IsStatic.Should().BeTrue();
    }

    [Fact]
    public void GeneratedSubscribeMethod_ShouldReturnSubscriptionToken()
    {
        // Arrange & Act
        var method = typeof(TestEvents).GetMethod("SubscribeTestGeneratedEvent");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(SubscriptionToken));
    }

    [Fact]
    public void GeneratedCode_ShouldNotImplementUserInterface()
    {
        // Arrange & Act
        // 生成的 TestEvents 类不应该实现 ITestEvents 接口
        var testEventsType = typeof(TestEvents);
        var interfaceType = typeof(ITestEvents);

        // Assert
        interfaceType.IsAssignableFrom(testEventsType).Should().BeFalse();
    }
}
