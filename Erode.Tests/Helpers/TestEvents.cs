namespace Erode.Tests.Helpers;

/// <summary>
/// 基本测试事件
/// </summary>
public readonly record struct TestEvent : IEvent;

/// <summary>
/// 带数据的测试事件
/// </summary>
public readonly record struct TestEventWithData(string Message, int Value) : IEvent;

/// <summary>
/// 测试用事件定义（用于 Source Generator）
/// </summary>
public interface ITestEvents
{
    /// <summary>
    /// 使用 Source Generator 生成的事件
    /// </summary>
    [GenerateEvent]
    void PublishTestGeneratedEvent(string message, int value);
}

