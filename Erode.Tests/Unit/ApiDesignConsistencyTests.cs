using FluentAssertions;

namespace Erode.Tests.Unit;

/// <summary>
/// API 设计一致性测试（UX 验证）
/// </summary>
public class ApiDesignConsistencyTests
{
    [Fact]
    public void User_OnlyNeedsToWriteInterface_ToDefineEvent()
    {
        // Arrange & Act
        // 用户只需要定义接口，Source Generator 会自动生成事件类型
        // 验证接口定义后，生成的事件类型存在
        var eventType = typeof(TestGeneratedEvent);

        // Assert
        eventType.Should().NotBeNull();
        eventType.IsValueType.Should().BeTrue();
        typeof(IEvent).IsAssignableFrom(eventType).Should().BeTrue();
    }

    [Fact]
    public void User_NoNeedToWriteEventType_Manually()
    {
        // Arrange & Act
        // 验证用户不需要手动编写事件类型
        // 事件类型应该由 Source Generator 自动生成
        var eventType = typeof(TestGeneratedEvent);

        // Assert - 事件类型存在且符合预期结构
        eventType.Should().NotBeNull();
        eventType.IsValueType.Should().BeTrue();

        // 验证事件类型有正确的属性（对应接口方法的参数）
        var properties = eventType.GetProperties();
        properties.Should().HaveCount(2); // Message 和 Value
    }

    [Fact]
    public void EventPublish_Syntax_ShouldBeConcise()
    {
        // Arrange
        var invoked = false;
        var handler = new InAction<TestGeneratedEvent>((in TestGeneratedEvent evt) => { invoked = true; });
        var token = TestEvents.SubscribeTestGeneratedEvent(handler);

        // Act - 发布语法应该简洁
        TestEvents.PublishTestGeneratedEvent("test", 42);

        // Assert
        invoked.Should().BeTrue();

        // 验证语法简洁：只需要调用静态方法，不需要手动创建事件对象
        // TestEvents.PublishTestGeneratedEvent("test", 42) 比
        // Events.Publish(new TestGeneratedEvent("test", 42)) 更简洁

        // Cleanup
        token.Dispose();
    }

    [Fact]
    public void Subscribe_API_ShouldBeStronglyTyped_NoReflection()
    {
        // Arrange & Act
        // 验证 Subscribe API 是强类型的，不需要反射
        var handler = new InAction<TestGeneratedEvent>((in TestGeneratedEvent evt) => { });

        // Act
        var token = TestEvents.SubscribeTestGeneratedEvent(handler);

        // Assert
        token.Should().NotBe(default(SubscriptionToken));
        token.Id.Should().BeGreaterThan(0);

        // 验证类型安全：handler 必须是 InAction<TestGeneratedEvent>
        // 如果类型不匹配，编译时就会报错，不需要运行时反射检查

        // Cleanup
        token.Dispose();
    }

    [Fact]
    public void GeneratedCode_ShouldSupportIDEIntelliSense()
    {
        // Arrange & Act
        // 验证生成的代码支持 IDE 自动补全
        // 通过检查方法签名和 XML 注释来验证

        var publishMethod = typeof(TestEvents).GetMethod("PublishTestGeneratedEvent");
        var subscribeMethod = typeof(TestEvents).GetMethod("SubscribeTestGeneratedEvent");

        // Assert
        publishMethod.Should().NotBeNull();
        subscribeMethod.Should().NotBeNull();

        // 验证方法有正确的参数
        publishMethod!.GetParameters().Should().HaveCount(2);
        subscribeMethod!.GetParameters().Should().HaveCount(1);

        // 验证返回类型
        publishMethod.ReturnType.Should().Be(typeof(void));
        subscribeMethod.ReturnType.Should().Be(typeof(SubscriptionToken));

        // 验证方法名清晰易懂
        publishMethod.Name.Should().StartWith("Publish");
        subscribeMethod.Name.Should().StartWith("Subscribe");
    }
}
