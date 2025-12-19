namespace Erode;

/// <summary>
/// 内部接口，用于抽象泛型调度器，方便 EventDispatcher 进行统一管理和查找。
/// </summary>
internal interface IDispatcher
{
    /// <summary>
    /// 抽象方法，用于处理通用退订逻辑
    /// </summary>
    /// <param name="id">订阅令牌的 ID</param>
    void Unsubscribe(long id);
}

