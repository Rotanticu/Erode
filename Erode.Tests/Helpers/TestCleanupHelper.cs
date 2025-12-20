using System.Reflection;

namespace Erode.Tests.Helpers;

/// <summary>
/// 测试清理辅助类 - 用于清理所有 EventDispatcher 的订阅状态
/// 解决测试并行执行时的"幽灵订阅者"问题
/// </summary>
public static class TestCleanupHelper
{
    /// <summary>
    /// 清空所有已注册的 EventDispatcher 的订阅列表
    /// 直接操作 _handlerArray 字段来清空订阅
    /// </summary>
    public static void ClearAllSubscriptions()
    {
        // 获取 EventDispatcher 静态类中注册的所有调度器
        var allDispatchersField = typeof(EventDispatcher).GetField("_allDispatchers",
            BindingFlags.NonPublic | BindingFlags.Static);

        if (allDispatchersField == null)
            return;

        var allDispatchersValue = allDispatchersField.GetValue(null);
        if (allDispatchersValue == null)
            return;

        // 使用反射获取 Keys 属性（因为 IDispatcher 是 internal 的）
        var keysProperty = allDispatchersValue.GetType().GetProperty("Keys");
        if (keysProperty == null)
            return;

        var keys = keysProperty.GetValue(allDispatchersValue);
        if (keys == null)
            return;

        // 转换为 IEnumerable<Type> 并遍历
        var eventTypes = new List<Type>();
        foreach (var key in (System.Collections.IEnumerable)keys)
        {
            if (key is Type eventType)
            {
                eventTypes.Add(eventType);
            }
        }

        // 遍历所有已注册的事件类型，直接清空 _handlerArray
        foreach (var eventType in eventTypes)
        {
            try
            {
                // 构造 EventDispatcher<TEvent> 类型
                var dispatcherType = typeof(EventDispatcher<>).MakeGenericType(eventType);

                // 获取 _handlerArray 字段
                var handlerArrayField = dispatcherType.GetField("_handlerArray",
                    BindingFlags.NonPublic | BindingFlags.Static);
                if (handlerArrayField != null)
                {
                    // 获取 _writeLock 字段
                    var writeLockField = dispatcherType.GetField("_writeLock",
                        BindingFlags.NonPublic | BindingFlags.Static);

                    if (writeLockField != null)
                    {
                        var writeLock = writeLockField.GetValue(null);
                        if (writeLock != null)
                        {
                            // 加锁后清空数组
                            lock (writeLock)
                            {
                                // 直接使用 Array.Empty<T>() 的等价方式
                                // 获取数组类型（HandlerEntry[]）
                                var arrayType = handlerArrayField.FieldType;
                                // 创建空数组（使用 Array.Empty 的反射等价方式）
                                var emptyArrayMethod = typeof(Array).GetMethod("Empty", BindingFlags.Public | BindingFlags.Static);
                                if (emptyArrayMethod != null)
                                {
                                    var genericEmptyArrayMethod = emptyArrayMethod.MakeGenericMethod(arrayType.GetElementType()!);
                                    var emptyArray = genericEmptyArrayMethod.Invoke(null, null);
                                    handlerArrayField.SetValue(null, emptyArray);
                                }
                                else
                                {
                                    // 如果 Array.Empty 不可用，使用 CreateInstance
                                    var emptyArray = Array.CreateInstance(arrayType.GetElementType()!, 0);
                                    handlerArrayField.SetValue(null, emptyArray);
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                // 忽略反射调用失败的情况（某些类型可能无法访问）
            }
        }
    }

    /// <summary>
    /// 重置全局异常处理钩子和所有生成类的 OnException 字段
    /// </summary>
    public static void ResetGlobalExceptionHandler()
    {
        // 重置全局异常处理钩子
        EventDispatcher.OnException = null;

        // 重置所有生成类的 OnException 字段
        // 查找所有以 "Events" 结尾的静态类（Source Generator 生成的类）
        var assembly = typeof(TestEvents).Assembly;
        var allTypes = assembly.GetTypes();

        foreach (var type in allTypes)
        {
            // 检查是否是静态类且以 "Events" 结尾
            if (type.IsAbstract && type.IsSealed && type.Name.EndsWith("Events"))
            {
                try
                {
                    // 获取 OnException 字段
                    var onExceptionField = type.GetField("OnException",
                        BindingFlags.Public | BindingFlags.Static);
                    if (onExceptionField != null)
                    {
                        // 重置为 null
                        onExceptionField.SetValue(null, null);
                    }
                }
                catch
                {
                    // 忽略反射调用失败的情况
                }
            }
        }
    }

    /// <summary>
    /// 执行完整的测试环境清理
    /// </summary>
    public static void CleanupAll()
    {
        ClearAllSubscriptions();
        ResetGlobalExceptionHandler();
    }

    /// <summary>
    /// 清空特定事件类型的订阅列表（用于性能测试的迭代清理）
    /// </summary>
    public static void ClearSubscriptions<TEvent>() where TEvent : struct, IEvent
    {
        try
        {
            var dispatcherType = typeof(EventDispatcher<>).MakeGenericType(typeof(TEvent));

            // 获取 _handlerArray 字段
            var handlerArrayField = dispatcherType.GetField("_handlerArray",
                BindingFlags.NonPublic | BindingFlags.Static);
            if (handlerArrayField == null)
                return;

            // 获取 _writeLock 字段
            var writeLockField = dispatcherType.GetField("_writeLock",
                BindingFlags.NonPublic | BindingFlags.Static);

            if (writeLockField != null)
            {
                var writeLock = writeLockField.GetValue(null);
                if (writeLock != null)
                {
                    // 加锁后清空数组
                    lock (writeLock)
                    {
                        var arrayType = handlerArrayField.FieldType;
                        var emptyArrayMethod = typeof(Array).GetMethod("Empty", BindingFlags.Public | BindingFlags.Static);
                        if (emptyArrayMethod != null)
                        {
                            var genericEmptyArrayMethod = emptyArrayMethod.MakeGenericMethod(arrayType.GetElementType()!);
                            var emptyArray = genericEmptyArrayMethod.Invoke(null, null);
                            handlerArrayField.SetValue(null, emptyArray);
                        }
                        else
                        {
                            var emptyArray = Array.CreateInstance(arrayType.GetElementType()!, 0);
                            handlerArrayField.SetValue(null, emptyArray);
                        }
                    }
                }
            }
        }
        catch
        {
            // 忽略反射调用失败的情况
        }
    }
}

/// <summary>
/// 测试基类 - 自动清理测试环境
/// 所有测试类可以继承此类，在测试后自动清理所有订阅
/// 注意：只在测试结束后清理，不在测试开始前清理，避免影响测试本身
/// </summary>
public class TestBase : IDisposable
{
    public void Dispose()
    {
        // 测试结束后清理环境，防止污染其他测试
        TestCleanupHelper.CleanupAll();
    }
}

