namespace Erode.Tests.Benchmarks.Frameworks.Prism;

[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
public class ErodeVsPrismSingleValueEventPublishBenchmarks : BenchmarkBase
{
    private static readonly Consumer Consumer = new Consumer();
    [Params(1, 10, 100)]
    public int SubscriberCount { get; set; }

    // Erode 订阅者
    private SubscriptionToken[] _erodeTokens = Array.Empty<SubscriptionToken>();

    // Prism EventAggregator（多订阅者场景）
    private global::Prism.Events.IEventAggregator _eventAggregator = null!;
    private Frameworks.EventTypes.PrismEventTypes.PrismSingleValueEvent _prismEvent = null!;
    private List<global::Prism.Events.SubscriptionToken> _prismTokens = new();

    // Prism EventAggregator（无订阅者场景）
    private global::Prism.Events.IEventAggregator _noSubscribersEventAggregator = null!;
    private Frameworks.EventTypes.PrismEventTypes.PrismSingleValueEvent _noSubscribersPrismEvent = null!;

    // Prism EventAggregator（单订阅者场景）
    private global::Prism.Events.IEventAggregator _singleSubscriberEventAggregator = null!;
    private Frameworks.EventTypes.PrismEventTypes.PrismSingleValueEvent _singleSubscriberPrismEvent = null!;
    private Action<Frameworks.EventTypes.PrismEventTypes.PrismSingleValueEventData> _singleSubscriberPrismHandler = null!;

    [GlobalSetup]
    public void Setup()
    {
        TestCleanupHelper.CleanupAll();
        HandlerHelpers.ResetSink();

        WarmupErode();
        SetupMultiSubscribers();
        SetupNoSubscribers();
        SetupSingleSubscriber();
    }

    private void WarmupErode()
    {
        var warmupEvent = new Frameworks.EventTypes.ErodeEventTypes.SingleValueEvent(12345L);
        var warmupToken = EventDispatcher<Frameworks.EventTypes.ErodeEventTypes.SingleValueEvent>.Subscribe(HandlerHelpers.CreateSingleValueEventHandler(0));
        for (int i = 0; i < 100; i++)
        {
            EventDispatcher<Frameworks.EventTypes.ErodeEventTypes.SingleValueEvent>.Publish(warmupEvent);
        }
        warmupToken.Dispose();
        TestCleanupHelper.CleanupAll();
        HandlerHelpers.ResetSink();
    }

    private void SetupMultiSubscribers()
    {
        _erodeTokens = new SubscriptionToken[SubscriberCount];
        for (int i = 0; i < SubscriberCount; i++)
        {
            _erodeTokens[i] = EventDispatcher<Frameworks.EventTypes.ErodeEventTypes.SingleValueEvent>.Subscribe(HandlerHelpers.CreateSingleValueEventHandler(i));
        }

        _eventAggregator = new global::Prism.Events.EventAggregator();
        _prismEvent = _eventAggregator.GetEvent<Frameworks.EventTypes.PrismEventTypes.PrismSingleValueEvent>();
        for (int i = 0; i < SubscriberCount; i++)
        {
            var handlerId = i;
            var token = _prismEvent.Subscribe(data => HandlerHelpers.CreatePrismSingleValueEventHandler(handlerId)(data));
            _prismTokens.Add(token);
        }
    }

    private void SetupNoSubscribers()
    {
        _noSubscribersEventAggregator = new global::Prism.Events.EventAggregator();
        _noSubscribersPrismEvent = _noSubscribersEventAggregator.GetEvent<Frameworks.EventTypes.PrismEventTypes.PrismSingleValueEvent>();
    }

    private void SetupSingleSubscriber()
    {
        _singleSubscriberEventAggregator = new global::Prism.Events.EventAggregator();
        _singleSubscriberPrismEvent = _singleSubscriberEventAggregator.GetEvent<Frameworks.EventTypes.PrismEventTypes.PrismSingleValueEvent>();
        _singleSubscriberPrismHandler = HandlerHelpers.CreatePrismSingleValueEventHandler(0);
    }

    [Benchmark(Baseline = true)]
    public long Publish_MultiSubscribers_Erode()
    {
        HandlerHelpers.ResetSink();
        var evt = new Frameworks.EventTypes.ErodeEventTypes.SingleValueEvent(12345L);
        EventDispatcher<Frameworks.EventTypes.ErodeEventTypes.SingleValueEvent>.Publish(evt);
        var result = HandlerHelpers.ReadSink();
        Consumer.Consume(result);
        return result;
    }

    [Benchmark]
    public long Publish_MultiSubscribers_Prism()
    {
        HandlerHelpers.ResetSink();
        var evt = new Frameworks.EventTypes.PrismEventTypes.PrismSingleValueEventData { Value = 12345L };
        _prismEvent.Publish(evt);
        var result = HandlerHelpers.ReadSink();
        Consumer.Consume(result);
        return result;
    }

    [Benchmark]
    public long Publish_NoSubscribers_Erode()
    {
        HandlerHelpers.ResetSink();
        var evt = new Frameworks.EventTypes.ErodeEventTypes.SingleValueEvent(12345L);
        EventDispatcher<Frameworks.EventTypes.ErodeEventTypes.SingleValueEvent>.Publish(evt);
        var result = HandlerHelpers.ReadSink();
        Consumer.Consume(result);
        return result;
    }

    [Benchmark]
    public long Publish_NoSubscribers_Prism()
    {
        HandlerHelpers.ResetSink();
        var evt = new Frameworks.EventTypes.PrismEventTypes.PrismSingleValueEventData { Value = 12345L };
        _noSubscribersPrismEvent.Publish(evt);
        var result = HandlerHelpers.ReadSink();
        Consumer.Consume(result);
        return result;
    }

    [Benchmark]
    public long Publish_SingleSubscriber_Erode()
    {
        HandlerHelpers.ResetSink();
        var token = EventDispatcher<Frameworks.EventTypes.ErodeEventTypes.SingleValueEvent>.Subscribe(HandlerHelpers.CreateSingleValueEventHandler(0));
        var evt = new Frameworks.EventTypes.ErodeEventTypes.SingleValueEvent(12345L);
        EventDispatcher<Frameworks.EventTypes.ErodeEventTypes.SingleValueEvent>.Publish(evt);
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
        var evt = new Frameworks.EventTypes.PrismEventTypes.PrismSingleValueEventData { Value = 12345L };
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









