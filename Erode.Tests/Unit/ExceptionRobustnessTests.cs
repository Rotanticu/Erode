using FluentAssertions;
using Erode.Tests.Helpers;

namespace Erode.Tests.Unit;

[Collection("Sequential")]
public class ExceptionRobustnessTests : TestBase
{
    [Fact]
    public void Handler_ThrowingException_OtherHandlersShouldStillBeCalled()
    {
        // Arrange - 使用独立的事件类型避免测试间干扰
        var handler1Invoked = false;
        var handler2Invoked = false;
        var handler3Invoked = false;

        var handler1 = new InAction<IsolatedTestEvent>((in IsolatedTestEvent evt) =>
        {
            handler1Invoked = true;
            throw new InvalidOperationException("Handler 1 exception");
        });
        var handler2 = new InAction<IsolatedTestEvent>((in IsolatedTestEvent evt) =>
        {
            handler2Invoked = true;
        });
        var handler3 = new InAction<IsolatedTestEvent>((in IsolatedTestEvent evt) =>
        {
            handler3Invoked = true;
        });

        var token1 = EventDispatcher<IsolatedTestEvent>.Subscribe(handler1);
        var token2 = EventDispatcher<IsolatedTestEvent>.Subscribe(handler2);
        var token3 = EventDispatcher<IsolatedTestEvent>.Subscribe(handler3);

        // Act - 异常不再抛出，而是通过 OnException 转发
        var exception = Record.Exception(() =>
        {
            EventDispatcher<IsolatedTestEvent>.Publish(new IsolatedTestEvent(1));
        });

        // Assert - 所有处理器都应该被调用，即使第一个抛异常
        // 异常不会抛出，发布者逻辑不受影响
        handler1Invoked.Should().BeTrue();
        handler2Invoked.Should().BeTrue();
        handler3Invoked.Should().BeTrue();
        exception.Should().BeNull(); // 异常不再抛出

        // Cleanup
        token1.Dispose();
        token2.Dispose();
        token3.Dispose();
    }

    [Fact]
    public void Handler_ThrowingException_ShouldNotAffectNextPublish()
    {
        // Arrange - 使用独立的事件类型避免测试间干扰
        var invocationCount = 0;
        var handler = new InAction<IsolatedTestEvent>((in IsolatedTestEvent evt) =>
        {
            invocationCount++;
            if (invocationCount == 1)
            {
                throw new InvalidOperationException("First invocation exception");
            }
        });

        var token = EventDispatcher<IsolatedTestEvent>.Subscribe(handler);

        // Act - 异常不再抛出，发布者逻辑不受影响
        var exception1 = Record.Exception(() =>
        {
            EventDispatcher<IsolatedTestEvent>.Publish(new IsolatedTestEvent(1));
        });

        // 第二次发布应该仍然正常工作
        var exception2 = Record.Exception(() =>
        {
            EventDispatcher<IsolatedTestEvent>.Publish(new IsolatedTestEvent(2));
        });

        // Assert
        invocationCount.Should().Be(2);
        exception1.Should().BeNull(); // 异常不再抛出
        exception2.Should().BeNull(); // 第二次也不抛异常

        // Cleanup
        token.Dispose();
    }

    [Fact]
    public void Unsubscribe_FromExceptionHandler_ShouldSucceed()
    {
        // Arrange - 使用独立的事件类型避免测试间干扰
        SubscriptionToken? tokenToUnsubscribe = null;
        var handlerInvoked = false;
        var handler = new InAction<ExceptionRobustnessTestEvent>((in ExceptionRobustnessTestEvent evt) =>
        {
            handlerInvoked = true;
            if (tokenToUnsubscribe.HasValue)
            {
                tokenToUnsubscribe.Value.Dispose();
            }
            throw new InvalidOperationException("Exception after unsubscribe");
        });

        var token = ExceptionTestEvents.SubscribeExceptionRobustnessTestEvent(handler);
        tokenToUnsubscribe = token;

        // Act - 异常不再抛出
        var exception = Record.Exception(() =>
        {
            ExceptionTestEvents.PublishExceptionRobustnessTestEvent(1);
        });

        // Assert - 应该能成功退订，即使抛异常（异常不再抛出）
        handlerInvoked.Should().BeTrue();
        exception.Should().BeNull(); // 异常不再抛出

        // 下次发布不应该触发（因为已经退订）
        handlerInvoked = false;
        ExceptionTestEvents.PublishExceptionRobustnessTestEvent(2);
        handlerInvoked.Should().BeFalse();
    }

