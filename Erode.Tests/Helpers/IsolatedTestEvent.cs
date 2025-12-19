using Erode;

namespace Erode.Tests.Helpers;

/// <summary>
/// 用于测试隔离的独立事件类型
/// </summary>
public readonly record struct IsolatedTestEvent(int Value) : IEvent;

/// <summary>
/// 用于异常测试的独立事件类型
/// </summary>
public readonly record struct ExceptionTestEvent(int Value) : IEvent;

/// <summary>
/// 用于单个异常测试的独立事件类型
/// </summary>
public readonly record struct SingleExceptionTestEvent(int Value) : IEvent;

/// <summary>
/// 用于多个异常测试的独立事件类型
/// </summary>
public readonly record struct MultipleExceptionTestEvent(int Value) : IEvent;

/// <summary>
/// 用于 PublishTests 的独立事件类型
/// </summary>
public readonly record struct PublishTestEvent : IEvent;

/// <summary>
/// 用于 EdgeCaseTests 的独立事件类型
/// </summary>
public readonly record struct EdgeCaseTestEvent : IEvent;

/// <summary>
/// 用于 IntegrationTests 的独立事件类型
/// </summary>
public readonly record struct IntegrationTestEvent : IEvent;

/// <summary>
/// 用于 ThreadSafetyTests 的独立事件类型
/// </summary>
public readonly record struct ThreadSafetyTestEvent : IEvent;

/// <summary>
/// 用于 EventDispatcherUnsubscribeTests 的独立事件类型
/// </summary>
public readonly record struct UnsubscribeTestEvent : IEvent;

/// <summary>
/// 用于 EventDispatcherUnsubscribeTests 的另一个独立事件类型（用于跨调度器测试）
/// </summary>
public readonly record struct UnsubscribeTestEvent2 : IEvent;

/// <summary>
/// 用于 EventDispatcherSubscribePublishTests 的独立事件类型（测试订阅顺序）
/// </summary>
public readonly record struct OrderTestEventIsolated(int Order) : IEvent;

/// <summary>
/// 用于 LifecycleTests 的独立事件类型
/// </summary>
public readonly record struct LifecycleTestEvent : IEvent;

