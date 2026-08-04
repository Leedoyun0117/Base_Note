using GameName.Core.Clues;
using GameName.Core.Emotions;
using GameName.Core.Inventory;
using GameName.Core.MemoryRooms;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    public class ClueTransferProcessorTests
    {
        private static readonly MemoryGraphNodeId AnalysisRoom = new MemoryGraphNodeId("analysis-room");
        private static readonly MemoryGraphNodeId OtherRoom = new MemoryGraphNodeId("other-room");

        private static ClueInfo MakeClue(string id) =>
            new ClueInfo(
                new ClueId(id),
                new MemoryRoomId("room-1"),
                new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Love, 1) }));

        private static ClueTransferProcessor MakeProcessor(
            MemoryGraphNodeId playerPosition,
            out IClueStorage storage,
            out IPlayerInventory inventory,
            int storageCapacity = 3,
            int inventoryCapacity = 3)
        {
            storage = new ClueStorage(new ClueStorageSettings(storageCapacity));
            inventory = new PlayerInventory(new InventorySettings(inventoryCapacity), new SharedSlotInventoryPolicy());
            var location = new PlayerLocation(playerPosition);

            return new ClueTransferProcessor(location, AnalysisRoom, storage, inventory);
        }

        [Test]
        public void 분석실에서_인벤토리의_단서를_보관대로_옮기면_인벤토리_칸이_빈다()
        {
            var processor = MakeProcessor(AnalysisRoom, out var storage, out var inventory);
            var clue = MakeClue("clue-1");
            inventory.TryStore(clue);

            var result = processor.MoveToStorage(clue);

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(1, storage.Clues.Count);
            Assert.AreEqual(0, inventory.Items.Count);
        }

        [Test]
        public void 분석실에서_보관대의_단서를_인벤토리로_옮길_수_있다()
        {
            var processor = MakeProcessor(AnalysisRoom, out var storage, out var inventory);
            var clue = MakeClue("clue-1");
            storage.TryStore(clue);

            var result = processor.MoveToInventory(clue);

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(0, storage.Clues.Count);
            Assert.AreEqual(1, inventory.Items.Count);
        }

        [Test]
        public void 분석실이_아니면_어느_방향으로도_옮길_수_없다()
        {
            var processor = MakeProcessor(OtherRoom, out var storage, out var inventory);
            var clue = MakeClue("clue-1");
            inventory.TryStore(clue);

            var result = processor.MoveToStorage(clue);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ClueTransferFailureReason.NotInAnalysisRoom, result.FailureReason);
            Assert.AreEqual(1, inventory.Items.Count);
            Assert.AreEqual(0, storage.Clues.Count);
        }

        [Test]
        public void 출발지에_없는_단서는_ClueNotFound_사유로_실패한다()
        {
            var processor = MakeProcessor(AnalysisRoom, out _, out _);
            var clue = MakeClue("clue-1");

            var result = processor.MoveToStorage(clue);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ClueTransferFailureReason.ClueNotFound, result.FailureReason);
        }

        [Test]
        public void 목적지가_가득_차면_DestinationFull_사유로_실패하고_단서가_사라지지_않는다()
        {
            var processor = MakeProcessor(AnalysisRoom, out var storage, out var inventory, storageCapacity: 0);
            var clue = MakeClue("clue-1");
            inventory.TryStore(clue);

            var result = processor.MoveToStorage(clue);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ClueTransferFailureReason.DestinationFull, result.FailureReason);
            Assert.AreEqual(0, storage.Clues.Count);
            Assert.AreEqual(1, inventory.Items.Count);
        }
    }
}
