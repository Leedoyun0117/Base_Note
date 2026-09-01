using System.Linq;
using GameName.Core.Clues;
using GameName.Core.MemoryRooms;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    public class MemoryRoomClueTrackerTests
    {
        private static ClueDefinition MakeClueDefinition(string id, ClueKind kind = ClueKind.FloorObject) =>
            new ClueDefinition(
                new ClueId(id),
                kind,
                new CluePositionRatio(0.5f));

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
                Place(roomId, "clue-1"),
                Place(roomId, "clue-2"),
            });

            tracker.MarkCollected(new ClueId("clue-1"));
            var infos = tracker.GetAvailableClueInfos(roomId);

            Assert.AreEqual(1, infos.Count);
            Assert.AreEqual(new ClueId("clue-2"), infos[0].Id);
        }

        [Test]
        public void Load_후에는_이전_습득_기록이_사라지고_새_정의만_조회된다()
        {
            var roomId = new MemoryRoomId("room-a");
            var tracker = new MemoryRoomClueTracker(new[] { Place(roomId, "clue-1") });
            tracker.MarkCollected(new ClueId("clue-1"));

            tracker.Load(new[] { Place(roomId, "clue-1") });

            var infos = tracker.GetAvailableClueInfos(roomId);
            Assert.AreEqual(1, infos.Count);
        }

        // 섹션 0 검증: 소속 방이 정의가 아니라 추적기의 상태라는 것.
        [Test]
        public void 다른_방에_놓으면_그_방_소속으로_재배정된다()
        {
            var origin = new MemoryRoomId("room-a");
            var destination = new MemoryRoomId("room-b");
            var tracker = new MemoryRoomClueTracker(new[] { Place(origin, "clue-1") });

            tracker.MarkCollected(new ClueId("clue-1"));
            tracker.PlaceInRoom(new ClueId("clue-1"), destination);

            Assert.AreEqual(0, tracker.GetAvailableClueInfos(origin).Count);
            Assert.AreEqual(1, tracker.GetAvailableClueInfos(destination).Count);

            Assert.IsTrue(tracker.TryGetPlacedClue(new ClueId("clue-1"), out _, out var roomId));
            Assert.AreEqual(destination, roomId);
        }

        [Test]
        public void 습득한_단서는_어느_방에도_놓여_있지_않다()
        {
            var roomId = new MemoryRoomId("room-a");
            var tracker = new MemoryRoomClueTracker(new[] { Place(roomId, "clue-1") });

            tracker.MarkCollected(new ClueId("clue-1"));

            Assert.IsFalse(tracker.TryGetPlacedClue(new ClueId("clue-1"), out _, out _));

            // 정의 자체는 그대로 남아 있어야 한다 — 분석기가 인벤토리에 든
            // 단서를 계속 조회할 수 있어야 하기 때문이다.
            Assert.IsTrue(tracker.TryGetDefinition(new ClueId("clue-1"), out _));
        }

        // 단서를 집었다 버렸다 해도 방에 남은 단서들의 순서가 흔들리면 안 된다
        // — 화면이 그 순서로 배치 위치를 정하므로 제자리가 뒤바뀌어 보인다.
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

            tracker.MarkCollected(new ClueId("clue-1"));
            tracker.PlaceInRoom(new ClueId("clue-1"), roomId);

            var ids = tracker.GetAvailableClueInfos(roomId).Select(info => info.Id.Value).ToArray();

            CollectionAssert.AreEqual(new[] { "clue-1", "clue-2", "clue-3" }, ids);
        }
    }
}
