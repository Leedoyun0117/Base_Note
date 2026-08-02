using GameName.Core.Ampoules;
using GameName.Core.Emotions;
using GameName.Core.MemoryRooms;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    public class AmpouleStorageTests
    {
        private static Ampoule MakeAmpoule(string id) =>
            new Ampoule(
                new AmpouleId(id),
                new MemoryRoomId("room-1"),
                new Scent(EmotionType.Joy, new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Love, 1) })));

        [Test]
        public void 빈_보관함에_담으면_성공한다()
        {
            var storage = new AmpouleStorage(new AmpouleStorageSettings(3));

            var result = storage.TryStore(MakeAmpoule("ampoule-1"));

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(1, storage.Ampoules.Count);
        }

        [Test]
        public void 상한을_넘으면_담기가_사유와_함께_실패한다()
        {
            var storage = new AmpouleStorage(new AmpouleStorageSettings(1));
            storage.TryStore(MakeAmpoule("ampoule-1"));

            var result = storage.TryStore(MakeAmpoule("ampoule-2"));

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(AmpouleStorageStoreFailureReason.Full, result.FailureReason);
        }

        [Test]
        public void 같은_앰플을_중복으로_담으면_실패한다()
        {
            var storage = new AmpouleStorage(new AmpouleStorageSettings(3));
            var ampoule = MakeAmpoule("ampoule-1");
            storage.TryStore(ampoule);

            var result = storage.TryStore(ampoule);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(AmpouleStorageStoreFailureReason.Duplicate, result.FailureReason);
        }

        [Test]
        public void 담긴_앰플을_꺼낼_수_있다()
        {
            var storage = new AmpouleStorage(new AmpouleStorageSettings(3));
            var ampoule = MakeAmpoule("ampoule-1");
            storage.TryStore(ampoule);

            var result = storage.TryRemove(ampoule);

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(0, storage.Ampoules.Count);
        }

        [Test]
        public void 없는_앰플을_꺼내려_하면_NotFound_사유로_실패한다()
        {
            var storage = new AmpouleStorage(new AmpouleStorageSettings(3));

            var result = storage.TryRemove(MakeAmpoule("ampoule-1"));

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(AmpouleStorageRemoveFailureReason.NotFound, result.FailureReason);
        }

        [Test]
        public void CanAccept은_상한을_넘기지_않는_추가만_허용한다()
        {
            var storage = new AmpouleStorage(new AmpouleStorageSettings(3));
            storage.TryStore(MakeAmpoule("ampoule-1"));

            Assert.IsTrue(storage.CanAccept(2));
            Assert.IsFalse(storage.CanAccept(3));
        }
    }
}
