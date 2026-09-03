using System;
using System.Linq;
using GameName.Core.Clues;
using GameName.Core.Dialogue;
using GameName.Core.Events;
using GameName.Core.Inventory;
using GameName.Core.Memories;
using GameName.Core.MemoryRooms;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    // 인벤토리는 ClueState의 투영이다 — 수집 사건에 채워지고, 추출·방 시작에 비워진다.
    public class InventoryProjectionTests
    {
        private static readonly MemoryRoomId TheRoom = new MemoryRoomId("room-1");

        private static ClueDefinition Clue(string id) =>
            new ClueDefinition(new ClueId(id), ClueKind.Poster, id, new CluePositionRatio(0.5f), MemoryColor.Red);

        private sealed class Fixture
        {
            public readonly EventBus Bus = new EventBus(new NoOpEventExceptionHandler());
            public readonly PlayerInventory Inventory =
                new PlayerInventory(new InventorySettings(4), new SharedSlotInventoryPolicy());

            public Fixture(params string[] clueIds)
            {
                var placements = clueIds.Select(id => new CluePlacement(TheRoom, Clue(id))).ToArray();
                var tracker = new MemoryRoomClueTracker(placements);
                _ = new InventoryProjection(Inventory, tracker, Bus);
            }

            public bool Holds(string id) =>
                Inventory.Items.OfType<ClueInfo>().Any(c => c.Id.Equals(new ClueId(id)));
        }

        [Test]
        public void 수집_사건이_오면_그_단서가_인벤토리에_담긴다()
        {
            var fx = new Fixture("clue-1");

            fx.Bus.Publish(new ClueCollectedEvent(new ClueId("clue-1"), TheRoom));

            Assert.IsTrue(fx.Holds("clue-1"));
            Assert.AreEqual(1, fx.Inventory.Items.Count);
        }

        [Test]
        public void 추출_사건이_오면_그_단서가_인벤토리에서_빠진다()
        {
            var fx = new Fixture("clue-1", "clue-2");
            fx.Bus.Publish(new ClueCollectedEvent(new ClueId("clue-1"), TheRoom));
            fx.Bus.Publish(new ClueCollectedEvent(new ClueId("clue-2"), TheRoom));

            fx.Bus.Publish(new ClueExtractedEvent(new ClueId("clue-1"), remainingExtractions: 4));

            Assert.IsFalse(fx.Holds("clue-1"));
            Assert.IsTrue(fx.Holds("clue-2"));
        }

        [Test]
        public void 방이_시작되면_인벤토리가_비워진다()
        {
            var fx = new Fixture("clue-1", "clue-2");
            fx.Bus.Publish(new ClueCollectedEvent(new ClueId("clue-1"), TheRoom));
            fx.Bus.Publish(new ClueCollectedEvent(new ClueId("clue-2"), TheRoom));

            fx.Bus.Publish(new RoomStartedEvent(new MemoryRoomId("room-2"), 1));

            Assert.AreEqual(0, fx.Inventory.Items.Count);
        }

    }
}
