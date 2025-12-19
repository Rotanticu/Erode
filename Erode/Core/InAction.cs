namespace Erode;

/// <summary>
/// 支持 in 参数的委托类型，用于零拷贝事件处理
/// </summary>
public delegate void InAction<T>(in T arg);

