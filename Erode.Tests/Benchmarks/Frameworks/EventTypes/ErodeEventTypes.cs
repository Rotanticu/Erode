namespace Erode.Tests.Benchmarks.Frameworks.EventTypes;

/// <summary>
/// Erode 事件类型定义
/// </summary>
public static class ErodeEventTypes
{
    /// <summary>
    /// 信号型事件
    /// 无数据，仅通知事件发生
    /// </summary>
    public readonly record struct SignalEvent : IEvent;

    /// <summary>
    /// 单参数事件
    /// </summary>
    public readonly record struct SingleValueEvent(long Value) : IEvent;

    /// <summary>
    /// 标准战斗数据事件
    /// </summary>
    public readonly record struct CombatDataEvent(
        int Health,
        int Attack,
        int Defense,
        float X,
        float Y,
        float Z,
        bool IsAlive
    ) : IEvent;

    /// <summary>
    /// 混合业务数据事件
    /// </summary>
    public readonly record struct MixedBusinessDataEvent(
        Guid SessionId,
        long Timestamp,
        DateTime CreatedAt,
        string UserName,
        Common.SharedTestTypes.PlayerInfo Player,
        int Id
    ) : IEvent;

    /// <summary>
    /// 重负载数据事件
    /// </summary>
    public readonly record struct HeavyPayloadEvent(
        Guid EventId,
        long Timestamp,
        double Value1,
        double Value2,
        DateTime CreatedAt,
        string Name,
        string Description,
        string Category,
        List<int> Tags,
        Dictionary<string, int> Metadata,
        Common.SharedTestTypes.PlayerInfo Player,
        int Id,
        float X,
        float Y,
        float Z,
        bool Flag1,
        bool Flag2,
        bool Flag3,
        bool Flag4,
        bool Flag5
    ) : IEvent;
}