    [Fact]
    public void Handler_MultipleExceptions_ShouldNotThrow()
    {
        // Arrange - 使用独立的事件类型避免测试间干扰
        var handler1 = new InAction<MultipleExceptionRobustnessTestEvent>((in MultipleExceptionRobustnessTestEvent evt) =>
        {
            throw new InvalidOperationException("Handler 1 exception");
        });
        var handler2 = new InAction<MultipleExceptionRobustnessTestEvent>((in MultipleExceptionRobustnessTestEvent evt) =>
        {
            throw new ArgumentException("Handler 2 exception");
        });
        var handler3 = new InAction<MultipleExceptionRobustnessTestEvent>((in MultipleExceptionRobustnessTestEvent evt) =>
        {
            throw new NotSupportedException("Handler 3 exception");
        });

        var token1 = ExceptionTestEvents.SubscribeMultipleExceptionRobustnessTestEvent(handler1);
        var token2 = ExceptionTestEvents.SubscribeMultipleExceptionRobustnessTestEvent(handler2);
        var token3 = ExceptionTestEvents.SubscribeMultipleExceptionRobustnessTestEvent(handler3);

        // Act - 异常不再抛出
        var exception = Record.Exception(() =>
        {
            ExceptionTestEvents.PublishMultipleExceptionRobustnessTestEvent(1);
        });

        // Assert - 异常不再抛出，发布者逻辑不受影响
        exception.Should().BeNull();

        // Cleanup
        token1.Dispose();
        token2.Dispose();
        token3.Dispose();
    }

    [Fact]
    public void Handler_SingleException_ShouldNotThrow()
    {
        // Arrange - 使用独立的事件类型避免测试间干扰
        var handler = new InAction<SingleExceptionRobustnessTestEvent>((in SingleExceptionRobustnessTestEvent evt) =>
        {
            throw new InvalidOperationException("Single exception");
        });

        var token = ExceptionTestEvents.SubscribeSingleExceptionRobustnessTestEvent(handler);

        // Act - 异常不再抛出
        var exception = Record.Exception(() =>
        {
            ExceptionTestEvents.PublishSingleExceptionRobustnessTestEvent(1);
        });

        // Assert - 异常不再抛出，发布者逻辑不受影响
        exception.Should().BeNull();

        // Cleanup
        token.Dispose();
    }

    [Fact]
    public void Subscribe_NullHandler_ShouldThrowArgumentNullException()
    {
        // Arrange - 使用独立的事件类型避免测试间干扰
        InAction<IsolatedTestEvent>? nullHandler = null;

        // Act & Assert
        var exception = Record.Exception(() =>
        {
            EventDispatcher<IsolatedTestEvent>.Subscribe(nullHandler!);
        });

        // Assert - 应该抛出 ArgumentNullException
        exception.Should().NotBeNull();
        exception.Should().BeOfType<ArgumentNullException>();
        var argNullEx = exception as ArgumentNullException;
        argNullEx!.ParamName.Should().Be("handler");
    }

    [Fact]
    public void OnException_WhenNull_ShouldSwallowExceptions()
    {
        // Arrange - 使用生成的事件类型，确保 OnException 为 null
        var originalHandler = TestEvents.OnException;
        TestEvents.OnException = null;

        try
        {
            var handler = new InAction<TestGeneratedEvent>((in TestGeneratedEvent evt) =>
            {
                throw new InvalidOperationException("Test exception");
            });

            var token = TestEvents.SubscribeTestGeneratedEvent(handler);

            // Act - 异常应该被静默吞掉（因为 OnException 为 null）
            var exception = Record.Exception(() =>
            {
                TestEvents.PublishTestGeneratedEvent("test", 42);
            });

            // Assert - 异常不应该抛出
            exception.Should().BeNull();

            // Cleanup
            token.Dispose();
        }
        finally
        {
            TestEvents.OnException = originalHandler;
        }
    }

    [Fact]
    public void OnException_WhenSet_ShouldReceiveExceptions()
    {
        // Arrange - 使用生成的事件类型
        var originalHandler = TestEvents.OnException;
        IEvent? receivedEvent = null;
        System.Delegate? receivedHandler = null;
        Exception? receivedException = null;

        TestEvents.OnException = (evt, handler, ex) =>
        {
            receivedEvent = evt;
            receivedHandler = handler;
            receivedException = ex;
        };

        try
        {
            var handler = new InAction<TestGeneratedEvent>((in TestGeneratedEvent evt) =>
            {
                throw new InvalidOperationException("Test exception");
            });

            var token = TestEvents.SubscribeTestGeneratedEvent(handler);

            // Act
            var exception = Record.Exception(() =>
            {
                TestEvents.PublishTestGeneratedEvent("test", 42);
            });

            // Assert - 异常不应该抛出，但应该被转发到 OnException
            exception.Should().BeNull();
            receivedEvent.Should().NotBeNull();
            receivedEvent.Should().BeOfType<TestGeneratedEvent>();
            var testEvent = (TestGeneratedEvent)receivedEvent!;
            testEvent.Message.Should().Be("test");
            testEvent.Value.Should().Be(42);
            receivedHandler.Should().Be(handler);
            receivedException.Should().BeOfType<InvalidOperationException>();
            receivedException!.Message.Should().Be("Test exception");

            // Cleanup
            token.Dispose();
        }
        finally
        {
            TestEvents.OnException = originalHandler;
        }
    }

