using System.Linq;
using GameName.Core.Clues;
using GameName.Core.MemoryRooms;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    // 카탈로그로서의 추적기: 어떤 단서가 어느 라운드에 놓여 있고 그 정의가
    // 무엇인가. "읽었는가"는 여기서 답하지 않는다 — 그건 ClueState의 몫이다.
    public class MemoryRoomClueTrackerTests
    {
        private static ClueDefinition MakeClueDefinition(string id, ClueKind kind = ClueKind.FloorObject) =>
            new ClueDefinition(new ClueId(id), kind, id, new CluePositionRatio(0.5f), story: $"{id} 서사");

        private static CluePlacement Place(MemoryRoomId roomId, string id, ClueKind kind = ClueKind.FloorObject) =>
            new CluePlacement(roomId, MakeClueDefinition(id, kind));

        [Test]
        public void 라운드별_조회는_그_라운드의_단서만_ClueInfo로_반환한다()
        {
            var roomA = new MemoryRoomId("room-a");
            var roomB = new MemoryRoomId("room-b");
            var tracker = new MemoryRoomClueTracker(new[]
            {
                Place(roomA, "clue-1"),
                Place(roomA, "clue-2"),
                Place(roomB, "clue-3"),
            });

            var infosInRoomA = tracker.GetCluesInRoom(roomA);

            Assert.AreEqual(2, infosInRoomA.Count);
            Assert.IsTrue(infosInRoomA.All(info => info is ClueInfo));
        }

        [Test]
        public void 정의는_읽힘_여부와_무관하게_조회되고_서사를_담는다()
        {
            var tracker = new MemoryRoomClueTracker(new[] { Place(new MemoryRoomId("room-a"), "clue-1") });

            Assert.IsTrue(tracker.TryGetDefinition(new ClueId("clue-1"), out var def));
            Assert.AreEqual("clue-1 서사", def.Story);
            Assert.IsFalse(tracker.TryGetDefinition(new ClueId("clue-없음"), out _));
        }

        [Test]
        public void Load하면_이전_구성이_사라지고_새_정의만_조회된다()
        {
            var roomId = new MemoryRoomId("room-a");
            var tracker = new MemoryRoomClueTracker(new[] { Place(roomId, "clue-1") });

            tracker.Load(new[] { Place(roomId, "clue-2"), Place(roomId, "clue-3") });

            var ids = tracker.GetCluesInRoom(roomId).Select(i => i.Id.Value).ToArray();
            CollectionAssert.AreEqual(new[] { "clue-2", "clue-3" }, ids);
            Assert.IsFalse(tracker.TryGetDefinition(new ClueId("clue-1"), out _));
        }

        [Test]
        public void 라운드별_조회_순서는_등록_순서를_유지한다()
        {
            var roomId = new MemoryRoomId("room-a");
            var tracker = new MemoryRoomClueTracker(new[]
            {
                Place(roomId, "clue-1"),
                Place(roomId, "clue-2"),
                Place(roomId, "clue-3"),
            });

            var ids = tracker.GetCluesInRoom(roomId).Select(info => info.Id.Value).ToArray();

            CollectionAssert.AreEqual(new[] { "clue-1", "clue-2", "clue-3" }, ids);
        }

        [Test]
        public void 중복된_단서_식별자는_거부된다()
        {
            Assert.Throws<System.ArgumentException>(() => new MemoryRoomClueTracker(new[]
            {
                Place(new MemoryRoomId("room-a"), "clue-1"),
                Place(new MemoryRoomId("room-b"), "clue-1"),
            }));
        }
    }
}
