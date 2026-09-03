using System.Linq;
using GameName.Core.Clues;
using GameName.Core.Memories;
using GameName.Core.MemoryRooms;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    // 카탈로그로서의 추적기: 어떤 단서가 어느 방에 놓여 있고 그 정의가 무엇인가.
    // "집었는가"는 여기서 답하지 않는다 — 그건 ClueState의 몫이다.
    public class MemoryRoomClueTrackerTests
    {
        private static ClueDefinition MakeClueDefinition(string id, ClueKind kind = ClueKind.FloorObject) =>
            new ClueDefinition(new ClueId(id), kind, id, new CluePositionRatio(0.5f), MemoryColor.Red);

        private static CluePlacement Place(MemoryRoomId roomId, string id, ClueKind kind = ClueKind.FloorObject) =>
            new CluePlacement(roomId, MakeClueDefinition(id, kind));

        [Test]
        public void 방별_조회는_그_방의_단서만_ClueInfo로_반환한다()
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
        public void 방별_조회는_집힘_여부로_거르지_않는다()
        {
            // 집힌 단서를 방에서 지우는 것은 ClueState를 보는 화면의 몫이다 —
            // 카탈로그는 배치된 단서를 항상 그대로 내어준다.
            var roomId = new MemoryRoomId("room-a");
            var tracker = new MemoryRoomClueTracker(new[]
            {
                Place(roomId, "clue-1"),
                Place(roomId, "clue-2"),
            });

            Assert.AreEqual(2, tracker.GetCluesInRoom(roomId).Count);
        }

        [Test]
        public void 정의는_습득_여부와_무관하게_조회된다()
        {
            var tracker = new MemoryRoomClueTracker(new[] { Place(new MemoryRoomId("room-a"), "clue-1") });

            Assert.IsTrue(tracker.TryGetDefinition(new ClueId("clue-1"), out var def));
            Assert.AreEqual(MemoryColor.Red, def.HiddenColor);
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
        public void 방별_조회_순서는_등록_순서를_유지한다()
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
