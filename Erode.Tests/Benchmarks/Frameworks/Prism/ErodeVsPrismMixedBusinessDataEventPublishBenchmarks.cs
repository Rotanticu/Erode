using Erode.Tests.Benchmarks.Frameworks.EventTypes;

namespace Erode.Tests.Benchmarks.Frameworks.Prism;

[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
[DisassemblyDiagnoser]
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

    // 缓存的 Handler 委托（在 GlobalSetup 中预先创建，避免每次调用都创建新实例）
    private InAction<MixedBusinessDataEvent> _warmupErodeHandler = null!;
    private InAction<MixedBusinessDataMultiEvent>[] _erodeMultiHandlers = Array.Empty<InAction<MixedBusinessDataMultiEvent>>();
    private Action<Frameworks.EventTypes.PrismEventTypes.PrismMixedBusinessDataEventData>[] _prismMultiHandlers = Array.Empty<Action<Frameworks.EventTypes.PrismEventTypes.PrismMixedBusinessDataEventData>>();
    private InAction<MixedBusinessDataSingleEvent> _singleSubscriberErodeHandler = null!;

    // 共享的测试数据
    private static readonly Guid TestSessionId = Guid.NewGuid();
    private static readonly Common.SharedTestTypes.PlayerInfo TestPlayer = new Common.SharedTestTypes.PlayerInfo { Id = 1, Name = "TestPlayer", Level = 10 };

    [GlobalSetup]
    public void Setup()
    {
        TestCleanupHelper.CleanupAll();
        HandlerHelpers.ResetSink();

        // 预先创建所有 Handler 委托（避免每次调用都创建新实例，消除对象分配）
        _warmupErodeHandler = HandlerHelpers.CreateMixedBusinessDataEventHandler(0);
        _singleSubscriberErodeHandler = HandlerHelpers.CreateMixedBusinessDataSingleEventHandler(0);
        
        // 预先创建多订阅者所需的 Handler 委托（最多 100 个）
        _erodeMultiHandlers = new InAction<MixedBusinessDataMultiEvent>[SubscriberCount];
        _prismMultiHandlers = new Action<Frameworks.EventTypes.PrismEventTypes.PrismMixedBusinessDataEventData>[SubscriberCount];
        for (int i = 0; i < SubscriberCount; i++)
        {
            _erodeMultiHandlers[i] = HandlerHelpers.CreateMixedBusinessDataMultiEventHandler(i);
            _prismMultiHandlers[i] = HandlerHelpers.CreatePrismMixedBusinessDataEventHandler(i);
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
        var warmupToken = BenchmarkEvents.SubscribeMixedBusinessDataEvent(_warmupErodeHandler);
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
        // Erode 多订阅者（使用 MixedBusinessDataMultiEvent 实现静态隔离）
        _erodeTokens = new SubscriptionToken[SubscriberCount];
        for (int i = 0; i < SubscriberCount; i++)
        {
            // 使用预先创建的委托，避免每次调用都创建新实例
            _erodeTokens[i] = BenchmarkEvents.SubscribeMixedBusinessDataMultiEvent(_erodeMultiHandlers[i]);
        }

        // Prism 多订阅者
        _eventAggregator = new global::Prism.Events.EventAggregator();
        _prismEvent = _eventAggregator.GetEvent<Frameworks.EventTypes.PrismEventTypes.PrismMixedBusinessDataEvent>();
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
        _noSubscribersPrismEvent = _noSubscribersEventAggregator.GetEvent<Frameworks.EventTypes.PrismEventTypes.PrismMixedBusinessDataEvent>();
    }

    private void SetupSingleSubscriber()
    {
        _singleSubscriberEventAggregator = new global::Prism.Events.EventAggregator();
        _singleSubscriberPrismEvent = _singleSubscriberEventAggregator.GetEvent<Frameworks.EventTypes.PrismEventTypes.PrismMixedBusinessDataEvent>();
        // 使用预先创建的委托（在 Setup 中已创建）
        _singleSubscriberPrismHandler = _prismMultiHandlers[0];
    }

    // ========== 多订阅者发布 ==========
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

    // ========== 无订阅者发布 ==========

    [Benchmark(Baseline = true)]
    public long Publish_NoSubscribers_Erode()
    {
        // 使用 MixedBusinessDataEmptyEvent 实现静态隔离，确保真正的"无订阅"状态
        // 不同的事件类型在静态空间中是物理隔离的，无需清理
        HandlerHelpers.ResetSink();
        BenchmarkEvents.PublishMixedBusinessDataEmptyEvent(TestSessionId, DateTime.UtcNow.Ticks, DateTime.UtcNow, "TestUser", TestPlayer, 1);
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

    // ========== 单订阅者发布（包含订阅+发布+取消） ==========

    [Benchmark]
    public long Publish_SingleSubscriber_Erode()
    {
        // 使用 MixedBusinessDataSingleEvent 实现静态隔离
        HandlerHelpers.ResetSink();
        // 使用预先创建的委托，避免每次调用都创建新实例
        var token = BenchmarkEvents.SubscribeMixedBusinessDataSingleEvent(_singleSubscriberErodeHandler);
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
        var token = _singleSubscriberPrismEvent.Subscribe(_singleSubscriberPrismHandler, ThreadOption.PublisherThread);
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










