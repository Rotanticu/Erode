namespace Erode.Tests.Benchmarks.Common;

/// <summary>
/// 基准测试基类
/// 提供清理逻辑
/// </summary>
public abstract class BenchmarkBase
{
    /// <summary>
    /// 全局清理方法，确保测试后清理所有订阅
    /// </summary>
    [BenchmarkDotNet.Attributes.GlobalCleanup]
    public virtual void Cleanup()
    {
        TestCleanupHelper.CleanupAll();
    }
}

