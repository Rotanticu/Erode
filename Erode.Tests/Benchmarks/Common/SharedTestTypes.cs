namespace Erode.Tests.Benchmarks.Common;

/// <summary>
/// 共享的测试类型定义
/// 用于 Erode 和 Prism 事件类型的公共类型
/// </summary>
public static class SharedTestTypes
{
    /// <summary>
    /// 玩家信息，用于混合业务数据和重负载数据事件
    /// </summary>
    public class PlayerInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Level { get; set; }
    }
}

