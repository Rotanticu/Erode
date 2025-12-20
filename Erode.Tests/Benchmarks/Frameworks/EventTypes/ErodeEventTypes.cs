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

    // ========== 测试隔离专用事件类型 ==========
    // 使用不同的事件类型实现静态隔离，避免测试之间的订阅污染

    /// <summary>
    /// 无订阅者测试专用事件（静态隔离）
    /// </summary>
    public readonly record struct SignalEvent_Empty : IEvent;

    /// <summary>
    /// 多订阅者测试专用事件（静态隔离）
    /// </summary>
    public readonly record struct SignalEvent_Multi : IEvent;

    /// <summary>
    /// 单订阅者测试专用事件（静态隔离）
    /// </summary>
    public readonly record struct SignalEvent_Single : IEvent;

    /// <summary>
    /// 单参数事件
    /// </summary>
    public readonly record struct SingleValueEvent(long Value) : IEvent;

    // ========== SingleValueEvent 测试隔离专用事件类型 ==========

    /// <summary>
    /// 无订阅者测试专用事件（静态隔离）
    /// </summary>
    public readonly record struct SingleValueEvent_Empty(long Value) : IEvent;

    /// <summary>
    /// 多订阅者测试专用事件（静态隔离）
    /// </summary>
    public readonly record struct SingleValueEvent_Multi(long Value) : IEvent;

    /// <summary>
    /// 单订阅者测试专用事件（静态隔离）
    /// </summary>
    public readonly record struct SingleValueEvent_Single(long Value) : IEvent;

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

    // ========== CombatDataEvent 测试隔离专用事件类型 ==========

    /// <summary>
    /// 无订阅者测试专用事件（静态隔离）
    /// </summary>
    public readonly record struct CombatDataEvent_Empty(
        int Health,
        int Attack,
        int Defense,
        float X,
        float Y,
        float Z,
        bool IsAlive
    ) : IEvent;

    /// <summary>
    /// 多订阅者测试专用事件（静态隔离）
    /// </summary>
    public readonly record struct CombatDataEvent_Multi(
        int Health,
        int Attack,
        int Defense,
        float X,
        float Y,
        float Z,
        bool IsAlive
    ) : IEvent;

    /// <summary>
    /// 单订阅者测试专用事件（静态隔离）
    /// </summary>
    public readonly record struct CombatDataEvent_Single(
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

    // ========== MixedBusinessDataEvent 测试隔离专用事件类型 ==========

    /// <summary>
    /// 无订阅者测试专用事件（静态隔离）
    /// </summary>
    public readonly record struct MixedBusinessDataEvent_Empty(
        Guid SessionId,
        long Timestamp,
        DateTime CreatedAt,
        string UserName,
        Common.SharedTestTypes.PlayerInfo Player,
        int Id
    ) : IEvent;

    /// <summary>
    /// 多订阅者测试专用事件（静态隔离）
    /// </summary>
    public readonly record struct MixedBusinessDataEvent_Multi(
        Guid SessionId,
        long Timestamp,
        DateTime CreatedAt,
        string UserName,
        Common.SharedTestTypes.PlayerInfo Player,
        int Id
    ) : IEvent;

    /// <summary>
    /// 单订阅者测试专用事件（静态隔离）
    /// </summary>
    public readonly record struct MixedBusinessDataEvent_Single(
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

    // ========== HeavyPayloadEvent 测试隔离专用事件类型 ==========

    /// <summary>
    /// 无订阅者测试专用事件（静态隔离）
    /// </summary>
    public readonly record struct HeavyPayloadEvent_Empty(
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

    /// <summary>
    /// 多订阅者测试专用事件（静态隔离）
    /// </summary>
    public readonly record struct HeavyPayloadEvent_Multi(
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

    /// <summary>
    /// 单订阅者测试专用事件（静态隔离）
    /// </summary>
    public readonly record struct HeavyPayloadEvent_Single(
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

