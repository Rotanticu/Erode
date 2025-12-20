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
