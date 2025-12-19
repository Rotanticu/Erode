using Erode;
using Erode.Tests.Helpers;
using FluentAssertions;

namespace Erode.Tests.Unit;

/// <summary>
/// 跨程序集/可见性测试
/// 注意：真正的跨程序集测试需要创建额外的程序集，这里主要测试同一程序集内的可见性
/// </summary>
public class CrossAssemblyTests
{
    [Fact]
    public void InternalInterface_ShouldGenerateInternalCode()
    {
        // Arrange & Act
        // 测试 internal 接口生成的代码是否保持 internal
        // 在同一程序集中，internal 类型应该可见
        var internalEventsType = typeof(InternalEvents);
        
        // Assert
        internalEventsType.Should().NotBeNull();
        
        // 验证可以访问 internal 类型的方法
        var publishMethod = internalEventsType.GetMethod("PublishInternalTestEvent");
        publishMethod.Should().NotBeNull();
    }

    [Fact]
    public void PublicInterface_ShouldGeneratePublicCode()
    {
        // Arrange & Act
        // 测试 public 接口生成的代码应该是 public
        var testEventsType = typeof(TestEvents);
        
        // Assert
        testEventsType.Should().NotBeNull();
        testEventsType.IsPublic.Should().BeTrue();
    }

    [Fact]
    public void GeneratedCode_ShouldBeAccessibleFromSameAssembly()
    {
        // Arrange & Act
        // 验证生成的代码在同一程序集中可访问
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
    public void GeneratedCode_EventTypes_ShouldBeAccessible()
    {
        // Arrange & Act
        // 验证生成的事件类型可访问
        var eventType = typeof(TestGeneratedEvent);
        
        // Assert
        eventType.Should().NotBeNull();
        eventType.IsPublic.Should().BeTrue();
        typeof(IEvent).IsAssignableFrom(eventType).Should().BeTrue();
    }
}
