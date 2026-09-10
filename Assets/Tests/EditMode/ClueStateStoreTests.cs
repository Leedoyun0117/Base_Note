using System;
using System.Collections.Generic;
using GameName.Core.Authoring;
using GameName.Core.Clues;
using GameName.Core.Events;
using GameName.Core.MemoryRooms;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    // 단서 단계는 Available → Used 한 방향으로만 흐르고, 라운드가 시작되면 그
    // 라운드 단서가 새로 Available로 시드된다.
    public class ClueStateStoreTests
    {
        private static ClueDefinition Clue(string id) =>
            new ClueDefinition(new ClueId(id), ClueKind.Poster, id, new CluePositionRatio(0.5f));

        private static RoomDefinition Round(string id, params string[] clueIds)
        {
            var clues = new List<ClueDefinition>();
            foreach (var c in clueIds) clues.Add(Clue(c));
            return new RoomDefinition(new MemoryRoomId(id), clues, turnsToSurvive: 3);
        }

        private static (ClueStateStore store, EventBus bus) Make(params RoomDefinition[] rounds)
        {
            var bus = new EventBus(new NoOpEventExceptionHandler());
            var store = new ClueStateStore(rounds, bus);
            store.Seed(0);
            return (store, bus);
        }

        [Test]
        public void 시드하면_그_라운드_단서가_전부_Available이다()
        {
            var (store, _) = Make(Round("round-1", "clue-1", "clue-2"));

            Assert.AreEqual(ClueState.Available, store.GetState(new ClueId("clue-1")));
            Assert.AreEqual(ClueState.Available, store.GetState(new ClueId("clue-2")));
        }

        [Test]
        public void 이_라운드에_없는_단서를_물으면_예외지만_TryGetState는_false다()
        {
            var (store, _) = Make(Round("round-1", "clue-1"));

            Assert.Throws<ArgumentException>(() => store.GetState(new ClueId("clue-없음")));
            Assert.IsFalse(store.TryGetState(new ClueId("clue-없음"), out _));
        }

        [Test]
        public void Available에서_Used로만_갈_수_있다()
        {
            var (store, _) = Make(Round("round-1", "a"));
            var a = new ClueId("a");

            store.SetState(a, ClueState.Used);
            Assert.AreEqual(ClueState.Used, store.GetState(a));
        }

        [Test]
        public void 이미_읽은_단서를_다시_읽거나_되돌리는_전이는_예외다()
        {
            var (store, _) = Make(Round("round-1", "a"));
            var a = new ClueId("a");

            store.SetState(a, ClueState.Used);

            Assert.Throws<InvalidOperationException>(() => store.SetState(a, ClueState.Used));
            Assert.Throws<InvalidOperationException>(() => store.SetState(a, ClueState.Available));
        }

        [Test]
        public void 라운드가_시작되면_새_라운드_단서가_Available로_더해지고_이미_아는_단서_단계는_유지된다()
        {
            var (store, bus) = Make(Round("round-1", "a"), Round("round-2", "b"));
            store.SetState(new ClueId("a"), ClueState.Used);

            bus.Publish(new RoomStartedEvent(new MemoryRoomId("round-2"), 1));

            Assert.AreEqual(ClueState.Available, store.GetState(new ClueId("b")));
            Assert.AreEqual(ClueState.Used, store.GetState(new ClueId("a")));
        }
    }
}
