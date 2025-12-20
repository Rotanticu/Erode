using System.Runtime.CompilerServices;
using System.Threading;
using Erode.Tests.Benchmarks.Frameworks.EventTypes;

namespace Erode.Tests.Benchmarks.Common;

// ========== TestHandler 类：用于消除闭包分配 ==========

/// <summary>
/// SignalEvent 的 TestHandler，使用成员函数避免闭包分配
/// </summary>
internal sealed class SignalEventHandler
{
    private readonly int _id;

    public SignalEventHandler(int id)
    {
        _id = id;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void OnEvent(in SignalEvent evt)
    {
        // 信号型事件无数据，仅通知
        // 业务逻辑：记录事件发生
        var result = _id * 1000L + 1;
        HandlerHelpers.AddToSink(result);
    }
}

/// <summary>
/// SignalEvent_Empty 的 TestHandler
/// </summary>
internal sealed class SignalEventEmptyHandler
{
    private readonly int _id;

    public SignalEventEmptyHandler(int id)
    {
        _id = id;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void OnEvent(in SignalEmptyEvent evt)
    {
        var result = _id * 1000L + 1;
        HandlerHelpers.AddToSink(result);
    }
}

/// <summary>
/// SignalEvent_Multi 的 TestHandler
/// </summary>
internal sealed class SignalEventMultiHandler
{
    private readonly int _id;

    public SignalEventMultiHandler(int id)
    {
        _id = id;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void OnEvent(in SignalMultiEvent evt)
    {
        var result = _id * 1000L + 1;
        HandlerHelpers.AddToSink(result);
    }
}

/// <summary>
/// SignalEvent_Single 的 TestHandler
/// </summary>
internal sealed class SignalEventSingleHandler
{
    private readonly int _id;

    public SignalEventSingleHandler(int id)
    {
        _id = id;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void OnEvent(in SignalSingleEvent evt)
    {
        var result = _id * 1000L + 1;
        HandlerHelpers.AddToSink(result);
    }
}

/// <summary>
/// PrismSignalEventData 的 TestHandler
/// </summary>
internal sealed class PrismSignalEventHandler
{
    private readonly int _id;

    public PrismSignalEventHandler(int id)
    {
        _id = id;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void OnEvent(Frameworks.EventTypes.PrismEventTypes.PrismSignalEventData evt)
    {
        // 与 Erode 完全一致的逻辑
        var result = _id * 1000L + 1;
        HandlerHelpers.AddToSink(result);
    }
}

/// <summary>
/// SingleValueEvent 的 TestHandler（支持所有 SingleValue 变体）
/// </summary>
internal sealed class SingleValueEventHandler
{
    private readonly int _id;

