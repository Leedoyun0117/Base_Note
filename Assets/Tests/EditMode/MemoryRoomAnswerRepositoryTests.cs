using System.Linq;
using GameName.Core.Emotions;
using GameName.Core.MemoryRooms;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    public class MemoryRoomAnswerRepositoryTests
    {
        private static MemoryRoomAnswer MakeAnswer(MemoryRoomId roomId) =>
            new MemoryRoomAnswer(roomId, new Scent(EmotionType.Joy, new EmotionBlend(new[]
            {
                new EmotionBlendEntry(EmotionType.Love, 10),
                new EmotionBlendEntry(EmotionType.Anger, 5),
            })));

        [Test]
        public void 등록된_방의_정답을_그대로_조회할_수_있다()
        {
            var roomId = new MemoryRoomId("room-1");
            var answer = MakeAnswer(roomId);
            var repository = new MemoryRoomAnswerRepository(new[] { answer });

            var found = repository.TryGetAnswer(roomId, out var result);

            Assert.IsTrue(found);
            Assert.AreEqual(answer.CorrectScent, result.CorrectScent);
        }

        [Test]
        public void 등록되지_않은_방은_정답_조회가_실패한다()
        {
            var repository = new MemoryRoomAnswerRepository(new[] { MakeAnswer(new MemoryRoomId("room-1")) });

            var found = repository.TryGetAnswer(new MemoryRoomId("room-2"), out _);

            Assert.IsFalse(found);
        }

        [Test]
        public void 공개_정보_조회는_요구_총량만_돌려준다()
        {
            var roomId = new MemoryRoomId("room-1");
            var repository = new MemoryRoomAnswerRepository(new[] { MakeAnswer(roomId) });

            var found = repository.TryGetPublicInfo(roomId, out var info);

            Assert.IsTrue(found);
            Assert.AreEqual(15, info.RequiredSupportingIntensityTotal);
        }

        // MemoryRoomPublicInfo 자체가 정답(Scent)을 담을 프로퍼티를 아예 갖지
        // 않는다는 것을 타입 수준에서 증명한다.
        [Test]
        public void 공개_정보_타입에는_정답_향을_담을_프로퍼티가_없다()
        {
            var publicPropertyTypes = typeof(MemoryRoomPublicInfo)
                .GetProperties()
                .Select(p => p.PropertyType)
                .ToArray();

            CollectionAssert.DoesNotContain(publicPropertyTypes, typeof(Scent));
        }
    }
}
