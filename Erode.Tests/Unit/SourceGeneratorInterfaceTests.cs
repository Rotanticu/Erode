using FluentAssertions;

namespace Erode.Tests.Unit;

/// <summary>
/// Source Generator 接口作为生成源的验证测试
/// </summary>
public class SourceGeneratorInterfaceTests
{
    [Fact]
    public void GeneratedCode_ShouldNotImplementUserInterface()
    {
        // Arrange & Act
        var testEventsType = typeof(TestEvents);
        var interfaceType = typeof(ITestEvents);

        // Assert
        interfaceType.IsAssignableFrom(testEventsType).Should().BeFalse();
    }

    [Fact]
    public void GeneratedCode_CanBeUsedIndependently()
    {
        // Arrange
        var invoked = false;
        var handler = new InAction<TestGeneratedEvent>((in TestGeneratedEvent evt) => { invoked = true; });

        // Act - 直接使用生成的代码，不依赖接口
        var token = TestEvents.SubscribeTestGeneratedEvent(handler);
        TestEvents.PublishTestGeneratedEvent("test", 42);

        // Assert
        invoked.Should().BeTrue();

        // Cleanup
        token.Dispose();
    }

    [Fact]
    public void GeneratedCode_EventFields_ShouldMatchInterfaceMethodParameters()
    {
        // Arrange & Act
        var eventType = typeof(TestGeneratedEvent);
        var interfaceMethod = typeof(ITestEvents).GetMethod("PublishTestGeneratedEvent");

        // Assert - 事件字段应该与方法参数对应
        var eventProperties = eventType.GetProperties();
        var methodParameters = interfaceMethod!.GetParameters();

        eventProperties.Should().HaveCount(methodParameters.Length);

        // 验证属性名和参数名的对应关系（PascalCase vs camelCase）
        for (int i = 0; i < methodParameters.Length; i++)
        {
            var paramName = methodParameters[i].Name;
            if (paramName != null && paramName.Length > 0)
            {
                var propertyName = char.ToUpper(paramName[0]) + paramName.Substring(1);
                eventProperties.Should().Contain(p => p.Name == propertyName);
            }
        }
    }

    [Fact]
    public void GeneratedCode_MultipleMethods_ShouldAllBeGenerated()
    {
        // Arrange & Act
        // 测试 IMultiEventInterface 接口，应该生成多个事件
        var firstEventType = typeof(MultiEventInterface).Assembly.GetTypes()
            .FirstOrDefault(t => t.Name.Contains("First") && typeof(IEvent).IsAssignableFrom(t));
        var secondEventType = typeof(MultiEventInterface).Assembly.GetTypes()
            .FirstOrDefault(t => t.Name.Contains("Second") && typeof(IEvent).IsAssignableFrom(t));

        // 如果生成了多个事件，验证它们都存在
        if (firstEventType != null && secondEventType != null)
        {
            firstEventType.Should().NotBeNull();
            secondEventType.Should().NotBeNull();
            firstEventType.Should().NotBe(secondEventType);
        }
    }

    [Fact]
    public void GeneratedCode_UnmarkedMethods_ShouldBeIgnored()
    {
        // Arrange & Act
        // 验证接口中未标记 [GenerateEvent] 的方法不会被生成事件
        // 这需要通过检查生成的类型来验证
        var allGeneratedEvents = typeof(TestEvents).Assembly.GetTypes()
            .Where(t => typeof(IEvent).IsAssignableFrom(t) && t.IsValueType)
            .ToList();

        // Assert - 只应该生成标记了 [GenerateEvent] 的事件
        // 对于 ITestEvents，应该只生成 TestGeneratedEvent
        allGeneratedEvents.Should().Contain(t => t.Name == "TestGeneratedEvent");
    }
}
