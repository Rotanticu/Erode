using System.Runtime.CompilerServices;

namespace Erode;

/// <summary>
/// 零 GC 订阅令牌，用于 Unsubscribe。
/// 必须是 readonly record struct。
/// 存储 Action<long> 而不是 Type，避免类型查找开销和元数据开销。
/// </summary>
public readonly record struct SubscriptionToken(long Id, Action<long> UnsubscribeAction) : IDisposable
{
    /// <summary>
    /// 实现 IDisposable，允许在 using 块中自动退订。
    /// 退订逻辑是幂等的，可以安全地多次调用。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        UnsubscribeAction?.Invoke(Id);
    }
}

