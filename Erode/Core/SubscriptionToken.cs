using System.Runtime.CompilerServices;

namespace Erode;

/// <summary>
/// 零 GC 订阅令牌，用于手动退订。
/// 必须是 readonly record struct，完全在栈上分配，无堆分配。
/// 存储 IDispatcher 引用和 long Id，避免委托对象的创建。
/// </summary>
public readonly record struct SubscriptionToken(long Id, IDispatcher Dispatcher) : IDisposable
{
    /// <summary>
    /// 手动退订方法。
    /// 退订逻辑是幂等的，可以安全地多次调用。
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        Dispatcher?.Unsubscribe(Id);
    }
}

