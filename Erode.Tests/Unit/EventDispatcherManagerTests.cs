using Erode;
using Erode.Tests.Helpers;
using FluentAssertions;
using System.Reflection;

namespace Erode.Tests.Unit;

public class EventDispatcherManagerTests
{
    [Fact]
    public void EventDispatcher_EachTEvent_ShouldRegisterOnce()
    {
        // Arrange & Act
        var instance1 = EventDispatcher<TestEvent>.Instance;
        var instance2 = EventDispatcher<TestEvent>.Instance;
        
        // Assert - 应该是同一个实例（单例）
        instance1.Should().BeSameAs(instance2);
    }

    [Fact]
    public void EventDispatcher_RepeatedInitialization_ShouldNotOverwrite()
    {
        // Arrange
        var instance1 = EventDispatcher<TestEvent>.Instance;
        
        // Act - 多次访问 Instance
        var instance2 = EventDispatcher<TestEvent>.Instance;
        var instance3 = EventDispatcher<TestEvent>.Instance;
        
        // Assert - 应该始终是同一个实例
        instance1.Should().BeSameAs(instance2);
        instance2.Should().BeSameAs(instance3);
    }

    [Fact]
    public void EventDispatcher_Registration_ShouldHappenOnFirstUse()
    {
        // Arrange
        // 使用一个全新的、之前未使用过的事件类型
        var invoked = false;
        var handler = new InAction<EventWithReadonlyStructField>((in EventWithReadonlyStructField evt) => { invoked = true; });
        
        // Act - 第一次使用时应该自动注册
        var token = EventDispatcher<EventWithReadonlyStructField>.Subscribe(handler);
        EventDispatcher<EventWithReadonlyStructField>.Publish(new EventWithReadonlyStructField(new ReadonlyStructField(1)));
        
        // Assert
        invoked.Should().BeTrue();
        var instance = EventDispatcher<EventWithReadonlyStructField>.Instance;
        instance.Should().NotBeNull();
        
        // Cleanup
        token.Dispose();
    }

    [Fact]
    public void EventDispatcher_Dictionary_ShouldContainCorrectEventTypeKey()
    {
        // Arrange & Act
        // 通过使用不同的事件类型，验证它们都被正确注册
        var handler1 = new InAction<TestEvent>((in TestEvent evt) => { });
        var handler2 = new InAction<AnotherTestEvent>((in AnotherTestEvent evt) => { });
        
        var token1 = EventDispatcher<TestEvent>.Subscribe(handler1);
        var token2 = EventDispatcher<AnotherTestEvent>.Subscribe(handler2);
        
        // Assert - 两个不同的 dispatcher 实例应该存在
        var instance1 = EventDispatcher<TestEvent>.Instance;
        var instance2 = EventDispatcher<AnotherTestEvent>.Instance;
        instance1.Should().NotBeSameAs(instance2);
        
        // Cleanup
        token1.Dispose();
        token2.Dispose();
    }

    [Fact]
    public void Subscribe_MultipleTimes_ShouldGenerateUniqueIds()
    {
        // Arrange
        var handler = new InAction<TestEvent>((in TestEvent evt) => { });
        
        // Act
        var token1 = EventDispatcher<TestEvent>.Subscribe(handler);
        var token2 = EventDispatcher<TestEvent>.Subscribe(handler);
        var token3 = EventDispatcher<TestEvent>.Subscribe(handler);
        
        // Assert
        token1.Id.Should().NotBe(token2.Id);
        token2.Id.Should().NotBe(token3.Id);
        token1.Id.Should().NotBe(token3.Id);
        
        // Cleanup
        token1.Dispose();
        token2.Dispose();
        token3.Dispose();
    }

    [Fact]
    public void Subscribe_DifferentEventTypes_ShouldGenerateGloballyUniqueIds()
    {
        // Arrange
        var handler1 = new InAction<TestEvent>((in TestEvent evt) => { });
        var handler2 = new InAction<AnotherTestEvent>((in AnotherTestEvent evt) => { });
        
        // Act
        var token1 = EventDispatcher<TestEvent>.Subscribe(handler1);
        var token2 = EventDispatcher<AnotherTestEvent>.Subscribe(handler2);
        var token3 = EventDispatcher<TestEvent>.Subscribe(handler1);
        
        // Assert - 所有 ID 应该全局唯一
        token1.Id.Should().NotBe(token2.Id);
        token2.Id.Should().NotBe(token3.Id);
        token1.Id.Should().NotBe(token3.Id);
        
        // Cleanup
        token1.Dispose();
        token2.Dispose();
        token3.Dispose();
    }

    [Fact]
    public void Subscribe_Multithreaded_ShouldGenerateUniqueIds()
    {
        // Arrange
        const int threadCount = 100;
        var tokens = new SubscriptionToken[threadCount];
        var handler = new InAction<TestEvent>((in TestEvent evt) => { });
        
        // Act
        Parallel.For(0, threadCount, i =>
        {
            tokens[i] = EventDispatcher<TestEvent>.Subscribe(handler);
        });
        
        // Assert - 所有 ID 应该唯一
        var uniqueIds = tokens.Select(t => t.Id).Distinct().ToList();
        uniqueIds.Count.Should().Be(threadCount);
        
        // Cleanup
        foreach (var token in tokens)
        {
            token.Dispose();
        }
    }

    [Fact]
    public void Subscribe_Ids_ShouldBeMonotonicallyIncreasing()
    {
        // Arrange
        var handler = new InAction<TestEvent>((in TestEvent evt) => { });
        
        // Act
        var token1 = EventDispatcher<TestEvent>.Subscribe(handler);
        var token2 = EventDispatcher<TestEvent>.Subscribe(handler);
        var token3 = EventDispatcher<TestEvent>.Subscribe(handler);
        
        // Assert - ID 应该单调递增
        token2.Id.Should().BeGreaterThan(token1.Id);
        token3.Id.Should().BeGreaterThan(token2.Id);
        
        // Cleanup
        token1.Dispose();
        token2.Dispose();
        token3.Dispose();
    }
}
