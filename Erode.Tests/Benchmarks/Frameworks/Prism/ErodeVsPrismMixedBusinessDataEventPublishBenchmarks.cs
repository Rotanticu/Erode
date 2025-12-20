using Erode.Tests.Benchmarks.Frameworks.EventTypes;

namespace Erode.Tests.Benchmarks.Frameworks.Prism;

[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
[DisassemblyDiagnoser]
public class ErodeVsPrismMixedBusinessDataEventPublishBenchmarks : BenchmarkBase
{
    private static readonly Consumer Consumer = new Consumer();

    // 共享的测试数据
    private static readonly Guid TestSessionId = Guid.NewGuid();
    private static readonly Common.SharedTestTypes.PlayerInfo TestPlayer = new Common.SharedTestTypes.PlayerInfo { Id = 1, Name = "TestPlayer", Level = 10 };

    // ========== 多订阅者发布（使用嵌套类，SubscriberCount 只影响这个类） ==========

    [SimpleJob(RuntimeMoniker.Net80)]
    [MemoryDiagnoser]
    [DisassemblyDiagnoser]
    public class MultiSubscribersBenchmarks
    {
        private static readonly Consumer Consumer = new Consumer();
        /// <summary>
        /// 订阅者数量。0 = 无订阅者场景，1/10/100 = 多订阅者场景
        /// </summary>
        [Params(0, 1, 10, 100)]
        public int SubscriberCount { get; set; }

        // Erode 订阅者
        private SubscriptionToken[] _erodeTokens = Array.Empty<SubscriptionToken>();

        // Prism EventAggregator（多订阅者场景）
        private global::Prism.Events.IEventAggregator _eventAggregator = null!;
        private Frameworks.EventTypes.PrismEventTypes.PrismMixedBusinessDataEvent _prismEvent = null!;
        private List<global::Prism.Events.SubscriptionToken> _prismTokens = new();

        // 缓存的 Handler 委托（在 GlobalSetup 中预先创建，避免每次调用都创建新实例）
        private InAction<MixedBusinessDataEvent> _warmupErodeHandler = null!;
        private InAction<MixedBusinessDataMultiEvent>[] _erodeMultiHandlers = Array.Empty<InAction<MixedBusinessDataMultiEvent>>();
        private Action<Frameworks.EventTypes.PrismEventTypes.PrismMixedBusinessDataEventData>[] _prismMultiHandlers = Array.Empty<Action<Frameworks.EventTypes.PrismEventTypes.PrismMixedBusinessDataEventData>>();

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
            // 当 SubscriberCount = 0 时，循环不执行，正好是无订阅者场景
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

        [GlobalCleanup]
        public void Cleanup()
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
        }
    }

    // ========== 生命周期测试（包含订阅+发布+取消） ==========

    // Prism EventAggregator（生命周期测试场景）
    private global::Prism.Events.IEventAggregator _lifecycleEventAggregator = null!;
    private Frameworks.EventTypes.PrismEventTypes.PrismMixedBusinessDataEvent _lifecyclePrismEvent = null!;
    private Action<Frameworks.EventTypes.PrismEventTypes.PrismMixedBusinessDataEventData> _lifecyclePrismHandler = null!;

    // 缓存的 Handler 委托（在 GlobalSetup 中预先创建，避免每次调用都创建新实例）
    private InAction<MixedBusinessDataSingleEvent> _lifecycleErodeHandler = null!;

    [GlobalSetup]
    public void Setup()
    {
        TestCleanupHelper.CleanupAll();
        HandlerHelpers.ResetSink();

        // 设置生命周期测试环境
        _lifecycleErodeHandler = HandlerHelpers.CreateMixedBusinessDataSingleEventHandler(0);
        _lifecycleEventAggregator = new global::Prism.Events.EventAggregator();
        _lifecyclePrismEvent = _lifecycleEventAggregator.GetEvent<Frameworks.EventTypes.PrismEventTypes.PrismMixedBusinessDataEvent>();
        _lifecyclePrismHandler = HandlerHelpers.CreatePrismMixedBusinessDataEventHandler(0);
    }

    [Benchmark]
    public long Publish_Lifecycle_Erode()
    {
        // 使用 MixedBusinessDataSingleEvent 实现静态隔离
        // 测试完整的订阅-发布-取消生命周期
        HandlerHelpers.ResetSink();
        // 使用预先创建的委托，避免每次调用都创建新实例
        var token = BenchmarkEvents.SubscribeMixedBusinessDataSingleEvent(_lifecycleErodeHandler);
        BenchmarkEvents.PublishMixedBusinessDataSingleEvent(TestSessionId, DateTime.UtcNow.Ticks, DateTime.UtcNow, "TestUser", TestPlayer, 1);
        token.Dispose();
        var result = HandlerHelpers.ReadSink();
        Consumer.Consume(result);
        return result;
    }

    [Benchmark]
    public long Publish_Lifecycle_Prism()
    {
        HandlerHelpers.ResetSink();
        var token = _lifecyclePrismEvent.Subscribe(_lifecyclePrismHandler, ThreadOption.PublisherThread);
        var evt = new Frameworks.EventTypes.PrismEventTypes.PrismMixedBusinessDataEventData
        {
            SessionId = TestSessionId,
            Timestamp = DateTime.UtcNow.Ticks,
            CreatedAt = DateTime.UtcNow,
            UserName = "TestUser",
            Player = TestPlayer,
            Id = 1
        };
        _lifecyclePrismEvent.Publish(evt);
        token.Dispose(); // Prism 的 SubscriptionToken 使用 Dispose()
        var result = HandlerHelpers.ReadSink();
        Consumer.Consume(result);
        return result;
    }
}
