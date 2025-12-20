namespace Erode.Tests.Benchmarks.Frameworks.EventTypes;

/// <summary>
/// Prism EventAggregator 事件类型定义
/// </summary>
public static class PrismEventTypes
{
    /// <summary>
    /// 信号型事件
    /// </summary>
    public class PrismSignalEvent : PubSubEvent<PrismSignalEventData>
    {
    }

    public class PrismSignalEventData
    {
    }

    /// <summary>
    /// 单参数事件
    /// </summary>
    public class PrismSingleValueEvent : PubSubEvent<PrismSingleValueEventData>
    {
    }

    public class PrismSingleValueEventData
    {
        public long Value { get; set; }
    }

    /// <summary>
    /// 标准战斗数据事件
    /// </summary>
    public class PrismCombatDataEvent : PubSubEvent<PrismCombatDataEventData>
    {
    }

    public class PrismCombatDataEventData
    {
        public bool IsAlive { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public int Health { get; set; }
        public int Attack { get; set; }
        public int Defense { get; set; }
    }

    /// <summary>
    /// 混合业务数据事件
    /// </summary>
    public class PrismMixedBusinessDataEvent : PubSubEvent<PrismMixedBusinessDataEventData>
    {
    }

    public class PrismMixedBusinessDataEventData
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public long Timestamp { get; set; }
        public Common.SharedTestTypes.PlayerInfo Player { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public Guid SessionId { get; set; }
    }

    /// <summary>
    /// 重负载数据事件
    /// </summary>
    public class PrismHeavyPayloadEvent : PubSubEvent<PrismHeavyPayloadEventData>
    {
    }

    public class PrismHeavyPayloadEventData
    {
        public int Id { get; set; }
        public bool Flag1 { get; set; }
        public bool Flag2 { get; set; }
        public bool Flag3 { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public long Timestamp { get; set; }
        public DateTime CreatedAt { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public double Value1 { get; set; }
        public double Value2 { get; set; }
        public bool Flag4 { get; set; }
        public bool Flag5 { get; set; }
        public Guid EventId { get; set; }
        public List<int> Tags { get; set; } = new();
        public Dictionary<string, int> Metadata { get; set; } = new();
        public Common.SharedTestTypes.PlayerInfo Player { get; set; } = null!;
    }
}

