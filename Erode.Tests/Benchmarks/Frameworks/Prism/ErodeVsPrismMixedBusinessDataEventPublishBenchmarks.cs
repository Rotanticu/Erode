using Erode.Tests.Benchmarks.Frameworks.EventTypes;

namespace Erode.Tests.Benchmarks.Frameworks.Prism;

[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
public class ErodeVsPrismMixedBusinessDataEventPublishBenchmarks : BenchmarkBase
{
    private static readonly Consumer Consumer = new Consumer();
    [Params(1, 10, 100)]
    public int SubscriberCount { get; set; }

    // Erode 订阅者
    private SubscriptionToken[] _erodeTokens = Array.Empty<SubscriptionToken>();

    // Prism EventAggregator（多订阅者场景）
    private global::Prism.Events.IEventAggregator _eventAggregator = null!;
    private Frameworks.EventTypes.PrismEventTypes.PrismMixedBusinessDataEvent _prismEvent = null!;
    private List<global::Prism.Events.SubscriptionToken> _prismTokens = new();

    // Prism EventAggregator（无订阅者场景）
    private global::Prism.Events.IEventAggregator _noSubscribersEventAggregator = null!;
    private Frameworks.EventTypes.PrismEventTypes.PrismMixedBusinessDataEvent _noSubscribersPrismEvent = null!;

    // Prism EventAggregator（单订阅者场景）
    private global::Prism.Events.IEventAggregator _singleSubscriberEventAggregator = null!;
    private Frameworks.EventTypes.PrismEventTypes.PrismMixedBusinessDataEvent _singleSubscriberPrismEvent = null!;
    private Action<Frameworks.EventTypes.PrismEventTypes.PrismMixedBusinessDataEventData> _singleSubscriberPrismHandler = null!;

    // 共享的测试数据
    private static readonly Guid TestSessionId = Guid.NewGuid();
    private static readonly Common.SharedTestTypes.PlayerInfo TestPlayer = new Common.SharedTestTypes.PlayerInfo { Id = 1, Name = "TestPlayer", Level = 10 };

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
        var warmupToken = BenchmarkEvents.SubscribeMixedBusinessDataEvent(HandlerHelpers.CreateMixedBusinessDataEventHandler(0));
        for (int i = 0; i < 100; i++)
        {
            BenchmarkEvents.PublishMixedBusinessDataEvent(TestSessionId, DateTime.UtcNow.Ticks, DateTime.UtcNow, "TestUser", TestPlayer, 1);
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
            _erodeTokens[i] = BenchmarkEvents.SubscribeMixedBusinessDataMultiEvent(HandlerHelpers.CreateMixedBusinessDataMultiEventHandler(i));
        }

        _eventAggregator = new global::Prism.Events.EventAggregator();
        _prismEvent = _eventAggregator.GetEvent<Frameworks.EventTypes.PrismEventTypes.PrismMixedBusinessDataEvent>();
        for (int i = 0; i < SubscriberCount; i++)
        {
            var handlerId = i;
            var token = _prismEvent.Subscribe(data => HandlerHelpers.CreatePrismMixedBusinessDataEventHandler(handlerId)(data));
            _prismTokens.Add(token);
        }
    }

    private void SetupNoSubscribers()
    {
        _noSubscribersEventAggregator = new global::Prism.Events.EventAggregator();
        _noSubscribersPrismEvent = _noSubscribersEventAggregator.GetEvent<Frameworks.EventTypes.PrismEventTypes.PrismMixedBusinessDataEvent>();
    }

    private void SetupSingleSubscriber()
    {
        _singleSubscriberEventAggregator = new global::Prism.Events.EventAggregator();
        _singleSubscriberPrismEvent = _singleSubscriberEventAggregator.GetEvent<Frameworks.EventTypes.PrismEventTypes.PrismMixedBusinessDataEvent>();
        _singleSubscriberPrismHandler = HandlerHelpers.CreatePrismMixedBusinessDataEventHandler(0);
    }

    [Benchmark(Baseline = true)]
    public long Publish_MultiSubscribers_Erode()
    {
        HandlerHelpers.ResetSink();
        BenchmarkEvents.PublishMixedBusinessDataMultiEvent(TestSessionId, DateTime.UtcNow.Ticks, DateTime.UtcNow, "TestUser", TestPlayer, 1);
        var result = HandlerHelpers.ReadSink();
        Consumer.Consume(result);
        return result;
    }

    [Benchmark]
    public long Publish_MultiSubscribers_Prism()
    {
        HandlerHelpers.ResetSink();
        var evt = new Frameworks.EventTypes.PrismEventTypes.PrismMixedBusinessDataEventData
        {
            SessionId = TestSessionId,
            Timestamp = DateTime.UtcNow.Ticks,
            CreatedAt = DateTime.UtcNow,
            UserName = "TestUser",
            Player = TestPlayer,
            Id = 1
        };
        _prismEvent.Publish(evt);
        var result = HandlerHelpers.ReadSink();
        Consumer.Consume(result);
        return result;
    }

    [Benchmark]
    public long Publish_NoSubscribers_Erode()
    {
        HandlerHelpers.ResetSink();
        BenchmarkEvents.PublishMixedBusinessDataMultiEvent(TestSessionId, DateTime.UtcNow.Ticks, DateTime.UtcNow, "TestUser", TestPlayer, 1);
        var result = HandlerHelpers.ReadSink();
        Consumer.Consume(result);
        return result;
    }

    [Benchmark]
    public long Publish_NoSubscribers_Prism()
    {
        HandlerHelpers.ResetSink();
        var evt = new Frameworks.EventTypes.PrismEventTypes.PrismMixedBusinessDataEventData
        {
            SessionId = TestSessionId,
            Timestamp = DateTime.UtcNow.Ticks,
            CreatedAt = DateTime.UtcNow,
            UserName = "TestUser",
            Player = TestPlayer,
            Id = 1
        };
        _noSubscribersPrismEvent.Publish(evt);
        var result = HandlerHelpers.ReadSink();
        Consumer.Consume(result);
        return result;
    }

    [Benchmark]
    public long Publish_SingleSubscriber_Erode()
    {
        HandlerHelpers.ResetSink();
        var token = BenchmarkEvents.SubscribeMixedBusinessDataSingleEvent(HandlerHelpers.CreateMixedBusinessDataSingleEventHandler(0));
        BenchmarkEvents.PublishMixedBusinessDataSingleEvent(TestSessionId, DateTime.UtcNow.Ticks, DateTime.UtcNow, "TestUser", TestPlayer, 1);
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
        var evt = new Frameworks.EventTypes.PrismEventTypes.PrismMixedBusinessDataEventData
        {
            SessionId = TestSessionId,
            Timestamp = DateTime.UtcNow.Ticks,
            CreatedAt = DateTime.UtcNow,
            UserName = "TestUser",
            Player = TestPlayer,
            Id = 1
        };
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










