using GameName.Core.Analysis;
using GameName.Core.Ampoules;
using GameName.Core.Clues;
using GameName.Core.Emotions;
using GameName.Core.Events;
using GameName.Core.Inventory;
using GameName.Core.Journal;
using GameName.Core.Mentality;
using GameName.Core.MemoryRooms;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    // 섹션 2 검증: 인벤토리의 단서를 원래 방에 되돌려놓는 기능.
    public class ClueReturnProcessorTests
    {
        private static readonly MemoryRoomId Room1 = new MemoryRoomId("room-1");
        private static readonly MemoryGraphNodeId Room1Node = MemoryGraphNodeId.OfRoom(Room1);
        private static readonly MemoryGraphNodeId OtherRoomNode = new MemoryGraphNodeId("room-2");
        private static readonly MemoryGraphNodeId AnalysisRoomNode = new MemoryGraphNodeId("analysis-room");

        private sealed class Fixture
        {
            public MemoryRoomClueTracker Tracker;
            public PlayerInventory Inventory;
            public PlayerLocation PlayerLocation;
            public ClueCollector Collector;
            public ClueReturnProcessor ReturnProcessor;
            public ClueAnalyzer Analyzer;
            public ClueAnalysisProgress AnalysisProgress;
            public PlayerJournal Journal;
            public ClueDefinition ClueDefinition;
        }

        private static Fixture MakeFixture()
        {
            var clueDefinition = new ClueDefinition(
                new ClueId("clue-1"), Room1,
                new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Love, 5) }),
                new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Love, 5) }));

            var eventBus = new EventBus(new NoOpEventExceptionHandler());
            var tracker = new MemoryRoomClueTracker(new[] { clueDefinition });
            var inventory = new PlayerInventory(new InventorySettings(5), new SharedSlotInventoryPolicy());
            var playerLocation = new PlayerLocation(Room1Node);
            var collector = new ClueCollector(playerLocation, inventory, tracker);
            var returnProcessor = new ClueReturnProcessor(playerLocation, inventory, tracker, eventBus);

            var mentalityCostSettings = new MentalityCostSettings(100, 100, 1, 20, 30, 8, 20);
            var gauge = new MentalityGauge(mentalityCostSettings, eventBus);
            var analysisProgress = new ClueAnalysisProgress();
            var storage = new ClueStorage(new ClueStorageSettings(6));
            var analyzer = new ClueAnalyzer(
                playerLocation, AnalysisRoomNode, gauge, mentalityCostSettings, eventBus,
                analysisProgress, tracker, inventory, storage);

            var ampouleStorage = new AmpouleStorage(new AmpouleStorageSettings(3));
            var journal = new PlayerJournal(eventBus, ampouleStorage, inventory);

            return new Fixture
            {
                Tracker = tracker,
                Inventory = inventory,
                PlayerLocation = playerLocation,
                Collector = collector,
                ReturnProcessor = returnProcessor,
                Analyzer = analyzer,
                AnalysisProgress = analysisProgress,
                Journal = journal,
                ClueDefinition = clueDefinition,
            };
        }

        [Test]
        public void 되돌려놓은_단서가_원래_방에_다시_나타나고_다시_주울_수_있다()
        {
            var fixture = MakeFixture();
            fixture.Collector.Collect(fixture.ClueDefinition.Id);
            var clueInfo = fixture.ClueDefinition.ToInfo();

            var result = fixture.ReturnProcessor.Return(clueInfo);

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(0, fixture.Inventory.Items.Count);

            var availableAfterReturn = fixture.Tracker.GetAvailableClueInfos(Room1);
            Assert.AreEqual(1, availableAfterReturn.Count);

            var recollectResult = fixture.Collector.Collect(fixture.ClueDefinition.Id);
            Assert.IsTrue(recollectResult.Succeeded);
        }

        [Test]
        public void 다른_방에서는_되돌려놓을_수_없다()
        {
            var fixture = MakeFixture();
            fixture.Collector.Collect(fixture.ClueDefinition.Id);
            var clueInfo = fixture.ClueDefinition.ToInfo();

            fixture.PlayerLocation.MoveTo(OtherRoomNode);
            var result = fixture.ReturnProcessor.Return(clueInfo);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ClueReturnFailureReason.WrongRoom, result.FailureReason);
            Assert.AreEqual(1, fixture.Inventory.Items.Count);
        }

        [Test]
        public void 되돌려놓아도_분석_진행도와_기록지_기록이_유지된다()
        {
            var fixture = MakeFixture();
            var commissionId = new CommissionId("commission-1");
            fixture.Journal.BeginCommission(commissionId);

            fixture.Collector.Collect(fixture.ClueDefinition.Id);

            fixture.PlayerLocation.MoveTo(AnalysisRoomNode);
            fixture.Analyzer.Analyze(fixture.ClueDefinition.Id, AnalysisDepth.Advanced);

            fixture.PlayerLocation.MoveTo(Room1Node);
            var result = fixture.ReturnProcessor.Return(fixture.ClueDefinition.ToInfo());

            Assert.IsTrue(result.Succeeded);
            Assert.IsTrue(fixture.AnalysisProgress.TryGetBestDepth(fixture.ClueDefinition.Id, out var depth));
            Assert.AreEqual(AnalysisDepth.Advanced, depth);
            Assert.AreEqual(1, fixture.Journal.GetAnalyses(commissionId).Count);
        }
    }
}
