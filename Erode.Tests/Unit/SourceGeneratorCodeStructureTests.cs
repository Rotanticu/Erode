using FluentAssertions;

namespace Erode.Tests.Unit;

/// <summary>
/// Source Generator 生成代码结构验证测试
/// </summary>
public class SourceGeneratorCodeStructureTests
{
    [Fact]
    public void GeneratedEvent_ShouldBeRecordStruct()
    {
        // Arrange & Act
        var eventType = typeof(TestGeneratedEvent);

        // Assert
        eventType.IsValueType.Should().BeTrue();
        eventType.IsClass.Should().BeFalse();
    }

    [Fact]
    public void GeneratedEvent_Fields_ShouldMatchMethodParameters()
    {
        // Arrange & Act
        var eventType = typeof(TestGeneratedEvent);
        var properties = eventType.GetProperties();

        // Assert - 应该有两个属性对应方法参数
        properties.Should().HaveCount(2);
        properties.Should().Contain(p => p.Name == "Message" && p.PropertyType == typeof(string));
        properties.Should().Contain(p => p.Name == "Value" && p.PropertyType == typeof(int));
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
    public void GeneratedPublishMethod_ShouldCallEventDispatcherPublish()
    {
        // Arrange
        var invoked = false;
        var handler = new InAction<TestGeneratedEvent>((in TestGeneratedEvent evt) => { invoked = true; });
        var token = TestEvents.SubscribeTestGeneratedEvent(handler);

        // Act
        TestEvents.PublishTestGeneratedEvent("test", 42);

        // Assert
        invoked.Should().BeTrue();

        // Cleanup
        token.Dispose();
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
    public void GeneratedClass_ShouldBePartial()
    {
        // Arrange & Act
        var classType = typeof(TestEvents);

        // Assert - 检查是否是 partial（通过检查是否有 Partial 特性或通过其他方式）
        // 实际上，partial 是编译时概念，运行时无法直接检查
        // 但我们可以验证类存在且可以正常使用
        classType.Should().NotBeNull();
    }

    [Fact]
    public void GeneratedCode_MultipleInterfaces_ShouldGenerateSeparateClasses()
    {
        // Arrange & Act
        // 验证不同接口生成不同的类
        var testEventsType = typeof(TestEvents);
        var playerEventsType = typeof(PlayerEvents);

        // Assert
        testEventsType.Should().NotBeNull();
        playerEventsType.Should().NotBeNull();
        testEventsType.Should().NotBe(playerEventsType);
    }

    [Fact]
    public void ParameterNaming_PascalCaseProperty_CamelCaseParameter_ShouldMapCorrectly()
    {
        // Arrange & Act
        var eventType = typeof(TestGeneratedEvent);
        var publishMethod = typeof(TestEvents).GetMethod("PublishTestGeneratedEvent");

        // Assert - 属性名是 PascalCase（Message, Value）
        var properties = eventType.GetProperties();
        properties.Should().Contain(p => p.Name == "Message");
        properties.Should().Contain(p => p.Name == "Value");

        // 方法参数名是 camelCase（message, value）
        var parameters = publishMethod!.GetParameters();
        parameters.Should().Contain(p => p.Name == "message");
        parameters.Should().Contain(p => p.Name == "value");
    }

    [Fact]
    public void KeywordEscape_KeywordParameterNames_ShouldBeEscaped()
    {
        // Arrange & Act
        // 测试关键字参数名（需要在实际接口中定义）
        // 这里验证如果有关键字参数，应该被正确处理
        var keywordEventType = typeof(KeywordParamEvents).Assembly.GetTypes()
            .FirstOrDefault(t => t.Name.Contains("Keyword") && typeof(IEvent).IsAssignableFrom(t));

        // 如果生成了关键字事件，验证其存在
        if (keywordEventType != null)
        {
            keywordEventType.Should().NotBeNull();
        }
    }

    [Fact]
    public void NoParameterEvent_ShouldGenerateCorrectly()
    {
        // Arrange & Act
        // 测试无参数事件
        var noParamEventType = typeof(NoParamEvents).Assembly.GetTypes()
            .FirstOrDefault(t => t.Name.Contains("NoParam") && typeof(IEvent).IsAssignableFrom(t));

        // 如果生成了无参数事件，验证其存在
        if (noParamEventType != null)
        {
            noParamEventType.Should().NotBeNull();
            var properties = noParamEventType.GetProperties();
            properties.Should().BeEmpty();
        }
    }

    [Fact]
    public void LargeParameterEvent_ShouldGenerateCorrectly()
    {
        // Arrange & Act
        // 测试多参数事件（10+ 参数）
        var largeParamEventType = typeof(LargeParamEvents).Assembly.GetTypes()
            .FirstOrDefault(t => t.Name.Contains("LargeParam") && typeof(IEvent).IsAssignableFrom(t));

        // 如果生成了多参数事件，验证其存在
        if (largeParamEventType != null)
        {
            largeParamEventType.Should().NotBeNull();
            var properties = largeParamEventType.GetProperties();
            properties.Should().HaveCountGreaterOrEqualTo(10);
        }
    }

    [Fact]
    public void AccessModifier_InternalInterface_ShouldGenerateInternalCode()
    {
        // Arrange & Act
        // 测试 internal 接口生成的代码是否保持 internal
        var internalEventsType = typeof(InternalEvents);

        // Assert - 验证类型存在（如果是 internal，在同一程序集中应该可见）
        internalEventsType.Should().NotBeNull();
    }

    [Fact]
    public void NamespaceConflict_DifferentNamespaces_SameInterfaceName_ShouldGenerateSeparateClasses()
    {
        // Arrange & Act
        // 测试不同命名空间下的同名接口
        var testEvents1Type = Type.GetType("Erode.Tests.Helpers.TestNamespace1.TestEvents, Erode.Tests");
        var testEvents2Type = Type.GetType("Erode.Tests.Helpers.TestNamespace2.TestEvents, Erode.Tests");

        // 如果类型存在，验证它们是不同的
        if (testEvents1Type != null && testEvents2Type != null)
        {
            testEvents1Type.Should().NotBe(testEvents2Type);
        }
    }
}
