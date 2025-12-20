namespace Erode.Tests.Helpers;

/// <summary>
/// 用于测试隔离的独立事件类型
/// 注意：其他事件类型现在通过 Source Generator 自动生成，请使用生成的类和方法
/// </summary>
public readonly record struct IsolatedTestEvent(int Value) : IEvent;

/// <summary>
/// 用于 EventDispatcherUnsubscribeTests 的另一个独立事件类型（用于跨调度器测试）
/// </summary>
public readonly record struct UnsubscribeTestEvent2 : IEvent;

/// <summary>
/// 用于 EventDispatcherSubscribePublishTests 的独立事件类型（测试订阅顺序）
/// </summary>
public readonly record struct OrderTestEventIsolated(int Order) : IEvent;

