using GameName.Core.Ampoules;
using GameName.Core.Emotions;
using GameName.Core.Inventory;
using GameName.Core.MemoryRooms;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    public class AmpouleTransferProcessorTests
    {
        private static readonly MemoryGraphNodeId PerfumeryRoom = new MemoryGraphNodeId("perfumery-room");
        private static readonly MemoryGraphNodeId OtherRoom = new MemoryGraphNodeId("other-room");

        private static Ampoule MakeAmpoule(string id) =>
            new Ampoule(
                new AmpouleId(id),
                new MemoryRoomId("room-1"),
                new Scent(EmotionType.Joy, new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Love, 1) })));

        private static AmpouleTransferProcessor MakeProcessor(
            MemoryGraphNodeId playerPosition,
            out IAmpouleStorage storage,
            out IPlayerInventory inventory,
            int storageCapacity = 3,
            int inventoryCapacity = 3)
        {
            storage = new AmpouleStorage(new AmpouleStorageSettings(storageCapacity));
            inventory = new PlayerInventory(new InventorySettings(inventoryCapacity), new SharedSlotInventoryPolicy());
            var location = new PlayerLocation(playerPosition);

            return new AmpouleTransferProcessor(location, PerfumeryRoom, storage, inventory);
        }

        [Test]
        public void 조향실에서_보관함의_앰플을_인벤토리로_옮길_수_있다()
        {
            var processor = MakeProcessor(PerfumeryRoom, out var storage, out var inventory);
            var ampoule = MakeAmpoule("ampoule-1");
            storage.TryStore(ampoule);

            var result = processor.MoveToInventory(ampoule);

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(0, storage.Ampoules.Count);
            Assert.AreEqual(1, inventory.Items.Count);
        }

        [Test]
        public void 조향실에서_인벤토리의_앰플을_보관함으로_옮길_수_있다()
        {
            var processor = MakeProcessor(PerfumeryRoom, out var storage, out var inventory);
            var ampoule = MakeAmpoule("ampoule-1");
            inventory.TryStore(ampoule);

            var result = processor.MoveToStorage(ampoule);

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(1, storage.Ampoules.Count);
            Assert.AreEqual(0, inventory.Items.Count);
        }

        [Test]
        public void 조향실이_아니면_어느_방향으로도_옮길_수_없다()
        {
            var processor = MakeProcessor(OtherRoom, out var storage, out var inventory);
            var ampoule = MakeAmpoule("ampoule-1");
            storage.TryStore(ampoule);

            var result = processor.MoveToInventory(ampoule);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(AmpouleTransferFailureReason.NotInPerfumeryRoom, result.FailureReason);
            Assert.AreEqual(1, storage.Ampoules.Count);
        }

        [Test]
        public void 출발지에_없는_앰플은_AmpouleNotFound_사유로_실패한다()
        {
            var processor = MakeProcessor(PerfumeryRoom, out _, out _);
            var ampoule = MakeAmpoule("ampoule-1");

            var result = processor.MoveToInventory(ampoule);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(AmpouleTransferFailureReason.AmpouleNotFound, result.FailureReason);
        }

        [Test]
        public void 목적지가_가득_차면_DestinationFull_사유로_실패하고_출발지에_그대로_남는다()
        {
            var processor = MakeProcessor(PerfumeryRoom, out var storage, out var inventory, inventoryCapacity: 0);
            var ampoule = MakeAmpoule("ampoule-1");
            storage.TryStore(ampoule);

            var result = processor.MoveToInventory(ampoule);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(AmpouleTransferFailureReason.DestinationFull, result.FailureReason);
            Assert.AreEqual(1, storage.Ampoules.Count);
            Assert.AreEqual(0, inventory.Items.Count);
        }
    }
}
