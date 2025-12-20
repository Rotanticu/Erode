using Erode.Tests.Benchmarks.Frameworks.EventTypes;

namespace Erode.Tests.Benchmarks.Frameworks.Prism;

[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
public class ErodeVsPrismHeavyPayloadEventPublishBenchmarks : BenchmarkBase
{
    private static readonly Consumer Consumer = new Consumer();
    [Params(1, 10, 100)]
    public int SubscriberCount { get; set; }

    // Erode 订阅者
    private SubscriptionToken[] _erodeTokens = Array.Empty<SubscriptionToken>();

    // Prism EventAggregator（多订阅者场景）
    private global::Prism.Events.IEventAggregator _eventAggregator = null!;
    private Frameworks.EventTypes.PrismEventTypes.PrismHeavyPayloadEvent _prismEvent = null!;
    private List<global::Prism.Events.SubscriptionToken> _prismTokens = new();

    // Prism EventAggregator（无订阅者场景）
    private global::Prism.Events.IEventAggregator _noSubscribersEventAggregator = null!;
    private Frameworks.EventTypes.PrismEventTypes.PrismHeavyPayloadEvent _noSubscribersPrismEvent = null!;

    // Prism EventAggregator（单订阅者场景）
    private global::Prism.Events.IEventAggregator _singleSubscriberEventAggregator = null!;
    private Frameworks.EventTypes.PrismEventTypes.PrismHeavyPayloadEvent _singleSubscriberPrismEvent = null!;
    private Action<Frameworks.EventTypes.PrismEventTypes.PrismHeavyPayloadEventData> _singleSubscriberPrismHandler = null!;

    // 共享的测试数据
    private static readonly Guid TestEventId = Guid.NewGuid();
    private static readonly Common.SharedTestTypes.PlayerInfo TestPlayer = new Common.SharedTestTypes.PlayerInfo { Id = 1, Name = "TestPlayer", Level = 10 };
    private static readonly List<int> TestTags = new List<int> { 1, 2, 3, 4, 5 };
    private static readonly Dictionary<string, int> TestMetadata = new Dictionary<string, int> { { "key1", 100 }, { "key2", 200 } };

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
        var warmupToken = BenchmarkEvents.SubscribeHeavyPayloadEvent(HandlerHelpers.CreateHeavyPayloadEventHandler(0));
        for (int i = 0; i < 100; i++)
        {
            BenchmarkEvents.PublishHeavyPayloadEvent(TestEventId, DateTime.UtcNow.Ticks, 1.5, 2.5, DateTime.UtcNow, "TestName", "TestDescription", "TestCategory", TestTags, TestMetadata, TestPlayer, 1, 1.0f, 2.0f, 3.0f, true, false, true, false, true);
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
            _erodeTokens[i] = BenchmarkEvents.SubscribeHeavyPayloadMultiEvent(HandlerHelpers.CreateHeavyPayloadMultiEventHandler(i));
        }

        _eventAggregator = new global::Prism.Events.EventAggregator();
        _prismEvent = _eventAggregator.GetEvent<Frameworks.EventTypes.PrismEventTypes.PrismHeavyPayloadEvent>();
        for (int i = 0; i < SubscriberCount; i++)
        {
            var handlerId = i;
            var token = _prismEvent.Subscribe(data => HandlerHelpers.CreatePrismHeavyPayloadEventHandler(handlerId)(data));
            _prismTokens.Add(token);
        }
    }

    private void SetupNoSubscribers()
    {
        _noSubscribersEventAggregator = new global::Prism.Events.EventAggregator();
        _noSubscribersPrismEvent = _noSubscribersEventAggregator.GetEvent<Frameworks.EventTypes.PrismEventTypes.PrismHeavyPayloadEvent>();
    }

    private void SetupSingleSubscriber()
    {
        _singleSubscriberEventAggregator = new global::Prism.Events.EventAggregator();
        _singleSubscriberPrismEvent = _singleSubscriberEventAggregator.GetEvent<Frameworks.EventTypes.PrismEventTypes.PrismHeavyPayloadEvent>();
        _singleSubscriberPrismHandler = HandlerHelpers.CreatePrismHeavyPayloadEventHandler(0);
    }

    [Benchmark(Baseline = true)]
    public long Publish_MultiSubscribers_Erode()
    {
        HandlerHelpers.ResetSink();
        BenchmarkEvents.PublishHeavyPayloadMultiEvent(TestEventId, DateTime.UtcNow.Ticks, 1.5, 2.5, DateTime.UtcNow, "TestName", "TestDescription", "TestCategory", TestTags, TestMetadata, TestPlayer, 1, 1.0f, 2.0f, 3.0f, true, false, true, false, true);
        var result = HandlerHelpers.ReadSink();
        Consumer.Consume(result);
        return result;
    }

    [Benchmark]
    public long Publish_MultiSubscribers_Prism()
    {
        HandlerHelpers.ResetSink();
        var evt = new Frameworks.EventTypes.PrismEventTypes.PrismHeavyPayloadEventData
        {
            EventId = TestEventId,
            Timestamp = DateTime.UtcNow.Ticks,
            Value1 = 1.5,
            Value2 = 2.5,
            CreatedAt = DateTime.UtcNow,
            Name = "TestName",
            Description = "TestDescription",
            Category = "TestCategory",
            Tags = TestTags,
            Metadata = TestMetadata,
            Player = TestPlayer,
            Id = 1,
            X = 1.0f,
            Y = 2.0f,
            Z = 3.0f,
            Flag1 = true,
            Flag2 = false,
            Flag3 = true,
            Flag4 = false,
            Flag5 = true
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
        BenchmarkEvents.PublishHeavyPayloadMultiEvent(TestEventId, DateTime.UtcNow.Ticks, 1.5, 2.5, DateTime.UtcNow, "TestName", "TestDescription", "TestCategory", TestTags, TestMetadata, TestPlayer, 1, 1.0f, 2.0f, 3.0f, true, false, true, false, true);
        var result = HandlerHelpers.ReadSink();
        Consumer.Consume(result);
        return result;
    }

    [Benchmark]
    public long Publish_NoSubscribers_Prism()
    {
        HandlerHelpers.ResetSink();
        var evt = new Frameworks.EventTypes.PrismEventTypes.PrismHeavyPayloadEventData
        {
            EventId = TestEventId,
            Timestamp = DateTime.UtcNow.Ticks,
            Value1 = 1.5,
            Value2 = 2.5,
            CreatedAt = DateTime.UtcNow,
            Name = "TestName",
            Description = "TestDescription",
            Category = "TestCategory",
            Tags = TestTags,
            Metadata = TestMetadata,
            Player = TestPlayer,
            Id = 1,
            X = 1.0f,
            Y = 2.0f,
            Z = 3.0f,
            Flag1 = true,
            Flag2 = false,
            Flag3 = true,
            Flag4 = false,
            Flag5 = true
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
        var token = BenchmarkEvents.SubscribeHeavyPayloadSingleEvent(HandlerHelpers.CreateHeavyPayloadSingleEventHandler(0));
        BenchmarkEvents.PublishHeavyPayloadSingleEvent(TestEventId, DateTime.UtcNow.Ticks, 1.5, 2.5, DateTime.UtcNow, "TestName", "TestDescription", "TestCategory", TestTags, TestMetadata, TestPlayer, 1, 1.0f, 2.0f, 3.0f, true, false, true, false, true);
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
        var evt = new Frameworks.EventTypes.PrismEventTypes.PrismHeavyPayloadEventData
        {
            EventId = TestEventId,
            Timestamp = DateTime.UtcNow.Ticks,
            Value1 = 1.5,
            Value2 = 2.5,
            CreatedAt = DateTime.UtcNow,
            Name = "TestName",
            Description = "TestDescription",
            Category = "TestCategory",
            Tags = TestTags,
            Metadata = TestMetadata,
            Player = TestPlayer,
            Id = 1,
            X = 1.0f,
            Y = 2.0f,
            Z = 3.0f,
            Flag1 = true,
            Flag2 = false,
            Flag3 = true,
            Flag4 = false,
            Flag5 = true
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










