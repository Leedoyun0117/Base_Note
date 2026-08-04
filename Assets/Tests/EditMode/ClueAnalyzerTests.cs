using System.Linq;
using GameName.Core.Analysis;
using GameName.Core.Clues;
using GameName.Core.Emotions;
using GameName.Core.Events;
using GameName.Core.Inventory;
using GameName.Core.Mentality;
using GameName.Core.MemoryRooms;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    public class ClueAnalyzerTests
    {
        private sealed class AnalyzerFixture
        {
            public ClueAnalyzer Analyzer;
            public IMentalityGauge Gauge;
            public EventBus EventBus;
            public ClueAnalysisProgress Progress;
            public IPlayerInventory Inventory;
        }

        private static readonly MemoryGraphNodeId AnalysisRoom = new MemoryGraphNodeId("analysis-room");
        private static readonly MemoryGraphNodeId OtherRoom = new MemoryGraphNodeId("other-room");

        private static ClueDefinition MakeClue(EmotionBlend apparent, EmotionBlend actual = null) =>
            new ClueDefinition(new ClueId("clue-1"), new MemoryRoomId("room-1"), apparent, actual ?? apparent);

        private static AnalyzerFixture MakeFixture(
            MemoryGraphNodeId playerPosition,
            ClueDefinition clueDefinition,
            int initialMentality = 100,
            bool collectIntoInventory = true,
            bool collectIntoStorage = false)
        {
            var settings = new MentalityCostSettings(
                initialMentality: initialMentality, maxMentality: 100,
                memoryRoomMoveCost: 1, basicAnalysisCost: 20, advancedAnalysisCost: 30,
                ampouleCraftingCost: 8, memoryRoomFullRestorationRecovery: 20);
            var eventBus = new EventBus(new NoOpEventExceptionHandler());
            var gauge = new MentalityGauge(settings, eventBus);
            var progress = new ClueAnalysisProgress();
            var inventory = new PlayerInventory(new InventorySettings(10), new SharedSlotInventoryPolicy());
            var storage = new ClueStorage(new ClueStorageSettings(6));
            var tracker = new MemoryRoomClueTracker(new[] { clueDefinition });
            var location = new PlayerLocation(playerPosition);

            if (collectIntoInventory)
                inventory.TryStore(clueDefinition.ToInfo());
            if (collectIntoStorage)
                storage.TryStore(clueDefinition.ToInfo());

            var analyzer = new ClueAnalyzer(
                location, AnalysisRoom, gauge, settings, eventBus, progress, tracker, inventory, storage);

            return new AnalyzerFixture
            {
                Analyzer = analyzer,
                Gauge = gauge,
                EventBus = eventBus,
                Progress = progress,
                Inventory = inventory,
            };
        }

        [Test]
        public void 일반_분석은_감정_종류만_반환한다()
        {
            var clue = MakeClue(new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Love, 5) }));
            var fixture = MakeFixture(AnalysisRoom, clue);

            var result = fixture.Analyzer.Analyze(clue.Id, AnalysisDepth.Basic);

            Assert.IsTrue(result.Succeeded);
            var detected = result.AnalysisResult.DetectedEmotions.Single();
            Assert.AreEqual(EmotionType.Love, detected.Emotion);
            Assert.IsFalse(detected.Intensity.HasValue);
        }

        [Test]
        public void 고급_분석은_세기까지_반환한다()
        {
            var clue = MakeClue(new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Love, 5) }));
            var fixture = MakeFixture(AnalysisRoom, clue);

            var result = fixture.Analyzer.Analyze(clue.Id, AnalysisDepth.Advanced);

            var detected = result.AnalysisResult.DetectedEmotions.Single();
            Assert.AreEqual(5, detected.Intensity);
        }

        [Test]
        public void 분석실이_아니면_분석이_실패하고_정신력이_줄지_않는다()
        {
            var clue = MakeClue(new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Love, 5) }));
            var fixture = MakeFixture(OtherRoom, clue);

            var result = fixture.Analyzer.Analyze(clue.Id, AnalysisDepth.Basic);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ClueAnalysisFailureReason.NotInAnalysisRoom, result.FailureReason);
            Assert.AreEqual(100, fixture.Gauge.CurrentValue);
        }

        [Test]
        public void 인벤토리에도_보관대에도_없는_단서는_분석할_수_없다()
        {
            var clue = MakeClue(new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Love, 5) }));
            var fixture = MakeFixture(AnalysisRoom, clue, collectIntoInventory: false);

            var result = fixture.Analyzer.Analyze(clue.Id, AnalysisDepth.Basic);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ClueAnalysisFailureReason.ClueNotAccessible, result.FailureReason);
            Assert.AreEqual(100, fixture.Gauge.CurrentValue);
        }

        [Test]
        public void 보관대에_있는_단서도_분석할_수_있다()
        {
            var clue = MakeClue(new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Love, 5) }));
            var fixture = MakeFixture(AnalysisRoom, clue, collectIntoInventory: false, collectIntoStorage: true);

            var result = fixture.Analyzer.Analyze(clue.Id, AnalysisDepth.Basic);

            Assert.IsTrue(result.Succeeded);
        }

        [Test]
        public void 정신력이_0이면_분석이_실패하고_정신력이_전혀_줄지_않는다()
        {
            var clue = MakeClue(new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Love, 5) }));
            var fixture = MakeFixture(AnalysisRoom, clue, initialMentality: 0);

            var result = fixture.Analyzer.Analyze(clue.Id, AnalysisDepth.Basic);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ClueAnalysisFailureReason.InsufficientMentality, result.FailureReason);
            Assert.AreEqual(0, fixture.Gauge.CurrentValue);
        }

        [Test]
        public void 같은_깊이로_재분석하면_실패하고_더_깊은_분석은_허용된다()
        {
            var clue = MakeClue(new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Love, 5) }));
            var fixture = MakeFixture(AnalysisRoom, clue);

            var first = fixture.Analyzer.Analyze(clue.Id, AnalysisDepth.Basic);
            var repeat = fixture.Analyzer.Analyze(clue.Id, AnalysisDepth.Basic);
            var deeper = fixture.Analyzer.Analyze(clue.Id, AnalysisDepth.Advanced);

            Assert.IsTrue(first.Succeeded);
            Assert.IsFalse(repeat.Succeeded);
            Assert.AreEqual(ClueAnalysisFailureReason.AlreadyAnalyzedAtSameOrDeeperDepth, repeat.FailureReason);
            Assert.IsTrue(deeper.Succeeded);

            // 기본 분석(20) + 고급 분석(30)만 소모되어야 한다. 실패한 재분석
            // 시도는 정신력을 전혀 건드리지 않는다.
            Assert.AreEqual(100 - 20 - 30, fixture.Gauge.CurrentValue);
        }

        [Test]
        public void 분석_진행도를_초기화하면_같은_깊이로_다시_분석할_수_있다()
        {
            var clue = MakeClue(new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Love, 5) }));
            var fixture = MakeFixture(AnalysisRoom, clue);

            fixture.Analyzer.Analyze(clue.Id, AnalysisDepth.Basic);
            var beforeReset = fixture.Analyzer.Analyze(clue.Id, AnalysisDepth.Basic);

            fixture.Progress.Reset();
            var afterReset = fixture.Analyzer.Analyze(clue.Id, AnalysisDepth.Basic);

            Assert.IsFalse(beforeReset.Succeeded);
            Assert.IsTrue(afterReset.Succeeded);
        }

        [Test]
        public void 거짓_단서는_실제_구성이_아니라_겉보기_구성을_반환한다()
        {
            var apparent = new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Joy, 5) });
            var truth = new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Fear, 9) });
            var clue = MakeClue(apparent, truth);
            var fixture = MakeFixture(AnalysisRoom, clue);

            var result = fixture.Analyzer.Analyze(clue.Id, AnalysisDepth.Advanced);

            var detected = result.AnalysisResult.DetectedEmotions.Single();
            Assert.AreEqual(EmotionType.Joy, detected.Emotion);
            Assert.AreEqual(5, detected.Intensity);
        }

        [Test]
        public void 분석에_성공하면_이벤트가_발행된다()
        {
            var clue = MakeClue(new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Love, 5) }));
            var fixture = MakeFixture(AnalysisRoom, clue);

            ClueAnalyzedEvent? received = null;
            using (fixture.EventBus.Subscribe<ClueAnalyzedEvent>(e => received = e))
            {
                fixture.Analyzer.Analyze(clue.Id, AnalysisDepth.Basic);
            }

            Assert.IsTrue(received.HasValue);
            Assert.AreEqual(clue.Id, received.Value.ClueId);
        }
    }
}
