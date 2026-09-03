using System.Collections.Generic;
using GameName.Core.Clues;
using GameName.Core.Events;
using GameName.Core.Inventory;
using GameName.Core.Memories;
using GameName.Core.MemoryRooms;
using GameName.UI.Inventory;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace GameName.UI.Tests.EditMode
{
    // I 키로 여는 인벤토리 화면:
    //   · 칸 개수가 Core 설정값을 따른다.
    //   · 표시 전용이다 — 단서를 담는 것은 ClueState의 투영이 한다.
    public class InventoryScreenTests
    {
        private static readonly MemoryRoomId Room1 = new MemoryRoomId("room-1");

        private static VisualElement MakeInventoryRoot()
        {
            var root = new VisualElement();
            root.Add(new Label { name = "inventory-count" });
            root.Add(new VisualElement { name = "inventory-slot-grid" });
            root.Add(new Label { name = "inventory-message" });
            return root;
        }

        private static ClueDefinition MakeDefinition(string id) =>
            new ClueDefinition(new ClueId(id), ClueKind.FloorObject, id, new CluePositionRatio(0.5f), MemoryColor.Red);

        private sealed class Fixture
        {
            public readonly EventBus Bus = new EventBus(new NoOpEventExceptionHandler());
            public readonly InventoryScreenController Controller;
            public readonly VisualElement Root;
            public readonly PlayerInventory Inventory;

            public Fixture(int capacity, params string[] clueIds)
            {
                var placements = new List<CluePlacement>();
                foreach (var id in clueIds)
                    placements.Add(new CluePlacement(Room1, MakeDefinition(id)));
                var tracker = new MemoryRoomClueTracker(placements);

                Inventory = new PlayerInventory(new InventorySettings(capacity), new SharedSlotInventoryPolicy());
                _ = new InventoryProjection(Inventory, tracker, Bus);

                Root = MakeInventoryRoot();
                Controller = new InventoryScreenController(new InventoryScreenView(Root), Inventory);
                Controller.Refresh();
            }

            public VisualElement Grid => Root.Q<VisualElement>("inventory-slot-grid");
            public Label Count => Root.Q<Label>("inventory-count");
        }

        [Test]
        public void 칸_개수는_Core_설정값을_따른다()
        {
            Assert.AreEqual(4, new Fixture(capacity: 4).Grid.childCount);
        }

        [Test]
        public void 용량이_다르면_칸_개수도_다르다()
        {
            Assert.AreEqual(2, new Fixture(capacity: 2).Grid.childCount);
        }

        [Test]
        public void 단서를_수집하면_다시_그렸을_때_칸에_나타난다()
        {
            var fx = new Fixture(4, "clue-1");

            fx.Bus.Publish(new ClueCollectedEvent(new ClueId("clue-1"), Room1));
            fx.Controller.Refresh();

            Assert.AreEqual("1 / 4", fx.Count.text);
            Assert.AreEqual(1, fx.Inventory.Items.Count);
        }
    }
}
