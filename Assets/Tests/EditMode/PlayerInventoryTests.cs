using GameName.Core.Clues;
using GameName.Core.Inventory;
using GameName.Core.MemoryRooms;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    public class PlayerInventoryTests
    {
        // 인벤토리는 IInventoryItem만 알면 되고 단서 고유의 저작 데이터는 몰라야
        // 하므로, 테스트에서도 항상 ClueInfo(공개용 뷰)만 담는다.
        private static ClueInfo MakeClueInfo(string id) =>
            new ClueInfo(
                new ClueId(id),
                ClueKind.FloorObject,
                new CluePositionRatio(0.5f));

        [Test]
        public void 빈_인벤토리에_담으면_성공한다()
        {
            var inventory = new PlayerInventory(new InventorySettings(2), new SharedSlotInventoryPolicy());

            var result = inventory.TryStore(MakeClueInfo("clue-1"));

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(1, inventory.Items.Count);
        }

        [Test]
        public void 용량이_가득_차면_담기가_사유와_함께_실패한다()
        {
            var inventory = new PlayerInventory(new InventorySettings(1), new SharedSlotInventoryPolicy());
            inventory.TryStore(MakeClueInfo("clue-1"));

            var result = inventory.TryStore(MakeClueInfo("clue-2"));

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(InventoryStoreFailureReason.Full, result.FailureReason);
            Assert.AreEqual(1, inventory.Items.Count);
        }

        [Test]
        public void 같은_단서를_중복으로_담으려_하면_사유와_함께_실패한다()
        {
            var inventory = new PlayerInventory(new InventorySettings(2), new SharedSlotInventoryPolicy());
            inventory.TryStore(MakeClueInfo("clue-1"));

            // 같은 ClueId를 가진 별개의 ClueInfo 인스턴스 — 참조가 달라도
            // 정체성(Id)이 같으면 중복으로 잡혀야 한다.
            var result = inventory.TryStore(MakeClueInfo("clue-1"));

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(InventoryStoreFailureReason.Duplicate, result.FailureReason);
            Assert.AreEqual(1, inventory.Items.Count);
        }

        [Test]
        public void 용량을_늘리면_더_담을_수_있다()
        {
            var inventory = new PlayerInventory(new InventorySettings(1), new SharedSlotInventoryPolicy());
            inventory.TryStore(MakeClueInfo("clue-1"));
            inventory.IncreaseCapacity(1);

            var result = inventory.TryStore(MakeClueInfo("clue-2"));

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(2, inventory.Items.Count);
        }

        [Test]
        public void 담긴_물건을_꺼낼_수_있다()
        {
            var inventory = new PlayerInventory(new InventorySettings(2), new SharedSlotInventoryPolicy());
            var clue = MakeClueInfo("clue-1");
            inventory.TryStore(clue);

            var result = inventory.TryRemove(clue);

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(0, inventory.Items.Count);
        }

        [Test]
        public void 없는_물건을_꺼내려_하면_NotFound_사유로_실패한다()
        {
            var inventory = new PlayerInventory(new InventorySettings(2), new SharedSlotInventoryPolicy());

            var result = inventory.TryRemove(MakeClueInfo("clue-1"));

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(InventoryRemoveFailureReason.NotFound, result.FailureReason);
        }

        [Test]
        public void 용량을_늘려도_이미_담긴_물건은_그대로_남는다()
        {
            var inventory = new PlayerInventory(new InventorySettings(2), new SharedSlotInventoryPolicy());
            inventory.TryStore(MakeClueInfo("clue-1"));

            inventory.IncreaseCapacity(3);

            Assert.AreEqual(1, inventory.Items.Count);
            Assert.AreEqual(5, inventory.Capacity);
        }
    }
}
