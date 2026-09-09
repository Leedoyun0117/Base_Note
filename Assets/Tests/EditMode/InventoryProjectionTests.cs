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
    // 인벤토리는 ClueState의 투영이다 — 수집 사건에 채워지고, 추출·버리기에 비워진다.
    // 방이 바뀌는 것만으로는 비워지지 않는다(런 전체에 걸쳐 누적).
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
        public void 추출_사건이_와도_그_단서는_인벤토리에_남는다()
        {
            var fx = new Fixture("clue-1", "clue-2");
            fx.Bus.Publish(new ClueCollectedEvent(new ClueId("clue-1"), TheRoom));
            fx.Bus.Publish(new ClueCollectedEvent(new ClueId("clue-2"), TheRoom));

            fx.Bus.Publish(new ClueExtractedEvent(new ClueId("clue-1")));

            Assert.IsTrue(fx.Holds("clue-1"), "추출은 손에서 빼지 않는다 — 그 자리에서 답으로 낼 수 있어야 한다.");
            Assert.IsTrue(fx.Holds("clue-2"));
        }

        [Test]
        public void 방이_시작돼도_인벤토리는_유지된다()
        {
            var fx = new Fixture("clue-1", "clue-2");
            fx.Bus.Publish(new ClueCollectedEvent(new ClueId("clue-1"), TheRoom));
            fx.Bus.Publish(new ClueCollectedEvent(new ClueId("clue-2"), TheRoom));

            fx.Bus.Publish(new RoomStartedEvent(new MemoryRoomId("room-2"), 1));

            Assert.AreEqual(2, fx.Inventory.Items.Count, "런 전체에 걸쳐 누적되므로 방 전환으로 비워지면 안 된다.");
        }

        [Test]
        public void 버리기_사건이_오면_그_단서가_인벤토리에서_빠진다()
        {
            var fx = new Fixture("clue-1", "clue-2");
            fx.Bus.Publish(new ClueCollectedEvent(new ClueId("clue-1"), TheRoom));
            fx.Bus.Publish(new ClueCollectedEvent(new ClueId("clue-2"), TheRoom));

            fx.Bus.Publish(new ClueDiscardedEvent(new ClueId("clue-1")));

            Assert.IsFalse(fx.Holds("clue-1"));
            Assert.IsTrue(fx.Holds("clue-2"));
        }
    }
}
