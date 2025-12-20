namespace Erode.Tests.Benchmarks.Frameworks.Prism;

[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
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

    [GlobalSetup]
    public void Setup()
    {
        TestCleanupHelper.CleanupAll();
        HandlerHelpers.ResetSink();

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
        var warmupToken = EventDispatcher<Frameworks.EventTypes.ErodeEventTypes.SignalEvent>.Subscribe(HandlerHelpers.CreateSignalEventHandler(0));
        for (int i = 0; i < 100; i++)
        {
            EventDispatcher<Frameworks.EventTypes.ErodeEventTypes.SignalEvent>.Publish(warmupEvent);
        }
        warmupToken.Dispose();
        TestCleanupHelper.CleanupAll();
        HandlerHelpers.ResetSink();
    }

    private void SetupMultiSubscribers()
    {
        // Erode 多订阅者
        _erodeTokens = new SubscriptionToken[SubscriberCount];
        for (int i = 0; i < SubscriberCount; i++)
        {
            _erodeTokens[i] = EventDispatcher<Frameworks.EventTypes.ErodeEventTypes.SignalEvent>.Subscribe(HandlerHelpers.CreateSignalEventHandler(i));
        }

        // Prism 多订阅者
        _eventAggregator = new global::Prism.Events.EventAggregator();
        _prismEvent = _eventAggregator.GetEvent<Frameworks.EventTypes.PrismEventTypes.PrismSignalEvent>();
        for (int i = 0; i < SubscriberCount; i++)
        {
            var handlerId = i;
            var token = _prismEvent.Subscribe(data => HandlerHelpers.CreatePrismSignalEventHandler(handlerId)(data));
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
        _singleSubscriberPrismHandler = HandlerHelpers.CreatePrismSignalEventHandler(0);
    }

    // ========== 多订阅者发布 ==========

    [Benchmark(Baseline = true)]
    public long Publish_MultiSubscribers_Erode()
    {
        HandlerHelpers.ResetSink();
        var evt = new Frameworks.EventTypes.ErodeEventTypes.SignalEvent();
        EventDispatcher<Frameworks.EventTypes.ErodeEventTypes.SignalEvent>.Publish(evt);
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
        HandlerHelpers.ResetSink();
        var evt = new Frameworks.EventTypes.ErodeEventTypes.SignalEvent();
        EventDispatcher<Frameworks.EventTypes.ErodeEventTypes.SignalEvent>.Publish(evt);
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
        HandlerHelpers.ResetSink();
        var token = EventDispatcher<Frameworks.EventTypes.ErodeEventTypes.SignalEvent>.Subscribe(HandlerHelpers.CreateSignalEventHandler(0));
        var evt = new Frameworks.EventTypes.ErodeEventTypes.SignalEvent();
        EventDispatcher<Frameworks.EventTypes.ErodeEventTypes.SignalEvent>.Publish(evt);
        token.Dispose();
        var result = HandlerHelpers.ReadSink();
        Consumer.Consume(result);
        return result;
    }

    [Benchmark]
    public long Publish_SingleSubscriber_Prism()
    {
        HandlerHelpers.ResetSink();
        var token = _singleSubscriberPrismEvent.Subscribe(_singleSubscriberPrismHandler);
        var evt = new Frameworks.EventTypes.PrismEventTypes.PrismSignalEventData();
        _singleSubscriberPrismEvent.Publish(evt);
        token.Dispose();
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
            token.Dispose();
        }
        _prismTokens.Clear();

        base.Cleanup();
    }
}








