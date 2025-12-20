namespace Erode.Tests.Helpers
{
    /// <summary>
    /// 用于测试 Source Generator 的接口定义
    /// </summary>

    // 正常接口
    public interface IPlayerEvents
    {
        [GenerateEvent]
        void PublishPlayerMovedEvent(int x, int y);
    }

    // Internal 接口
    internal interface IInternalEvents
    {
        [GenerateEvent]
        void PublishInternalTestEvent(string message);
    }

    // 多个事件的接口
    public interface IMultiEventInterface
    {
        [GenerateEvent]
        void PublishFirstEvent(string data);

        [GenerateEvent]
        void PublishSecondEvent(int value);
    }

    // 无参数事件接口
    public interface INoParamEvents
    {
        [GenerateEvent]
        void PublishNoParamEvent();
    }

    // 多参数事件接口（10+ 参数）
    public interface ILargeParamEvents
    {
        [GenerateEvent]
        void PublishLargeParamEvent(
            int p1, int p2, int p3, int p4, int p5,
            int p6, int p7, int p8, int p9, int p10,
            int p11, int p12);
    }

    // 关键字参数名接口
    public interface IKeywordParamEvents
    {
        [GenerateEvent]
        void PublishKeywordEvent(int @event, string @class, int @params);
    }

    // ========== 单元测试事件接口 ==========

    /// <summary>
    /// 并发测试事件接口
    /// </summary>
    public interface IConcurrencyTestEvents
    {
        [GenerateEvent]
        void PublishConcurrentTestEvent(int id);
    }

    /// <summary>
    /// 异步测试事件接口
    /// </summary>
    public interface IAsyncTestEvents
    {
        [GenerateEvent]
        void PublishAsyncTestEvent(int value);
    }

    /// <summary>
    /// 退订测试事件接口
    /// </summary>
    public interface IUnsubscribeTestEvents
    {
        [GenerateEvent]
        void PublishUnsubscribeTestEvent(int value);

        [GenerateEvent]
        void PublishUnsubscribeTest2Event();
    }

    /// <summary>
    /// 发布测试事件接口
    /// </summary>
    public interface IPublishTestEvents
    {
        [GenerateEvent]
        void PublishBasicTestEvent(int value);

        [GenerateEvent]
        void PublishBasicTestEmptyEvent();
    }

    /// <summary>
    /// 线程安全测试事件接口
    /// </summary>
    public interface IThreadSafetyTestEvents
    {
        [GenerateEvent]
        void PublishThreadSafetyTestEvent(int value);
    }

    /// <summary>
    /// 边界情况测试事件接口
    /// </summary>
    public interface IEdgeCaseTestEvents
    {
        [GenerateEvent]
        void PublishEdgeCaseTestEvent();
    }

    /// <summary>
    /// 异常测试事件接口
    /// </summary>
    public interface IExceptionTestEvents
    {
        [GenerateEvent]
        void PublishExceptionRobustnessTestEvent(int value);

        [GenerateEvent]
        void PublishSingleExceptionRobustnessTestEvent(int value);

        [GenerateEvent]
        void PublishMultipleExceptionRobustnessTestEvent(int value);
    }

    /// <summary>
    /// 顺序测试事件接口
    /// </summary>
    public interface IOrderTestEvents
    {
        [GenerateEvent]
        void PublishOrderTestNewEvent(int order);
    }

    /// <summary>
    /// 生命周期测试事件接口
    /// </summary>
    public interface ILifecycleTestEvents
    {
        [GenerateEvent]
        void PublishLifecycleTestEvent();
    }

    /// <summary>
    /// 集成测试事件接口
    /// </summary>
    public interface IIntegrationTestEvents
    {
        [GenerateEvent]
        void PublishIntegrationTestEvent();
    }

    /// <summary>
    /// Copy-On-Write 测试事件接口
    /// </summary>
    public interface ICopyOnWriteTestEvents
    {
        [GenerateEvent]
        void PublishCopyOnWriteTestEvent(int value);
    }

    /// <summary>
    /// 静态单例测试事件接口
    /// </summary>
    public interface IStaticSingletonTestEvents
    {
        [GenerateEvent]
        void PublishStaticSingletonTestEvent(int value);
    }

    /// <summary>
    /// 事件结构测试事件接口
    /// </summary>
    public interface IEventStructureTestEvents
    {
        [GenerateEvent]
        void PublishStructureSimpleEvent();

        [GenerateEvent]
        void PublishStructureValueTypesEvent(int x, int y, float z);
    }

    /// <summary>
    /// 基础测试事件接口（用于替换手写的 TestEvent）
    /// </summary>
    public interface IBasicTestEvents
    {
        [GenerateEvent]
        void PublishSimpleTestEvent();
    }

    /// <summary>
    /// 带数据的测试事件接口（用于替换手写的 TestEventWithData）
    /// </summary>
    public interface IBasicTestEventsWithData
    {
        [GenerateEvent]
        void PublishBasicTestWithDataEvent(string message, int value);
    }
}

// 不同命名空间的同名接口（用于测试命名空间冲突）
namespace Erode.Tests.Helpers.TestNamespace1
{
    public interface ITestEvents
    {
        [GenerateEvent]
        void PublishTestEvent(string data);
    }
}

namespace Erode.Tests.Helpers.TestNamespace2
{
    public interface ITestEvents
    {
        [GenerateEvent]
        void PublishTestEvent(int value);
    }
}
