using GameName.Core.Clues;
using GameName.Core.Inventory;
using GameName.Core.MemoryRooms;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    public class ClueCollectorTests
    {
        private static ClueDefinition MakeClueDefinition(ClueId id) =>
            new ClueDefinition(
                id,
                ClueKind.FloorObject,
                new CluePositionRatio(0.4f));

        private static CluePlacement Place(MemoryRoomId roomId, ClueId id) =>
            new CluePlacement(roomId, MakeClueDefinition(id));

        private static IPlayerInventory MakeInventory(int capacity = 2) =>
            new PlayerInventory(new InventorySettings(capacity), new SharedSlotInventoryPolicy());

        [Test]
        public void 같은_방에_있으면_단서를_집을_수_있다()
        {
            var roomId = new MemoryRoomId("room-1");
            var clueId = new ClueId("clue-1");
            var tracker = new MemoryRoomClueTracker(new[] { Place(roomId, clueId) });
            var location = new PlayerLocation(MemoryGraphNodeId.OfRoom(roomId));
            var inventory = MakeInventory();
            var collector = new ClueCollector(location, inventory, tracker);

            var result = collector.Collect(clueId);

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(1, inventory.Items.Count);
        }

        [Test]
        public void 다른_방의_단서는_집을_수_없다()
        {
            var clueRoomId = new MemoryRoomId("room-1");
            var clueId = new ClueId("clue-1");
            var tracker = new MemoryRoomClueTracker(new[] { Place(clueRoomId, clueId) });
            var location = new PlayerLocation(new MemoryGraphNodeId("room-2"));
            var inventory = MakeInventory();
            var collector = new ClueCollector(location, inventory, tracker);

            var result = collector.Collect(clueId);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ClueCollectionFailureReason.WrongRoom, result.FailureReason);
            Assert.AreEqual(0, inventory.Items.Count);
        }

        // 소속 방이 바뀐 뒤에는 "원래 있던 방"이 아니라 "지금 있는 방"이
        // 기준이어야 한다 — 습득 규칙이 정의가 아니라 추적기의 배치를 본다는
        // 것을 증명한다.
        [Test]
        public void 재배정된_단서는_새_방에서_집을_수_있다()
        {
            var origin = new MemoryRoomId("room-1");
            var destination = new MemoryRoomId("room-2");
            var clueId = new ClueId("clue-1");
            var tracker = new MemoryRoomClueTracker(new[] { Place(origin, clueId) });
            tracker.PlaceInRoom(clueId, destination);

            var location = new PlayerLocation(MemoryGraphNodeId.OfRoom(destination));
            var inventory = MakeInventory();
            var collector = new ClueCollector(location, inventory, tracker);

            var result = collector.Collect(clueId);

            Assert.IsTrue(result.Succeeded);
        }

        [Test]
        public void 이미_집은_단서를_다시_집을_수_없다()
        {
            var roomId = new MemoryRoomId("room-1");
            var clueId = new ClueId("clue-1");
            var tracker = new MemoryRoomClueTracker(new[] { Place(roomId, clueId) });
            var location = new PlayerLocation(MemoryGraphNodeId.OfRoom(roomId));
            var inventory = MakeInventory();
            var collector = new ClueCollector(location, inventory, tracker);

            var first = collector.Collect(clueId);
            var second = collector.Collect(clueId);

            Assert.IsTrue(first.Succeeded);
            Assert.IsFalse(second.Succeeded);
            Assert.AreEqual(ClueCollectionFailureReason.NotAvailable, second.FailureReason);
            Assert.AreEqual(1, inventory.Items.Count);
        }

        [Test]
        public void 단서를_집으면_방의_남은_단서_목록에서_사라진다()
        {
            // 씬의 단서 오브젝트는 이 조회(GetAvailableClueInfos) 결과를 그대로
            // 그린다 — 여기서 사라짐을 증명하면 방에서도 사라짐이 보장된다.
            var roomId = new MemoryRoomId("room-1");
            var clueId = new ClueId("clue-1");
            var tracker = new MemoryRoomClueTracker(new[] { Place(roomId, clueId) });
            var location = new PlayerLocation(MemoryGraphNodeId.OfRoom(roomId));
            var inventory = MakeInventory();
            var collector = new ClueCollector(location, inventory, tracker);

            var beforeCount = tracker.GetAvailableClueInfos(roomId).Count;
            collector.Collect(clueId);
            var afterCount = tracker.GetAvailableClueInfos(roomId).Count;

            Assert.AreEqual(1, beforeCount);
            Assert.AreEqual(0, afterCount);
        }

        [Test]
        public void 인벤토리가_가득_차면_InventoryFull_사유로_실패한다()
        {
            var roomId = new MemoryRoomId("room-1");
            var clueId1 = new ClueId("clue-1");
            var clueId2 = new ClueId("clue-2");
            var tracker = new MemoryRoomClueTracker(new[]
            {
                Place(roomId, clueId1),
                Place(roomId, clueId2),
            });
            var location = new PlayerLocation(MemoryGraphNodeId.OfRoom(roomId));
            var inventory = MakeInventory(capacity: 1);
            var collector = new ClueCollector(location, inventory, tracker);

            var firstResult = collector.Collect(clueId1);
            var secondResult = collector.Collect(clueId2);

            Assert.IsTrue(firstResult.Succeeded);
            Assert.IsFalse(secondResult.Succeeded);
            Assert.AreEqual(ClueCollectionFailureReason.InventoryFull, secondResult.FailureReason);
        }
    }
}
