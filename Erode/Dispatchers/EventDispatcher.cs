using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Erode;

/// <summary>
/// 事件调度器，负责管理事件的订阅和发布。
/// 合并了静态管理器和泛型调度器的功能。
/// </summary>
public static class EventDispatcher
{
    // ========== 静态管理器功能 ==========
    
    // Key: 事件类型 Type, Value: 对应的泛型调度器实例
    private static readonly ConcurrentDictionary<Type, IDispatcher> _allDispatchers = new();

    // 用于生成全局唯一的订阅 ID
    private static long _nextId = 1;

    /// <summary>
    /// 线程安全的获取下一个 ID
    /// </summary>
    internal static long GetNextId()
    {
        return Interlocked.Increment(ref _nextId);
    }

    /// <summary>
    /// 线程安全的注册泛型调度器
    /// </summary>
    internal static void RegisterDispatcher(Type eventType, IDispatcher dispatcher)
    {
        _allDispatchers[eventType] = dispatcher;
    }

    /// <summary>
    /// 全局异常处理钩子（可选）。当事件 handler 抛出异常时，会先调用生成类自己的 OnException，
    /// 然后如果设置了此钩子，也会调用此委托进行统一处理。
    /// 参数：(事件对象, 出错的 handler 委托, 异常对象)
    /// </summary>
    public static System.Action<IEvent, System.Delegate, System.Exception>? OnException;

}

    /// <summary>
    /// 泛型零 GC 调度器核心。为每种事件类型 TEvent 独立存储和分发订阅。
    /// 使用不可变数组 + Copy-On-Write 实现零 GC、零锁的发布操作。
    /// </summary>
    public sealed class EventDispatcher<TEvent> : IDispatcher
        where TEvent : struct, IEvent
    {
        // 单例实例
        private static readonly EventDispatcher<TEvent> _instance = new();

        // 零 GC 核心：使用不可变数组存储订阅处理器（ID + Handler 对）
        // 读操作（Publish）无锁，直接获取数组引用（引用赋值是原子的）
        // volatile 确保多核 CPU 环境下的内存可见性，防止编译器或 CPU 缓存过度优化
        private static volatile HandlerEntry[] _handlerArray = Array.Empty<HandlerEntry>();

        // 写操作（Subscribe/Unsubscribe）的锁
        private static readonly object _writeLock = new();

        // 静态构造函数：自动注册当前类型的调度器实例到全局中心
        // 利用静态泛型类的特性，无需外部手动调用 RegisterDispatcher
        static EventDispatcher()
        {
            EventDispatcher.RegisterDispatcher(typeof(TEvent), _instance);
        }

        // 私有构造函数，确保单例
        private EventDispatcher()
        {
        }

        /// <summary>
        /// 获取单例实例
        /// </summary>
        public static EventDispatcher<TEvent> Instance => _instance;

        /// <summary>
        /// 订阅事件，返回 SubscriptionToken。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SubscriptionToken Subscribe(InAction<TEvent> handler)
        {
            ArgumentNullException.ThrowIfNull(handler);

            return _instance.SubscribeInternal(handler);
        }

        /// <summary>
        /// 内部订阅方法（Copy-On-Write）
        /// </summary>
        private SubscriptionToken SubscribeInternal(InAction<TEvent> handler)
        {
            var id = EventDispatcher.GetNextId();
            var entry = new HandlerEntry(id, handler);

            lock (_writeLock)
            {
                // Copy-On-Write：新建数组替换旧数组
                var newArray = new HandlerEntry[_handlerArray.Length + 1];
                _handlerArray.AsSpan().CopyTo(newArray.AsSpan());
                newArray[^1] = entry;
                _handlerArray = newArray;
            }

            // 直接传入 Unsubscribe 方法的委托，避免 Type 查找开销
            return new SubscriptionToken(id, Unsubscribe);
        }

        /// <summary>
        /// 退订方法（用于创建 SubscriptionToken 的委托）
        /// </summary>
        private void Unsubscribe(long id)
        {
            ((IDispatcher)this).Unsubscribe(id);
        }

        /// <summary>
        /// 零 GC、零锁发布核心。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Publish(in TEvent eventData)
        {
            PublishInternal(in eventData, null);
        }

        /// <summary>
        /// 零 GC、零锁发布核心（带异常处理回调）。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Publish(in TEvent eventData, System.Action<IEvent, System.Delegate, System.Exception>? onException)
        {
            PublishInternal(in eventData, onException);
        }

        /// <summary>
        /// 内部发布方法（零 GC、零锁）
        /// 注意：即使某个 handler 抛异常，也会继续调用其他 handler
        /// 异常通过 onException 回调转发，不在发布路径抛出，保证发布者逻辑的健壮性
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void PublishInternal(in TEvent eventData, System.Action<IEvent, System.Delegate, System.Exception>? onException)
        {
            // 1. 无锁获取当前数组引用（引用赋值是原子的）
            var localArray = _handlerArray;

            // 2. 转换为 Span 享受最高性能迭代，零分配
            var span = localArray.AsSpan();
            
            for (int i = 0; i < span.Length; i++)
            {
                try
                {
                    span[i].Handler.Invoke(in eventData);
                }
                catch (Exception ex)
                {
                    // 通过回调转发异常，不在分发路径内抛出
                    // 这保证了发布者逻辑的健壮性和 0 GC（在正常上报流程下）
                    onException?.Invoke(eventData, span[i].Handler, ex);
                }
            }
        }

        /// <summary>
        /// IDispatcher 接口实现，用于移除指定 ID 的订阅（Copy-On-Write）。
        /// </summary>
        void IDispatcher.Unsubscribe(long id)
        {
            lock (_writeLock)
            {
                // 查找要移除的项的索引
                var currentArray = _handlerArray;
                int indexToRemove = -1;
                for (int i = 0; i < currentArray.Length; i++)
                {
                    if (currentArray[i].Id == id)
                    {
                        indexToRemove = i;
                        break;
                    }
                }

                // 如果没找到，直接返回
                if (indexToRemove == -1)
                    return;

                // Copy-On-Write：创建新数组，排除要移除的项
                if (currentArray.Length == 1)
                {
                    // 如果只有一个项，直接设为空数组
                    _handlerArray = [];
                }
                else
                {
                    var newArray = new HandlerEntry[currentArray.Length - 1];
                    var newSpan = newArray.AsSpan();
                    var currentSpan = currentArray.AsSpan();
                    
                    // 复制 indexToRemove 之前的项
                    if (indexToRemove > 0)
                    {
                        currentSpan[..indexToRemove].CopyTo(newSpan);
                    }
                    // 复制 indexToRemove 之后的项
                    if (indexToRemove < currentArray.Length - 1)
                    {
                        currentSpan[(indexToRemove + 1)..].CopyTo(newSpan.Slice(indexToRemove));
                    }
                    _handlerArray = newArray;
                }
            }
        }

        /// <summary>
        /// 处理器条目（ID + Handler 对）
        /// </summary>
        private readonly struct HandlerEntry(long id, InAction<TEvent> handler)
    {
            public readonly long Id = id;
            public readonly InAction<TEvent> Handler = handler;
    }
}

