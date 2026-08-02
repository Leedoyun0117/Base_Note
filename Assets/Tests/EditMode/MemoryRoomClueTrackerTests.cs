using System.Linq;
using GameName.Core.Clues;
using GameName.Core.Emotions;
using GameName.Core.MemoryRooms;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    public class MemoryRoomClueTrackerTests
    {
        private static ClueDefinition MakeClueDefinition(string id, MemoryRoomId roomId) =>
            new ClueDefinition(
                new ClueId(id),
                roomId,
                new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Joy, 1) }),
                new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Joy, 1) }));

        [Test]
        public void 방별_조회는_그_방의_단서만_ClueInfo로_반환한다()
        {
            var roomA = new MemoryRoomId("room-a");
            var roomB = new MemoryRoomId("room-b");
            var tracker = new MemoryRoomClueTracker(new[]
            {
                MakeClueDefinition("clue-1", roomA),
                MakeClueDefinition("clue-2", roomA),
                MakeClueDefinition("clue-3", roomB),
            });

            var infosInRoomA = tracker.GetAvailableClueInfos(roomA);

            Assert.AreEqual(2, infosInRoomA.Count);
            Assert.IsTrue(infosInRoomA.All(info => info is ClueInfo));
        }

        [Test]
        public void 습득된_단서는_방별_조회_결과에서_빠진다()
        {
            var roomId = new MemoryRoomId("room-a");
            var tracker = new MemoryRoomClueTracker(new[]
            {
                MakeClueDefinition("clue-1", roomId),
                MakeClueDefinition("clue-2", roomId),
            });

            tracker.MarkCollected(new ClueId("clue-1"));
            var infos = tracker.GetAvailableClueInfos(roomId);

            Assert.AreEqual(1, infos.Count);
            Assert.AreEqual(new ClueId("clue-2"), infos[0].Id);
        }
    }
}
