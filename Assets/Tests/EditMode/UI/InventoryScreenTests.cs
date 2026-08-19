using System;
using GameName.Core.Clues;
using GameName.Core.Emotions;
using GameName.Core.Events;
using GameName.Core.Inventory;
using GameName.Core.MemoryRooms;
using GameName.UI.Inventory;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace GameName.UI.Tests.EditMode
{
    // I 키로 여는 인벤토리 화면의 두 가지를 검증한다.
    //   · 칸 개수가 Core 설정값을 따른다.
    //   · 기억 방이 아닌 곳에서 버리려 하면 사유가 뜬다.
    public class InventoryScreenTests
    {
        private static readonly MemoryRoomId Room1 = new MemoryRoomId("room-1");
        private static readonly MemoryGraphNodeId Room1Node = MemoryGraphNodeId.OfRoom(Room1);
        private static readonly MemoryGraphNodeId AnalysisRoomNode = new MemoryGraphNodeId("analysis-room");

        private static VisualElement MakeInventoryRoot()
        {
            var root = new VisualElement();
            root.Add(new Label { name = "inventory-count" });
            root.Add(new VisualElement { name = "inventory-slot-grid" });
            root.Add(new Label { name = "inventory-message" });
            return root;
        }

        private static ClueDefinition MakeDefinition(string id) =>
            new ClueDefinition(
                new ClueId(id), ClueKind.FloorObject, new CluePositionRatio(0.5f),
                new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Joy, 3) }),
                new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Joy, 3) }));

        private static MemoryRoomGraph MakeGraph()
        {
            var nodes = new[]
            {
                new MemoryGraphNode(Room1Node, MemoryGraphNodeType.MemoryRoom, new MemoryGraphCoordinate(0, 1)),
                new MemoryGraphNode(AnalysisRoomNode, MemoryGraphNodeType.AnalysisRoom, new MemoryGraphCoordinate(0, 0)),
            };

            return new MemoryRoomGraph(
                nodes, new[] { new OpenConnection(AnalysisRoomNode, Room1Node) }, Array.Empty<LadderConnection>());
        }

        private static (InventoryScreenController Controller, VisualElement Root, PlayerLocation Location)
            MakeFixture(int capacity)
        {
            var definition = MakeDefinition("clue-1");
            var tracker = new MemoryRoomClueTracker(new[] { new CluePlacement(Room1, definition) });
            var inventory = new PlayerInventory(new InventorySettings(capacity), new SharedSlotInventoryPolicy());
            var location = new PlayerLocation(Room1Node);
            var eventBus = new EventBus(new NoOpEventExceptionHandler());

            var collector = new ClueCollector(location, inventory, tracker);
            collector.Collect(definition.Id);

            var dropProcessor = new ClueDropProcessor(location, MakeGraph(), inventory, tracker, eventBus);

            var root = MakeInventoryRoot();
            var view = new InventoryScreenView(root);
            var controller = new InventoryScreenController(view, inventory, dropProcessor);
            controller.Refresh();

            return (controller, root, location);
        }

        [Test]
        public void 칸_개수는_Core_설정값을_따른다()
        {
            var grid = MakeFixture(capacity: 4).Root.Q<VisualElement>("inventory-slot-grid");

            Assert.AreEqual(4, grid.childCount);
        }

        [Test]
        public void 용량이_다르면_칸_개수도_다르다()
        {
            var grid = MakeFixture(capacity: 2).Root.Q<VisualElement>("inventory-slot-grid");

            Assert.AreEqual(2, grid.childCount);
        }

        [Test]
        public void 기억_방_안에서_버리면_인벤토리에서_빠진다()
        {
            var (controller, root, _) = MakeFixture(capacity: 4);

            controller.RequestDrop(new ClueId("clue-1"));

            Assert.AreEqual("0 / 4", root.Q<Label>("inventory-count").text);

            // 성공도 결과다 — 아무 말이 없으면 통했는지 알 수 없다.
            Assert.AreEqual("서 있던 자리에 내려놓았습니다.", root.Q<Label>("inventory-message").text);
        }

        [Test]
        public void 기억_방이_아닌_곳에서는_사유가_뜨고_그대로_남는다()
        {
            var (controller, root, location) = MakeFixture(capacity: 4);
            location.MoveTo(AnalysisRoomNode);

            controller.RequestDrop(new ClueId("clue-1"));

            Assert.AreEqual("기억 방 안에서만 단서를 버릴 수 있습니다.", root.Q<Label>("inventory-message").text);
            Assert.AreEqual("1 / 4", root.Q<Label>("inventory-count").text);
        }
    }
}
