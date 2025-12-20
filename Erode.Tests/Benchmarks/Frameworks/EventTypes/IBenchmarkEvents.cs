using Erode;
using Erode.Tests.Benchmarks.Common;

namespace Erode.Tests.Benchmarks.Frameworks.EventTypes;

/// <summary>
/// Benchmark 事件接口定义
/// 所有 Benchmark 用到的事件都在这个接口中定义
/// 所有事件类型通过 Source Generator 自动生成
/// </summary>
public interface IBenchmarkEvents
{
    // ========== 信号事件（无参数） ==========
    
    [GenerateEvent]
    void PublishSignalEvent();

    [GenerateEvent]
    void PublishSignalEmptyEvent();

    [GenerateEvent]
    void PublishSignalMultiEvent();

    [GenerateEvent]
    void PublishSignalSingleEvent();

    // ========== 单值事件 ==========
    
    [GenerateEvent]
    void PublishSingleValueEvent(long value);

    [GenerateEvent]
    void PublishSingleValueEmptyEvent(long value);

    [GenerateEvent]
    void PublishSingleValueMultiEvent(long value);

    [GenerateEvent]
    void PublishSingleValueSingleEvent(long value);

    // ========== 战斗数据事件 ==========
    
    [GenerateEvent]
    void PublishCombatDataEvent(int health, int attack, int defense, float x, float y, float z, bool isAlive);

    [GenerateEvent]
    void PublishCombatDataEmptyEvent(int health, int attack, int defense, float x, float y, float z, bool isAlive);

    [GenerateEvent]
    void PublishCombatDataMultiEvent(int health, int attack, int defense, float x, float y, float z, bool isAlive);

    [GenerateEvent]
    void PublishCombatDataSingleEvent(int health, int attack, int defense, float x, float y, float z, bool isAlive);

    // ========== 混合业务数据事件 ==========
    
    [GenerateEvent]
    void PublishMixedBusinessDataEvent(Guid sessionId, long timestamp, DateTime createdAt, string userName, Common.SharedTestTypes.PlayerInfo player, int id);

    [GenerateEvent]
    void PublishMixedBusinessDataEmptyEvent(Guid sessionId, long timestamp, DateTime createdAt, string userName, Common.SharedTestTypes.PlayerInfo player, int id);

    [GenerateEvent]
    void PublishMixedBusinessDataMultiEvent(Guid sessionId, long timestamp, DateTime createdAt, string userName, Common.SharedTestTypes.PlayerInfo player, int id);

    [GenerateEvent]
    void PublishMixedBusinessDataSingleEvent(Guid sessionId, long timestamp, DateTime createdAt, string userName, Common.SharedTestTypes.PlayerInfo player, int id);

    // ========== 重负载数据事件 ==========
    
    [GenerateEvent]
    void PublishHeavyPayloadEvent(
        Guid eventId,
        long timestamp,
        double value1,
        double value2,
        DateTime createdAt,
        string name,
        string description,
        string category,
        List<int> tags,
        Dictionary<string, int> metadata,
        Common.SharedTestTypes.PlayerInfo player,
        int id,
        float x,
        float y,
        float z,
        bool flag1,
        bool flag2,
        bool flag3,
        bool flag4,
        bool flag5);

    [GenerateEvent]
    void PublishHeavyPayloadEmptyEvent(
        Guid eventId,
        long timestamp,
        double value1,
        double value2,
        DateTime createdAt,
        string name,
        string description,
        string category,
        List<int> tags,
        Dictionary<string, int> metadata,
        Common.SharedTestTypes.PlayerInfo player,
        int id,
        float x,
        float y,
        float z,
        bool flag1,
        bool flag2,
        bool flag3,
        bool flag4,
        bool flag5);

    [GenerateEvent]
    void PublishHeavyPayloadMultiEvent(
        Guid eventId,
        long timestamp,
        double value1,
        double value2,
        DateTime createdAt,
        string name,
        string description,
        string category,
        List<int> tags,
        Dictionary<string, int> metadata,
        Common.SharedTestTypes.PlayerInfo player,
        int id,
        float x,
        float y,
        float z,
        bool flag1,
        bool flag2,
        bool flag3,
        bool flag4,
        bool flag5);

    [GenerateEvent]
    void PublishHeavyPayloadSingleEvent(
        Guid eventId,
        long timestamp,
        double value1,
        double value2,
        DateTime createdAt,
        string name,
        string description,
        string category,
        List<int> tags,
        Dictionary<string, int> metadata,
        Common.SharedTestTypes.PlayerInfo player,
        int id,
        float x,
        float y,
        float z,
        bool flag1,
        bool flag2,
        bool flag3,
        bool flag4,
        bool flag5);
}

