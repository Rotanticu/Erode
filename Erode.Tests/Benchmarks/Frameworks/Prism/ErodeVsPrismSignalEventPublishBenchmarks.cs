namespace Erode.Tests.Benchmarks.Frameworks.Prism;

[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
[DisassemblyDiagnoser]
public class ErodeVsPrismSignalEventPublishBenchmarks : BenchmarkBase
{
    private static readonly Consumer Consumer = new Consumer();
    [Params(1, 10, 100)]
    public int SubscriberCount { get; set; }

    // Erode 订阅者
    private SubscriptionToken[] _erodeTokens = Array.Empty<SubscriptionToken>();

    // Prism EventAggregator（多订阅者场景）
    private global::Prism.Events.IEventAggregator _eventAggregator = null!;
    private Frameworks.EventTypes.PrismEventTypes.PrismSignalEvent _prismEvent = null!;
    private List<global::Prism.Events.SubscriptionToken> _prismTokens = new();

    // Prism EventAggregator（无订阅者场景）
    private global::Prism.Events.IEventAggregator _noSubscribersEventAggregator = null!;
    private Frameworks.EventTypes.PrismEventTypes.PrismSignalEvent _noSubscribersPrismEvent = null!;

    // Prism EventAggregator（单订阅者场景）
    private global::Prism.Events.IEventAggregator _singleSubscriberEventAggregator = null!;
    private Frameworks.EventTypes.PrismEventTypes.PrismSignalEvent _singleSubscriberPrismEvent = null!;
    private Action<Frameworks.EventTypes.PrismEventTypes.PrismSignalEventData> _singleSubscriberPrismHandler = null!;

    // 缓存的 Handler 委托（在 GlobalSetup 中预先创建，避免每次调用都创建新实例）
    private InAction<Frameworks.EventTypes.ErodeEventTypes.SignalEvent> _warmupErodeHandler = null!;
    private InAction<Frameworks.EventTypes.ErodeEventTypes.SignalEvent_Multi>[] _erodeMultiHandlers = Array.Empty<InAction<Frameworks.EventTypes.ErodeEventTypes.SignalEvent_Multi>>();
    private Action<Frameworks.EventTypes.PrismEventTypes.PrismSignalEventData>[] _prismMultiHandlers = Array.Empty<Action<Frameworks.EventTypes.PrismEventTypes.PrismSignalEventData>>();
    private InAction<Frameworks.EventTypes.ErodeEventTypes.SignalEvent_Single> _singleSubscriberErodeHandler = null!;

    [GlobalSetup]
    public void Setup()
    {
        TestCleanupHelper.CleanupAll();
        HandlerHelpers.ResetSink();

        // 预先创建所有 Handler 委托（避免每次调用都创建新实例，消除对象分配）
        _warmupErodeHandler = HandlerHelpers.CreateSignalEventHandler(0);
        _singleSubscriberErodeHandler = HandlerHelpers.CreateSignalEventSingleHandler(0);
        
        // 预先创建多订阅者所需的 Handler 委托（最多 100 个）
        _erodeMultiHandlers = new InAction<Frameworks.EventTypes.ErodeEventTypes.SignalEvent_Multi>[SubscriberCount];
        _prismMultiHandlers = new Action<Frameworks.EventTypes.PrismEventTypes.PrismSignalEventData>[SubscriberCount];
        for (int i = 0; i < SubscriberCount; i++)
        {
            _erodeMultiHandlers[i] = HandlerHelpers.CreateSignalEventMultiHandler(i);
            _prismMultiHandlers[i] = HandlerHelpers.CreatePrismSignalEventHandler(i);
        }

        // 预热 Erode
        WarmupErode();

        // 设置多订阅者环境
        SetupMultiSubscribers();

        // 设置无订阅者环境
        SetupNoSubscribers();

        // 设置单订阅者环境
        SetupSingleSubscriber();
    }

    private void WarmupErode()
    {
        var warmupEvent = new Frameworks.EventTypes.ErodeEventTypes.SignalEvent();
        var warmupToken = EventDispatcher<Frameworks.EventTypes.ErodeEventTypes.SignalEvent>.Subscribe(_warmupErodeHandler);
        for (int i = 0; i < 100; i++)
        {
            EventDispatcher<Frameworks.EventTypes.ErodeEventTypes.SignalEvent>.Publish(in warmupEvent);
        }
        warmupToken.Dispose();
        TestCleanupHelper.CleanupAll();
        HandlerHelpers.ResetSink();
    }

    private void SetupMultiSubscribers()
    {
        // Erode 多订阅者（使用 SignalEvent_Multi 实现静态隔离）
        _erodeTokens = new SubscriptionToken[SubscriberCount];
        for (int i = 0; i < SubscriberCount; i++)
        {
            // 使用预先创建的委托，避免每次调用都创建新实例
            _erodeTokens[i] = EventDispatcher<Frameworks.EventTypes.ErodeEventTypes.SignalEvent_Multi>.Subscribe(_erodeMultiHandlers[i]);
        }

        // Prism 多订阅者
        _eventAggregator = new global::Prism.Events.EventAggregator();
        _prismEvent = _eventAggregator.GetEvent<Frameworks.EventTypes.PrismEventTypes.PrismSignalEvent>();
        for (int i = 0; i < SubscriberCount; i++)
        {
            // 使用预先创建的委托，与 Erode 保持一致，确保公平对比
            var token = _prismEvent.Subscribe(_prismMultiHandlers[i], ThreadOption.PublisherThread);
            _prismTokens.Add(token);
        }
    }

    private void SetupNoSubscribers()
    {
        _noSubscribersEventAggregator = new global::Prism.Events.EventAggregator();
        _noSubscribersPrismEvent = _noSubscribersEventAggregator.GetEvent<Frameworks.EventTypes.PrismEventTypes.PrismSignalEvent>();
    }

    private void SetupSingleSubscriber()
    {
        _singleSubscriberEventAggregator = new global::Prism.Events.EventAggregator();
        _singleSubscriberPrismEvent = _singleSubscriberEventAggregator.GetEvent<Frameworks.EventTypes.PrismEventTypes.PrismSignalEvent>();
        // 使用预先创建的委托（在 Setup 中已创建）
        _singleSubscriberPrismHandler = _prismMultiHandlers[0];
    }

    // ========== 多订阅者发布 ==========

    [Benchmark(Baseline = true)]
    public long Publish_MultiSubscribers_Erode()
    {
        HandlerHelpers.ResetSink();
        var evt = new Frameworks.EventTypes.ErodeEventTypes.SignalEvent_Multi();
        EventDispatcher<Frameworks.EventTypes.ErodeEventTypes.SignalEvent_Multi>.Publish(in evt);
        var result = HandlerHelpers.ReadSink();
        Consumer.Consume(result);
        return result;
    }

    [Benchmark]
    public long Publish_MultiSubscribers_Prism()
    {
        HandlerHelpers.ResetSink();
        var evt = new Frameworks.EventTypes.PrismEventTypes.PrismSignalEventData();
        _prismEvent.Publish(evt);
        var result = HandlerHelpers.ReadSink();
        Consumer.Consume(result);
        return result;
    }

    // ========== 无订阅者发布 ==========

    [Benchmark]
    public long Publish_NoSubscribers_Erode()
    {
        // 使用 SignalEvent_Empty 实现静态隔离，确保真正的"无订阅"状态
        // 不同的事件类型在静态空间中是物理隔离的，无需清理
        HandlerHelpers.ResetSink();
        var evt = new Frameworks.EventTypes.ErodeEventTypes.SignalEvent_Empty();
        EventDispatcher<Frameworks.EventTypes.ErodeEventTypes.SignalEvent_Empty>.Publish(in evt);
        var result = HandlerHelpers.ReadSink();
        Consumer.Consume(result);
        return result;
    }

    [Benchmark]
    public long Publish_NoSubscribers_Prism()
    {
        HandlerHelpers.ResetSink();
        var evt = new Frameworks.EventTypes.PrismEventTypes.PrismSignalEventData();
        _noSubscribersPrismEvent.Publish(evt);
        var result = HandlerHelpers.ReadSink();
        Consumer.Consume(result);
        return result;
    }

    // ========== 单订阅者发布（包含订阅+发布+取消） ==========

    [Benchmark]
    public long Publish_SingleSubscriber_Erode()
    {
        // 使用 SignalEvent_Single 实现静态隔离
        HandlerHelpers.ResetSink();
        // 使用预先创建的委托，避免每次调用都创建新实例
        var token = EventDispatcher<Frameworks.EventTypes.ErodeEventTypes.SignalEvent_Single>.Subscribe(_singleSubscriberErodeHandler);
        var evt = new Frameworks.EventTypes.ErodeEventTypes.SignalEvent_Single();
        EventDispatcher<Frameworks.EventTypes.ErodeEventTypes.SignalEvent_Single>.Publish(in evt);
        token.Dispose();
        var result = HandlerHelpers.ReadSink();
        Consumer.Consume(result);
        return result;
    }

    [Benchmark]
    public long Publish_SingleSubscriber_Prism()
    {
        HandlerHelpers.ResetSink();
        var token = _singleSubscriberPrismEvent.Subscribe(_singleSubscriberPrismHandler, ThreadOption.PublisherThread);
        var evt = new Frameworks.EventTypes.PrismEventTypes.PrismSignalEventData();
        _singleSubscriberPrismEvent.Publish(evt);
        token.Dispose(); // Prism 的 SubscriptionToken 使用 Dispose()
        var result = HandlerHelpers.ReadSink();
        Consumer.Consume(result);
        return result;
    }

    public override void Cleanup()
    {
        foreach (var token in _erodeTokens)
        {
            token.Dispose();
        }
        _erodeTokens = Array.Empty<SubscriptionToken>();

        foreach (var token in _prismTokens)
        {
            token.Dispose(); // Prism 的 SubscriptionToken 使用 Dispose()
        }
        _prismTokens.Clear();

        base.Cleanup();
    }
}








