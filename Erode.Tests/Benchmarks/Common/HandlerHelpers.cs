using System.Runtime.CompilerServices;

namespace Erode.Tests.Benchmarks.Common;

/// <summary>
/// Handler 辅助类
/// 使用 Interlocked Sink 防止 JIT 优化
/// </summary>
internal static class HandlerHelpers
{
    /// <summary>
    /// 数据落点，使用 Interlocked 操作防止 JIT 优化掉整个调用链
    /// </summary>
    private static long _sink = 0;

    /// <summary>
    /// 读取 Sink 值
    /// </summary>
    internal static long ReadSink()
    {
        return Interlocked.Read(ref _sink);
    }

    /// <summary>
    /// 写入 Sink 值
    /// </summary>
    internal static void WriteSink(long value)
    {
        Interlocked.Exchange(ref _sink, value);
    }

    /// <summary>
    /// 重置 Sink
    /// </summary>
    public static void ResetSink()
    {
        Interlocked.Exchange(ref _sink, 0);
    }

    // ========== SignalEvent Handlers ==========

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static InAction<Frameworks.EventTypes.ErodeEventTypes.SignalEvent> CreateSignalEventHandler(int id)
    {
        return (in Frameworks.EventTypes.ErodeEventTypes.SignalEvent evt) =>
        {
            // 信号型事件无数据，仅通知
            // 业务逻辑：记录事件发生
            var result = id * 1000L + 1;
            WriteSink(result);
        };
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Action<Frameworks.EventTypes.PrismEventTypes.PrismSignalEventData> CreatePrismSignalEventHandler(int id)
    {
        return (Frameworks.EventTypes.PrismEventTypes.PrismSignalEventData evt) =>
        {
            // 与 Erode 完全一致的逻辑
            var result = id * 1000L + 1;
            WriteSink(result);
        };
    }

    // ========== SingleValueEvent Handlers ==========

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static InAction<Frameworks.EventTypes.ErodeEventTypes.SingleValueEvent> CreateSingleValueEventHandler(int id)
    {
        return (in Frameworks.EventTypes.ErodeEventTypes.SingleValueEvent evt) =>
        {
            // 业务逻辑：计算值的哈希并记录
            var value = evt.Value;
            var result = (value ^ id) * 1000L + value;
            WriteSink(result);
        };
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Action<Frameworks.EventTypes.PrismEventTypes.PrismSingleValueEventData> CreatePrismSingleValueEventHandler(int id)
    {
        return (Frameworks.EventTypes.PrismEventTypes.PrismSingleValueEventData evt) =>
        {
            // 与 Erode 完全一致的逻辑
            var value = evt.Value;
            var result = (value ^ id) * 1000L + value;
            WriteSink(result);
        };
    }

    // ========== CombatDataEvent Handlers ==========

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static InAction<Frameworks.EventTypes.ErodeEventTypes.CombatDataEvent> CreateCombatDataEventHandler(int id)
    {
        return (in Frameworks.EventTypes.ErodeEventTypes.CombatDataEvent evt) =>
        {
            // 业务逻辑：计算战斗力的综合评分
            var health = evt.Health;
            var attack = evt.Attack;
            var defense = evt.Defense;
            var x = evt.X;
            var y = evt.Y;
            var z = evt.Z;
            var isAlive = evt.IsAlive;

            // 计算综合战斗力
            var combatPower = health + attack * 2 + defense;
            var positionHash = (long)(x * 1000 + y * 100 + z);
            var result = (combatPower ^ id) * 1000L + positionHash + (isAlive ? 1 : 0);
            WriteSink(result);
        };
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Action<Frameworks.EventTypes.PrismEventTypes.PrismCombatDataEventData> CreatePrismCombatDataEventHandler(int id)
    {
        return (Frameworks.EventTypes.PrismEventTypes.PrismCombatDataEventData evt) =>
        {
            // 与 Erode 完全一致的逻辑
            var health = evt.Health;
            var attack = evt.Attack;
            var defense = evt.Defense;
            var x = evt.X;
            var y = evt.Y;
            var z = evt.Z;
            var isAlive = evt.IsAlive;

            var combatPower = health + attack * 2 + defense;
            var positionHash = (long)(x * 1000 + y * 100 + z);
            var result = (combatPower ^ id) * 1000L + positionHash + (isAlive ? 1 : 0);
            WriteSink(result);
        };
    }

    // ========== MixedBusinessDataEvent Handlers ==========

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static InAction<Frameworks.EventTypes.ErodeEventTypes.MixedBusinessDataEvent> CreateMixedBusinessDataEventHandler(int id)
    {
        return (in Frameworks.EventTypes.ErodeEventTypes.MixedBusinessDataEvent evt) =>
        {
            // 业务逻辑：计算业务数据的综合哈希
            var sessionId = evt.SessionId;
            var timestamp = evt.Timestamp;
            var createdAt = evt.CreatedAt;
            var userName = evt.UserName;
            var player = evt.Player;
            var eventId = evt.Id;

            // 计算综合哈希
            var sessionHash = sessionId.GetHashCode();
            var timeHash = timestamp ^ createdAt.Ticks;
            var userHash = userName?.GetHashCode() ?? 0;
            var playerHash = player?.Id ?? 0;
            var result = ((long)sessionHash ^ id) * 1000L + timeHash + userHash + playerHash + eventId;
            WriteSink(result);
        };
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Action<Frameworks.EventTypes.PrismEventTypes.PrismMixedBusinessDataEventData> CreatePrismMixedBusinessDataEventHandler(int id)
    {
        return (Frameworks.EventTypes.PrismEventTypes.PrismMixedBusinessDataEventData evt) =>
        {
            // 与 Erode 完全一致的逻辑
            var sessionId = evt.SessionId;
            var timestamp = evt.Timestamp;
            var createdAt = evt.CreatedAt;
            var userName = evt.UserName;
            var player = evt.Player;
            var eventId = evt.Id;

            var sessionHash = sessionId.GetHashCode();
            var timeHash = timestamp ^ createdAt.Ticks;
            var userHash = userName?.GetHashCode() ?? 0;
            var playerHash = player?.Id ?? 0;
            var result = ((long)sessionHash ^ id) * 1000L + timeHash + userHash + playerHash + eventId;
            WriteSink(result);
        };
    }

    // ========== HeavyPayloadEvent Handlers ==========

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static InAction<Frameworks.EventTypes.ErodeEventTypes.HeavyPayloadEvent> CreateHeavyPayloadEventHandler(int id)
    {
        return (in Frameworks.EventTypes.ErodeEventTypes.HeavyPayloadEvent evt) =>
        {
            // 业务逻辑：计算重负载数据的综合哈希
            var eventId = evt.EventId;
            var timestamp = evt.Timestamp;
            var value1 = evt.Value1;
            var value2 = evt.Value2;
            var createdAt = evt.CreatedAt;
            var name = evt.Name;
            var description = evt.Description;
            var category = evt.Category;
            var tags = evt.Tags;
            var metadata = evt.Metadata;
            var player = evt.Player;
            var idField = evt.Id;
            var x = evt.X;
            var y = evt.Y;
            var z = evt.Z;
            var flag1 = evt.Flag1;
            var flag2 = evt.Flag2;
            var flag3 = evt.Flag3;
            var flag4 = evt.Flag4;
            var flag5 = evt.Flag5;

            // 计算综合哈希
            var eventIdHash = eventId.GetHashCode();
            var timeHash = timestamp ^ createdAt.Ticks;
            var valueHash = (long)(value1 * 1000 + value2 * 100);
            var nameHash = name?.GetHashCode() ?? 0;
            var descHash = description?.GetHashCode() ?? 0;
            var catHash = category?.GetHashCode() ?? 0;
            var tagsHash = tags?.Count ?? 0;
            var metadataHash = metadata?.Count ?? 0;
            var playerHash = player?.Id ?? 0;
            var positionHash = (long)(x * 1000 + y * 100 + z);
            var flagsHash = (flag1 ? 1 : 0) + (flag2 ? 2 : 0) + (flag3 ? 4 : 0) + (flag4 ? 8 : 0) + (flag5 ? 16 : 0);

            var result = ((long)eventIdHash ^ id) * 1000L + timeHash + valueHash + nameHash + descHash + catHash + tagsHash + metadataHash + playerHash + positionHash + flagsHash + idField;
            WriteSink(result);
        };
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Action<Frameworks.EventTypes.PrismEventTypes.PrismHeavyPayloadEventData> CreatePrismHeavyPayloadEventHandler(int id)
    {
        return (Frameworks.EventTypes.PrismEventTypes.PrismHeavyPayloadEventData evt) =>
        {
            // 与 Erode 完全一致的逻辑
            var eventId = evt.EventId;
            var timestamp = evt.Timestamp;
            var value1 = evt.Value1;
            var value2 = evt.Value2;
            var createdAt = evt.CreatedAt;
            var name = evt.Name;
            var description = evt.Description;
            var category = evt.Category;
            var tags = evt.Tags;
            var metadata = evt.Metadata;
            var player = evt.Player;
            var idField = evt.Id;
            var x = evt.X;
            var y = evt.Y;
            var z = evt.Z;
            var flag1 = evt.Flag1;
            var flag2 = evt.Flag2;
            var flag3 = evt.Flag3;
            var flag4 = evt.Flag4;
            var flag5 = evt.Flag5;

            var eventIdHash = eventId.GetHashCode();
            var timeHash = timestamp ^ createdAt.Ticks;
            var valueHash = (long)(value1 * 1000 + value2 * 100);
            var nameHash = name?.GetHashCode() ?? 0;
            var descHash = description?.GetHashCode() ?? 0;
            var catHash = category?.GetHashCode() ?? 0;
            var tagsHash = tags?.Count ?? 0;
            var metadataHash = metadata?.Count ?? 0;
            var playerHash = player?.Id ?? 0;
            var positionHash = (long)(x * 1000 + y * 100 + z);
            var flagsHash = (flag1 ? 1 : 0) + (flag2 ? 2 : 0) + (flag3 ? 4 : 0) + (flag4 ? 8 : 0) + (flag5 ? 16 : 0);

            var result = ((long)eventIdHash ^ id) * 1000L + timeHash + valueHash + nameHash + descHash + catHash + tagsHash + metadataHash + playerHash + positionHash + flagsHash + idField;
            WriteSink(result);
        };
    }
}

