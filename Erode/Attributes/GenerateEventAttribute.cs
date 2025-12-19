namespace Erode;

/// <summary>
/// 标记方法，指示Source Generator为此方法生成对应的事件类和发布方法
/// 方法名必须符合：Publish 开头、Event 结尾、中间有内容、不能有多个 Publish 或 Event
/// 例如：PublishTestEvent -> TestEvent, PublishTestGeneratedEvent -> TestGeneratedEvent
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class GenerateEventAttribute : Attribute
{
    public GenerateEventAttribute()
    {
    }
}

