using System;
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
    // 섹션 0 검증: 단서를 지금 있는 방 아무 곳에나 버릴 수 있고, 버린 단서는
    // 그 방 소속으로 재배정된다.
    public class ClueDropProcessorTests
    {
        private static readonly MemoryRoomId Room1 = new MemoryRoomId("room-1");
        private static readonly MemoryRoomId Room2 = new MemoryRoomId("room-2");
        private static readonly MemoryGraphNodeId Room1Node = MemoryGraphNodeId.OfRoom(Room1);
        private static readonly MemoryGraphNodeId Room2Node = MemoryGraphNodeId.OfRoom(Room2);
        private static readonly MemoryGraphNodeId AnalysisRoomNode = new MemoryGraphNodeId("analysis-room");

        private sealed class Fixture
        {
            public MemoryRoomClueTracker Tracker;
            public PlayerInventory Inventory;
            public PlayerLocation PlayerLocation;
            public ClueCollector Collector;
            public ClueDropProcessor DropProcessor;
            public ClueAnalyzer Analyzer;
            public ClueAnalysisProgress AnalysisProgress;
            public PlayerJournal Journal;
            public ClueDefinition ClueDefinition;
        }

        // 방 두 개와 분석실을 가진 최소 그래프. 버리기 처리기가 "지금 있는 곳이
        // 기억 방인가"를 노드 종류로 판단하므로 실제 그래프가 필요하다.
        private static MemoryRoomGraph MakeGraph()
        {
            var nodes = new[]
            {
                new MemoryGraphNode(Room1Node, MemoryGraphNodeType.MemoryRoom, new MemoryGraphCoordinate(0, 1)),
                new MemoryGraphNode(Room2Node, MemoryGraphNodeType.MemoryRoom, new MemoryGraphCoordinate(1, 1)),
                new MemoryGraphNode(AnalysisRoomNode, MemoryGraphNodeType.AnalysisRoom, new MemoryGraphCoordinate(0, 0)),
            };

            var openConnections = new[]
            {
                new OpenConnection(Room1Node, Room2Node),
                new OpenConnection(AnalysisRoomNode, Room1Node),
            };

            return new MemoryRoomGraph(nodes, openConnections, Array.Empty<LadderConnection>());
        }

        private static Fixture MakeFixture()
        {
            var clueDefinition = new ClueDefinition(
                new ClueId("clue-1"), ClueKind.Poster, new CluePositionRatio(0.3f),
                new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Love, 5) }),
                new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Love, 5) }));

            var eventBus = new EventBus(new NoOpEventExceptionHandler());
            var tracker = new MemoryRoomClueTracker(new[] { new CluePlacement(Room1, clueDefinition) });
            var inventory = new PlayerInventory(new InventorySettings(5), new SharedSlotInventoryPolicy());
            var playerLocation = new PlayerLocation(Room1Node);
            var collector = new ClueCollector(playerLocation, inventory, tracker);
            var dropProcessor = new ClueDropProcessor(playerLocation, MakeGraph(), inventory, tracker, eventBus);

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
                DropProcessor = dropProcessor,
                Analyzer = analyzer,
                AnalysisProgress = analysisProgress,
                Journal = journal,
                ClueDefinition = clueDefinition,
            };
        }

        [Test]
        public void 버린_단서가_방에_다시_나타나고_다시_주울_수_있다()
        {
            var fixture = MakeFixture();
            fixture.Collector.Collect(fixture.ClueDefinition.Id);

            var result = fixture.DropProcessor.Drop(fixture.ClueDefinition.ToInfo());

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(0, fixture.Inventory.Items.Count);
            Assert.AreEqual(1, fixture.Tracker.GetAvailableClueInfos(Room1).Count);

            var recollectResult = fixture.Collector.Collect(fixture.ClueDefinition.Id);
            Assert.IsTrue(recollectResult.Succeeded);
        }

        [Test]
        public void 원래_있던_방이_아니어도_버릴_수_있고_그_방_소속이_된다()
        {
            var fixture = MakeFixture();
            fixture.Collector.Collect(fixture.ClueDefinition.Id);

            fixture.PlayerLocation.MoveTo(Room2Node);
            var result = fixture.DropProcessor.Drop(fixture.ClueDefinition.ToInfo());

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(0, fixture.Tracker.GetAvailableClueInfos(Room1).Count);
            Assert.AreEqual(1, fixture.Tracker.GetAvailableClueInfos(Room2).Count);
        }

        [Test]
        public void 기억_방이_아닌_곳에서는_버릴_수_없다()
        {
            var fixture = MakeFixture();
            fixture.Collector.Collect(fixture.ClueDefinition.Id);

            fixture.PlayerLocation.MoveTo(AnalysisRoomNode);
            var result = fixture.DropProcessor.Drop(fixture.ClueDefinition.ToInfo());

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ClueDropFailureReason.NotInMemoryRoom, result.FailureReason);
            Assert.AreEqual(1, fixture.Inventory.Items.Count);
        }

        [Test]
        public void 들고_있지_않은_단서는_버릴_수_없다()
        {
            var fixture = MakeFixture();

            var result = fixture.DropProcessor.Drop(fixture.ClueDefinition.ToInfo());

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ClueDropFailureReason.ClueNotInInventory, result.FailureReason);
        }

        [Test]
        public void 버린_뒤에도_분석_진행도와_기록지_기록이_유지된다()
        {
            var fixture = MakeFixture();
            var commissionId = new CommissionId("commission-1");
            fixture.Journal.BeginCommission(commissionId);

            fixture.Collector.Collect(fixture.ClueDefinition.Id);

            fixture.PlayerLocation.MoveTo(AnalysisRoomNode);
            fixture.Analyzer.Analyze(fixture.ClueDefinition.Id, AnalysisDepth.Advanced);

            // 원래 있던 방(Room1)이 아니라 다른 방(Room2)에 버려도 마찬가지다.
            fixture.PlayerLocation.MoveTo(Room2Node);
            var result = fixture.DropProcessor.Drop(fixture.ClueDefinition.ToInfo());

            Assert.IsTrue(result.Succeeded);
            Assert.IsTrue(fixture.AnalysisProgress.TryGetBestDepth(fixture.ClueDefinition.Id, out var depth));
            Assert.AreEqual(AnalysisDepth.Advanced, depth);
            Assert.AreEqual(1, fixture.Journal.GetAnalyses(commissionId).Count);
        }
    }
}
