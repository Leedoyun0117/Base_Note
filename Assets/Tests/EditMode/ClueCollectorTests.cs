using GameName.Core.Clues;
using GameName.Core.Emotions;
using GameName.Core.Inventory;
using GameName.Core.MemoryRooms;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    public class ClueCollectorTests
    {
        private static ClueDefinition MakeClueDefinition(ClueId id, MemoryRoomId roomId) =>
            new ClueDefinition(
                id,
                roomId,
                new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Sadness, 3) }),
                new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Sadness, 3) }));

        private static IPlayerInventory MakeInventory(int capacity = 2) =>
            new PlayerInventory(new InventorySettings(capacity), new SharedSlotInventoryPolicy());

        [Test]
        public void 같은_방에_있으면_단서를_집을_수_있다()
        {
            var roomId = new MemoryRoomId("room-1");
            var clueId = new ClueId("clue-1");
            var tracker = new MemoryRoomClueTracker(new[] { MakeClueDefinition(clueId, roomId) });
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
            var tracker = new MemoryRoomClueTracker(new[] { MakeClueDefinition(clueId, clueRoomId) });
            var location = new PlayerLocation(new MemoryGraphNodeId("room-2"));
            var inventory = MakeInventory();
            var collector = new ClueCollector(location, inventory, tracker);

            var result = collector.Collect(clueId);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ClueCollectionFailureReason.WrongRoom, result.FailureReason);
            Assert.AreEqual(0, inventory.Items.Count);
        }

        [Test]
        public void 이미_집은_단서를_다시_집을_수_없다()
        {
            var roomId = new MemoryRoomId("room-1");
            var clueId = new ClueId("clue-1");
            var tracker = new MemoryRoomClueTracker(new[] { MakeClueDefinition(clueId, roomId) });
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
            // 기억 방 화면의 단서 패널은 이 조회(GetAvailableClueInfos) 결과를
            // 그대로 그린다 — 여기서 사라짐을 증명하면 화면에서도 사라짐이
            // 보장된다.
            var roomId = new MemoryRoomId("room-1");
            var clueId = new ClueId("clue-1");
            var tracker = new MemoryRoomClueTracker(new[] { MakeClueDefinition(clueId, roomId) });
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
                MakeClueDefinition(clueId1, roomId),
                MakeClueDefinition(clueId2, roomId),
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
