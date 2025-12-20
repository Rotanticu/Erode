namespace Erode.Tests.Helpers;

/// <summary>
/// 用于测试结构约束的事件类型
/// </summary>
public readonly record struct SimpleEvent : IEvent;

/// <summary>
/// 包含多个值类型字段的事件
/// </summary>
public readonly record struct EventWithValueTypes(int X, int Y, float Z) : IEvent;

/// <summary>
/// 包含 readonly struct 字段的事件
/// </summary>
public readonly struct ReadonlyStructField
{
    public readonly int Value;

    public ReadonlyStructField(int value)
    {
        Value = value;
    }
}

public readonly record struct EventWithReadonlyStructField(ReadonlyStructField Field) : IEvent;

/// <summary>
/// 大型结构体事件（用于测试 in 参数传递）
/// </summary>
public readonly record struct LargeStructEvent(
    int Field1, int Field2, int Field3, int Field4, int Field5,
    int Field6, int Field7, int Field8, int Field9, int Field10,
    int Field11, int Field12, int Field13, int Field14, int Field15,
    int Field16, int Field17, int Field18, int Field19, int Field20
) : IEvent;

/// <summary>
/// 用于测试不同事件类型互不干扰
/// </summary>
public readonly record struct AnotherTestEvent(string Data) : IEvent;

/// <summary>
/// 用于测试订阅顺序的事件
/// </summary>
public readonly record struct OrderTestEvent(int Order) : IEvent;