    public SingleValueEventHandler(int id)
    {
        _id = id;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void OnEvent(in SingleValueEvent evt)
    {
        // 业务逻辑：计算值的哈希并记录
        var value = evt.Value;
        var result = (value ^ _id) * 1000L + value;
        HandlerHelpers.AddToSink(result);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void OnEvent(in SingleValueMultiEvent evt)
    {
        var value = evt.Value;
        var result = (value ^ _id) * 1000L + value;
        HandlerHelpers.AddToSink(result);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void OnEvent(in SingleValueSingleEvent evt)
    {
        var value = evt.Value;
        var result = (value ^ _id) * 1000L + value;
        HandlerHelpers.AddToSink(result);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void OnEvent(in SingleValueEmptyEvent evt)
    {
        var value = evt.Value;
        var result = (value ^ _id) * 1000L + value;
        HandlerHelpers.AddToSink(result);
    }
}

/// <summary>
/// PrismSingleValueEventData 的 TestHandler
/// </summary>
internal sealed class PrismSingleValueEventHandler
{
    private readonly int _id;

    public PrismSingleValueEventHandler(int id)
    {
        _id = id;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void OnEvent(Frameworks.EventTypes.PrismEventTypes.PrismSingleValueEventData evt)
    {
        // 与 Erode 完全一致的逻辑
        var value = evt.Value;
        var result = (value ^ _id) * 1000L + value;
        HandlerHelpers.AddToSink(result);
    }
}

/// <summary>
/// CombatDataEvent 的 TestHandler（支持所有 CombatData 变体）
/// </summary>
internal sealed class CombatDataEventHandler
{
    private readonly int _id;

    public CombatDataEventHandler(int id)
    {
        _id = id;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void OnEvent(in CombatDataEvent evt)
    {
        ProcessCombatData(evt.Health, evt.Attack, evt.Defense, evt.X, evt.Y, evt.Z, evt.IsAlive);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void OnEvent(in CombatDataMultiEvent evt)
    {
        ProcessCombatData(evt.Health, evt.Attack, evt.Defense, evt.X, evt.Y, evt.Z, evt.IsAlive);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void OnEvent(in CombatDataSingleEvent evt)
    {
        ProcessCombatData(evt.Health, evt.Attack, evt.Defense, evt.X, evt.Y, evt.Z, evt.IsAlive);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void OnEvent(in CombatDataEmptyEvent evt)
    {
        ProcessCombatData(evt.Health, evt.Attack, evt.Defense, evt.X, evt.Y, evt.Z, evt.IsAlive);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ProcessCombatData(int health, int attack, int defense, float x, float y, float z, bool isAlive)
    {
        var combatPower = health + attack * 2 + defense;
        var positionHash = (long)(x * 1000 + y * 100 + z);
        var result = (combatPower ^ _id) * 1000L + positionHash + (isAlive ? 1 : 0);
        HandlerHelpers.AddToSink(result);
    }
}

/// <summary>
/// PrismCombatDataEventData 的 TestHandler
/// </summary>
internal sealed class PrismCombatDataEventHandler
{
    private readonly int _id;

    public PrismCombatDataEventHandler(int id)
    {
        _id = id;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void OnEvent(Frameworks.EventTypes.PrismEventTypes.PrismCombatDataEventData evt)
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
        var result = (combatPower ^ _id) * 1000L + positionHash + (isAlive ? 1 : 0);
        HandlerHelpers.AddToSink(result);
    }
}

/// <summary>
/// MixedBusinessDataEvent 的 TestHandler（支持所有 MixedBusinessData 变体）
/// </summary>
internal sealed class MixedBusinessDataEventHandler
{
    private readonly int _id;

    public MixedBusinessDataEventHandler(int id)
    {
        _id = id;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void OnEvent(in MixedBusinessDataEvent evt)
    {
        ProcessMixedBusinessData(evt.SessionId, evt.Timestamp, evt.CreatedAt, evt.UserName, evt.Player, evt.Id);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void OnEvent(in MixedBusinessDataMultiEvent evt)
    {
        ProcessMixedBusinessData(evt.SessionId, evt.Timestamp, evt.CreatedAt, evt.UserName, evt.Player, evt.Id);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void OnEvent(in MixedBusinessDataSingleEvent evt)
    {
        ProcessMixedBusinessData(evt.SessionId, evt.Timestamp, evt.CreatedAt, evt.UserName, evt.Player, evt.Id);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void OnEvent(in MixedBusinessDataEmptyEvent evt)
    {
        ProcessMixedBusinessData(evt.SessionId, evt.Timestamp, evt.CreatedAt, evt.UserName, evt.Player, evt.Id);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ProcessMixedBusinessData(Guid sessionId, long timestamp, DateTime createdAt, string userName, Common.SharedTestTypes.PlayerInfo player, int eventId)
    {
        var sessionHash = sessionId.GetHashCode();
        var timeHash = timestamp ^ createdAt.Ticks;
        var userHash = userName?.GetHashCode() ?? 0;
        var playerHash = player?.Id ?? 0;
        var result = ((long)sessionHash ^ _id) * 1000L + timeHash + userHash + playerHash + eventId;
        HandlerHelpers.AddToSink(result);
    }
}

/// <summary>
/// PrismMixedBusinessDataEventData 的 TestHandler
/// </summary>
internal sealed class PrismMixedBusinessDataEventHandler
{
    private readonly int _id;

    public PrismMixedBusinessDataEventHandler(int id)
    {
        _id = id;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void OnEvent(Frameworks.EventTypes.PrismEventTypes.PrismMixedBusinessDataEventData evt)
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
        var result = ((long)sessionHash ^ _id) * 1000L + timeHash + userHash + playerHash + eventId;
        HandlerHelpers.AddToSink(result);
    }
}

/// <summary>
/// HeavyPayloadEvent 的 TestHandler（支持所有 HeavyPayload 变体）
/// </summary>
internal sealed class HeavyPayloadEventHandler
{
    private readonly int _id;

    public HeavyPayloadEventHandler(int id)
    {
        _id = id;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void OnEvent(in HeavyPayloadEvent evt)
    {
        ProcessHeavyPayload(evt.EventId, evt.Timestamp, evt.Value1, evt.Value2, evt.CreatedAt, evt.Name, evt.Description, evt.Category, evt.Tags, evt.Metadata, evt.Player, evt.Id, evt.X, evt.Y, evt.Z, evt.Flag1, evt.Flag2, evt.Flag3, evt.Flag4, evt.Flag5);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void OnEvent(in HeavyPayloadMultiEvent evt)
    {
        ProcessHeavyPayload(evt.EventId, evt.Timestamp, evt.Value1, evt.Value2, evt.CreatedAt, evt.Name, evt.Description, evt.Category, evt.Tags, evt.Metadata, evt.Player, evt.Id, evt.X, evt.Y, evt.Z, evt.Flag1, evt.Flag2, evt.Flag3, evt.Flag4, evt.Flag5);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void OnEvent(in HeavyPayloadSingleEvent evt)
    {
        ProcessHeavyPayload(evt.EventId, evt.Timestamp, evt.Value1, evt.Value2, evt.CreatedAt, evt.Name, evt.Description, evt.Category, evt.Tags, evt.Metadata, evt.Player, evt.Id, evt.X, evt.Y, evt.Z, evt.Flag1, evt.Flag2, evt.Flag3, evt.Flag4, evt.Flag5);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void OnEvent(in HeavyPayloadEmptyEvent evt)
    {
        ProcessHeavyPayload(evt.EventId, evt.Timestamp, evt.Value1, evt.Value2, evt.CreatedAt, evt.Name, evt.Description, evt.Category, evt.Tags, evt.Metadata, evt.Player, evt.Id, evt.X, evt.Y, evt.Z, evt.Flag1, evt.Flag2, evt.Flag3, evt.Flag4, evt.Flag5);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ProcessHeavyPayload(Guid eventId, long timestamp, double value1, double value2, DateTime createdAt, string name, string description, string category, List<int> tags, Dictionary<string, int> metadata, Common.SharedTestTypes.PlayerInfo player, int idField, float x, float y, float z, bool flag1, bool flag2, bool flag3, bool flag4, bool flag5)
    {
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

        var result = ((long)eventIdHash ^ _id) * 1000L + timeHash + valueHash + nameHash + descHash + catHash + tagsHash + metadataHash + playerHash + positionHash + flagsHash + idField;
        HandlerHelpers.AddToSink(result);
    }
}

/// <summary>
/// PrismHeavyPayloadEventData 的 TestHandler
/// </summary>
internal sealed class PrismHeavyPayloadEventHandler
{
    private readonly int _id;

    public PrismHeavyPayloadEventHandler(int id)
    {
        _id = id;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void OnEvent(Frameworks.EventTypes.PrismEventTypes.PrismHeavyPayloadEventData evt)
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

        var result = ((long)eventIdHash ^ _id) * 1000L + timeHash + valueHash + nameHash + descHash + catHash + tagsHash + metadataHash + playerHash + positionHash + flagsHash + idField;
        HandlerHelpers.AddToSink(result);
    }
}

/// <summary>
/// Handler 辅助类
/// 使用 volatile 语义防止 JIT 优化，比 Interlocked 轻量得多
/// </summary>
internal static class HandlerHelpers
{
    /// <summary>
    /// 数据落点，使用 Thread.VolatileWrite/Read 防止 JIT 优化掉整个调用链
    /// 比 Interlocked 轻量得多（约 1-2ns vs 10-20ns），更适合纳秒级性能测试
    /// </summary>
    private static long _sink = 0;

    /// <summary>
    /// 读取 Sink 值（使用 VolatileRead 防止 JIT 优化）
    /// </summary>
    internal static long ReadSink()
    {
        return Thread.VolatileRead(ref _sink);
    }

    /// <summary>
    /// 累加到 Sink（用于 Handler，防止 JIT 优化）
    /// 使用 VolatileRead + 累加 + VolatileWrite，比 Interlocked 轻量得多（约 1-2ns vs 10-20ns）
    /// 累加操作确保包含运算，防止 JIT 优化掉整个计算过程
    /// Benchmark 在单线程环境执行，volatile 语义足够防止 JIT 优化
    /// </summary>
    internal static void AddToSink(long value)
    {
        var current = Thread.VolatileRead(ref _sink);
        Thread.VolatileWrite(ref _sink, current + value);
    }

    /// <summary>
    /// 重置 Sink（仅在 Benchmark 方法开始时调用，单线程环境可使用普通赋值）
    /// </summary>
    public static void ResetSink()
    {
        // Benchmark 方法在单线程环境中执行，可以使用普通赋值
        _sink = 0;
    }

    // ========== SignalEvent Handlers ==========

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static InAction<SignalEvent> CreateSignalEventHandler(int id)
    {
        var handler = new SignalEventHandler(id);
        return handler.OnEvent; // 返回成员方法委托，无闭包分配
    }

    // ========== 测试隔离专用事件类型的 Handlers ==========

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static InAction<SignalEmptyEvent> CreateSignalEventEmptyHandler(int id)
    {
        var handler = new SignalEventEmptyHandler(id);
        return handler.OnEvent; // 返回成员方法委托，无闭包分配
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static InAction<SignalMultiEvent> CreateSignalEventMultiHandler(int id)
    {
        var handler = new SignalEventMultiHandler(id);
        return handler.OnEvent; // 返回成员方法委托，无闭包分配
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static InAction<SignalSingleEvent> CreateSignalEventSingleHandler(int id)
    {
        var handler = new SignalEventSingleHandler(id);
        return handler.OnEvent; // 返回成员方法委托，无闭包分配
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Action<Frameworks.EventTypes.PrismEventTypes.PrismSignalEventData> CreatePrismSignalEventHandler(int id)
    {
        var handler = new PrismSignalEventHandler(id);
        return handler.OnEvent; // 返回成员方法委托，无闭包分配
    }

    // ========== SingleValueEvent Handlers ==========

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static InAction<SingleValueEvent> CreateSingleValueEventHandler(int id)
    {
        var handler = new SingleValueEventHandler(id);
        return handler.OnEvent; // 返回成员方法委托，无闭包分配
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static InAction<SingleValueMultiEvent> CreateSingleValueMultiEventHandler(int id)
    {
        var handler = new SingleValueEventHandler(id);
        return handler.OnEvent; // 返回成员方法委托，无闭包分配
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static InAction<SingleValueSingleEvent> CreateSingleValueSingleEventHandler(int id)
    {
        var handler = new SingleValueEventHandler(id);
        return handler.OnEvent; // 返回成员方法委托，无闭包分配
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static InAction<SingleValueEmptyEvent> CreateSingleValueEmptyEventHandler(int id)
    {
        var handler = new SingleValueEventHandler(id);
        return handler.OnEvent; // 返回成员方法委托，无闭包分配
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Action<Frameworks.EventTypes.PrismEventTypes.PrismSingleValueEventData> CreatePrismSingleValueEventHandler(int id)
    {
        var handler = new PrismSingleValueEventHandler(id);
        return handler.OnEvent; // 返回成员方法委托，无闭包分配
    }

    // ========== CombatDataEvent Handlers ==========

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static InAction<CombatDataEvent> CreateCombatDataEventHandler(int id)
    {
        var handler = new CombatDataEventHandler(id);
        return handler.OnEvent; // 返回成员方法委托，无闭包分配
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static InAction<CombatDataMultiEvent> CreateCombatDataMultiEventHandler(int id)
    {
        var handler = new CombatDataEventHandler(id);
        return handler.OnEvent; // 返回成员方法委托，无闭包分配
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static InAction<CombatDataSingleEvent> CreateCombatDataSingleEventHandler(int id)
    {
        var handler = new CombatDataEventHandler(id);
        return handler.OnEvent; // 返回成员方法委托，无闭包分配
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static InAction<CombatDataEmptyEvent> CreateCombatDataEmptyEventHandler(int id)
    {
        var handler = new CombatDataEventHandler(id);
        return handler.OnEvent; // 返回成员方法委托，无闭包分配
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Action<Frameworks.EventTypes.PrismEventTypes.PrismCombatDataEventData> CreatePrismCombatDataEventHandler(int id)
    {
        var handler = new PrismCombatDataEventHandler(id);
        return handler.OnEvent; // 返回成员方法委托，无闭包分配
    }

    // ========== MixedBusinessDataEvent Handlers ==========

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static InAction<MixedBusinessDataEvent> CreateMixedBusinessDataEventHandler(int id)
    {
        var handler = new MixedBusinessDataEventHandler(id);
        return handler.OnEvent; // 返回成员方法委托，无闭包分配
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static InAction<MixedBusinessDataMultiEvent> CreateMixedBusinessDataMultiEventHandler(int id)
    {
        var handler = new MixedBusinessDataEventHandler(id);
        return handler.OnEvent; // 返回成员方法委托，无闭包分配
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static InAction<MixedBusinessDataSingleEvent> CreateMixedBusinessDataSingleEventHandler(int id)
    {
        var handler = new MixedBusinessDataEventHandler(id);
        return handler.OnEvent; // 返回成员方法委托，无闭包分配
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static InAction<MixedBusinessDataEmptyEvent> CreateMixedBusinessDataEmptyEventHandler(int id)
    {
        var handler = new MixedBusinessDataEventHandler(id);
        return handler.OnEvent; // 返回成员方法委托，无闭包分配
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Action<Frameworks.EventTypes.PrismEventTypes.PrismMixedBusinessDataEventData> CreatePrismMixedBusinessDataEventHandler(int id)
    {
        var handler = new PrismMixedBusinessDataEventHandler(id);
        return handler.OnEvent; // 返回成员方法委托，无闭包分配
    }

    // ========== HeavyPayloadEvent Handlers ==========

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static InAction<HeavyPayloadEvent> CreateHeavyPayloadEventHandler(int id)
    {
        var handler = new HeavyPayloadEventHandler(id);
        return handler.OnEvent; // 返回成员方法委托，无闭包分配
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static InAction<HeavyPayloadMultiEvent> CreateHeavyPayloadMultiEventHandler(int id)
    {
        var handler = new HeavyPayloadEventHandler(id);
        return handler.OnEvent; // 返回成员方法委托，无闭包分配
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static InAction<HeavyPayloadSingleEvent> CreateHeavyPayloadSingleEventHandler(int id)
    {
        var handler = new HeavyPayloadEventHandler(id);
        return handler.OnEvent; // 返回成员方法委托，无闭包分配
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static InAction<HeavyPayloadEmptyEvent> CreateHeavyPayloadEmptyEventHandler(int id)
    {
        var handler = new HeavyPayloadEventHandler(id);
        return handler.OnEvent; // 返回成员方法委托，无闭包分配
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Action<Frameworks.EventTypes.PrismEventTypes.PrismHeavyPayloadEventData> CreatePrismHeavyPayloadEventHandler(int id)
    {
        var handler = new PrismHeavyPayloadEventHandler(id);
        return handler.OnEvent; // 返回成员方法委托，无闭包分配
    }
}

