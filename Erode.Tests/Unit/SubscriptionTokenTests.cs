using Erode;
using Erode.Tests.Helpers;
using FluentAssertions;

namespace Erode.Tests.Unit;

public class SubscriptionTokenTests
{
    [Fact]
    public void Dispose_ShouldTriggerUnsubscribe()
    {
        // Arrange
        var invoked = false;
        var handler = new InAction<TestEvent>((in TestEvent evt) => { invoked = true; });
        var token = EventDispatcher<TestEvent>.Subscribe(handler);
        
        // Act
        token.Dispose();
        EventDispatcher<TestEvent>.Publish(new TestEvent());
        
        // Assert
        invoked.Should().BeFalse();
    }

    [Fact]
    public void Dispose_MultipleCalls_ShouldBeIdempotent()
    {
        // Arrange
        var handler = new InAction<TestEvent>((in TestEvent evt) => { });
        var token = EventDispatcher<TestEvent>.Subscribe(handler);
        
        // Act & Assert - 多次调用 Dispose 不应该抛异常
        token.Dispose();
        var exception1 = Record.Exception(() => token.Dispose());
        var exception2 = Record.Exception(() => token.Dispose());
        
        exception1.Should().BeNull();
        exception2.Should().BeNull();
    }

    [Fact]
    public void Dispose_AfterDispose_EventShouldNotTrigger()
    {
        // Arrange
        var invocationCount = 0;
        var handler = new InAction<TestEvent>((in TestEvent evt) => { invocationCount++; });
        var token = EventDispatcher<TestEvent>.Subscribe(handler);
        
        // Act
        token.Dispose();
        EventDispatcher<TestEvent>.Publish(new TestEvent());
        EventDispatcher<TestEvent>.Publish(new TestEvent());
        
        // Assert
        invocationCount.Should().Be(0);
    }

    [Fact]
    public void MultipleTokens_Dispose_ShouldNotAffectEachOther()
    {
        // Arrange
        var handler1Invoked = false;
        var handler2Invoked = false;
        var handler3Invoked = false;
        
        var handler1 = new InAction<TestEvent>((in TestEvent evt) => { handler1Invoked = true; });
        var handler2 = new InAction<TestEvent>((in TestEvent evt) => { handler2Invoked = true; });
        var handler3 = new InAction<TestEvent>((in TestEvent evt) => { handler3Invoked = true; });
        
        var token1 = EventDispatcher<TestEvent>.Subscribe(handler1);
        var token2 = EventDispatcher<TestEvent>.Subscribe(handler2);
        var token3 = EventDispatcher<TestEvent>.Subscribe(handler3);
        
        // Act - 只 Dispose token2
        token2.Dispose();
        EventDispatcher<TestEvent>.Publish(new TestEvent());
        
        // Assert
        handler1Invoked.Should().BeTrue();
        handler2Invoked.Should().BeFalse();
        handler3Invoked.Should().BeTrue();
        
        // Cleanup
        token1.Dispose();
        token3.Dispose();
    }

    [Fact]
    public void DefaultConstructedToken_Dispose_ShouldNotThrow()
    {
        // Arrange
        var defaultToken = default(SubscriptionToken);
        
        // Act & Assert
        var exception = Record.Exception(() => defaultToken.Dispose());
        exception.Should().BeNull();
    }

    [Fact]
    public void SubscriptionToken_AsReadonlyStruct_CopiedToken_ShouldBehaveConsistently()
    {
        // Arrange
        var invoked = false;
        var handler = new InAction<TestEvent>((in TestEvent evt) => { invoked = true; });
        var originalToken = EventDispatcher<TestEvent>.Subscribe(handler);
        
        // Act - 复制 token（readonly struct 会被复制）
        var copiedToken = originalToken;
        
        // 使用复制的 token 进行 Dispose
        copiedToken.Dispose();
        EventDispatcher<TestEvent>.Publish(new TestEvent());
        
        // Assert - 复制的 token 应该也能正常工作
        invoked.Should().BeFalse();
        
        // 验证原始 token 也被视为已 Dispose（因为它们共享同一个 ID）
        EventDispatcher<TestEvent>.Publish(new TestEvent());
        invoked.Should().BeFalse();
    }
}
