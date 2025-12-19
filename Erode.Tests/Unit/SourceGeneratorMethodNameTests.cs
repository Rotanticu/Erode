using Erode;
using Erode.Tests.Helpers;
using FluentAssertions;

namespace Erode.Tests.Unit;

/// <summary>
/// Source Generator 方法名推断规则测试
/// 注意：编译时错误（如方法名格式错误）需要通过实际编译来验证
/// 这里主要测试正确的方法名生成的事件名
/// </summary>
public class SourceGeneratorMethodNameTests
{
    [Fact]
    public void PublishXxxEvent_ShouldGenerateXxxEvent()
    {
        // Arrange & Act
        // 使用 IPlayerEvents 接口，方法名是 PublishPlayerMovedEvent
        // 应该生成 PlayerMovedEvent 事件类型
        
        // 验证事件类型存在
        var eventTypeName = "PlayerMovedEvent";
        var eventType = Type.GetType($"Erode.Tests.Helpers.{eventTypeName}, Erode.Tests");
        
        // 如果类型不存在，尝试在当前程序集中查找
        if (eventType == null)
        {
            eventType = typeof(PlayerEvents).Assembly.GetTypes()
                .FirstOrDefault(t => t.Name == eventTypeName);
        }
        
        // Assert
        eventType.Should().NotBeNull($"事件类型 {eventTypeName} 应该被生成");
        eventType!.IsValueType.Should().BeTrue();
        typeof(IEvent).IsAssignableFrom(eventType).Should().BeTrue();
    }

    [Fact]
    public void PublishPlayerMovedEvent_ShouldGeneratePlayerMovedEvent()
    {
        // Arrange & Act
        // 验证 PlayerMovedEvent 事件类型和发布方法存在
        var eventTypeName = "PlayerMovedEvent";
        var eventType = typeof(PlayerEvents).Assembly.GetTypes()
            .FirstOrDefault(t => t.Name == eventTypeName);
        
        // Assert
        eventType.Should().NotBeNull();
        
        // 验证发布方法存在
        var publishMethod = typeof(PlayerEvents).GetMethod("PublishPlayerMovedEvent");
        publishMethod.Should().NotBeNull();
        publishMethod!.IsStatic.Should().BeTrue();
    }

    [Fact]
    public void GeneratedEventName_ShouldMatchMethodNamePattern()
    {
        // Arrange & Act
        // 测试方法名 PublishTestGeneratedEvent 应该生成 TestGeneratedEvent
        var eventType = typeof(TestGeneratedEvent);
        
        // Assert
        eventType.Name.Should().Be("TestGeneratedEvent");
        eventType.IsValueType.Should().BeTrue();
    }

    [Fact]
    public void GeneratedPublishMethod_ShouldHaveCorrectName()
    {
        // Arrange & Act
        var method = typeof(TestEvents).GetMethod("PublishTestGeneratedEvent");
        
        // Assert
        method.Should().NotBeNull();
        method!.Name.Should().Be("PublishTestGeneratedEvent");
    }

    [Fact]
    public void GeneratedSubscribeMethod_ShouldHaveCorrectName()
    {
        // Arrange & Act
        var method = typeof(TestEvents).GetMethod("SubscribeTestGeneratedEvent");
        
        // Assert
        method.Should().NotBeNull();
        method!.Name.Should().Be("SubscribeTestGeneratedEvent");
    }

    [Fact]
    public void GeneratedEvent_ShouldHaveCorrectFieldNames()
    {
        // Arrange & Act
        var eventType = typeof(TestGeneratedEvent);
        var properties = eventType.GetProperties();
        
        // Assert - 应该有两个属性：Message 和 Value（PascalCase）
        properties.Should().HaveCount(2);
        properties.Should().Contain(p => p.Name == "Message");
        properties.Should().Contain(p => p.Name == "Value");
    }

    [Fact]
    public void GeneratedPublishMethod_ShouldHaveCorrectParameterNames()
    {
        // Arrange & Act
        var method = typeof(TestEvents).GetMethod("PublishTestGeneratedEvent");
        
        // Assert - 参数名应该是 camelCase（message, value）
        method.Should().NotBeNull();
        var parameters = method!.GetParameters();
        parameters.Should().HaveCount(2);
        parameters[0].Name.Should().Be("message");
        parameters[1].Name.Should().Be("value");
    }

    [Fact]
    public void GeneratedCode_ShouldHandleCaseCorrectly()
    {
        // Arrange & Act
        // 验证大小写处理：PublishPlayerMovedEvent -> PlayerMovedEvent
        var eventTypeName = "PlayerMovedEvent";
        var eventType = typeof(PlayerEvents).Assembly.GetTypes()
            .FirstOrDefault(t => t.Name == eventTypeName);
        
        // Assert
        eventType.Should().NotBeNull();
        // 验证事件名首字母大写
        eventType!.Name[0].Should().Be(char.ToUpper(eventType.Name[0]));
    }
}