    [Fact]
    public void OnException_MultipleExceptions_ShouldReceiveEachException()
    {
        // Arrange - 使用生成的事件类型
        var originalHandler = TestEvents.OnException;
        var receivedExceptions = new List<Exception>();

        TestEvents.OnException = (evt, handler, ex) =>
        {
            receivedExceptions.Add(ex);
        };

        try
        {
            var handler1 = new InAction<TestGeneratedEvent>((in TestGeneratedEvent evt) =>
            {
                throw new InvalidOperationException("Handler 1 exception");
            });
            var handler2 = new InAction<TestGeneratedEvent>((in TestGeneratedEvent evt) =>
            {
                throw new ArgumentException("Handler 2 exception");
            });
            var handler3 = new InAction<TestGeneratedEvent>((in TestGeneratedEvent evt) =>
            {
                throw new NotSupportedException("Handler 3 exception");
            });

            var token1 = TestEvents.SubscribeTestGeneratedEvent(handler1);
            var token2 = TestEvents.SubscribeTestGeneratedEvent(handler2);
            var token3 = TestEvents.SubscribeTestGeneratedEvent(handler3);

            // Act
            var exception = Record.Exception(() =>
            {
                TestEvents.PublishTestGeneratedEvent("test", 42);
            });

            // Assert - 每个异常都应该被转发到 OnException
            exception.Should().BeNull();
            receivedExceptions.Should().HaveCount(3);
            receivedExceptions.Should().Contain(ex => ex is InvalidOperationException);
            receivedExceptions.Should().Contain(ex => ex is ArgumentException);
            receivedExceptions.Should().Contain(ex => ex is NotSupportedException);

            // Cleanup
            token1.Dispose();
            token2.Dispose();
            token3.Dispose();
        }
        finally
        {
            TestEvents.OnException = originalHandler;
        }
    }

    [Fact]
    public void OnException_ExceptionInHandler_ShouldNotAffectOtherHandlers()
    {
        // Arrange - 使用生成的事件类型
        var originalHandler = TestEvents.OnException;
        var handler1Invoked = false;
        var handler2Invoked = false;
        var handler3Invoked = false;
        var exceptionCount = 0;

        TestEvents.OnException = (evt, handler, ex) =>
        {
            exceptionCount++;
        };

        try
        {
            var handler1 = new InAction<TestGeneratedEvent>((in TestGeneratedEvent evt) =>
            {
                handler1Invoked = true;
                throw new InvalidOperationException("Handler 1 exception");
            });
            var handler2 = new InAction<TestGeneratedEvent>((in TestGeneratedEvent evt) =>
            {
                handler2Invoked = true;
            });
            var handler3 = new InAction<TestGeneratedEvent>((in TestGeneratedEvent evt) =>
            {
                handler3Invoked = true;
            });

            var token1 = TestEvents.SubscribeTestGeneratedEvent(handler1);
            var token2 = TestEvents.SubscribeTestGeneratedEvent(handler2);
            var token3 = TestEvents.SubscribeTestGeneratedEvent(handler3);

            // Act
            var exception = Record.Exception(() =>
            {
                TestEvents.PublishTestGeneratedEvent("test", 42);
            });

            // Assert - 所有 handler 都应该被调用，即使第一个抛异常
            exception.Should().BeNull();
            handler1Invoked.Should().BeTrue();
            handler2Invoked.Should().BeTrue();
            handler3Invoked.Should().BeTrue();
            exceptionCount.Should().Be(1); // 只有一个异常

            // Cleanup
            token1.Dispose();
            token2.Dispose();
            token3.Dispose();
        }
        finally
        {
            TestEvents.OnException = originalHandler;
        }
    }

    [Fact]
    public void OnException_GlobalHandler_ShouldAlsoBeCalled()
    {
        // Arrange - 测试类级别的 OnException 和全局的 EventDispatcher.OnException 都会被调用
        var originalClassHandler = TestEvents.OnException;
        var originalGlobalHandler = EventDispatcher.OnException;

        var classHandlerCalled = false;
        var globalHandlerCalled = false;

        TestEvents.OnException = (evt, handler, ex) =>
        {
            classHandlerCalled = true;
        };

        EventDispatcher.OnException = (evt, handler, ex) =>
        {
            globalHandlerCalled = true;
        };

        try
        {
            var handler = new InAction<TestGeneratedEvent>((in TestGeneratedEvent evt) =>
            {
                throw new InvalidOperationException("Test exception");
            });

            var token = TestEvents.SubscribeTestGeneratedEvent(handler);

            // Act
            var exception = Record.Exception(() =>
            {
                TestEvents.PublishTestGeneratedEvent("test", 42);
            });

            // Assert - 类级别的和全局的 OnException 都应该被调用
            exception.Should().BeNull();
            classHandlerCalled.Should().BeTrue();
            globalHandlerCalled.Should().BeTrue();

            // Cleanup
            token.Dispose();
        }
        finally
        {
            TestEvents.OnException = originalClassHandler;
            EventDispatcher.OnException = originalGlobalHandler;
        }
    }
}
