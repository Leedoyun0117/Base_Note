using System;
using System.Collections.Generic;
using GameName.Core.Authoring;
using GameName.Core.Clues;
using GameName.Core.Dialogue;
using GameName.Core.Events;
using GameName.Core.Memories;
using GameName.Core.MemoryRooms;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    // 단서 단계는 한 방향으로만 흐르고, 런 전체에 걸쳐 누적된다 — 방이 바뀌어도
    // 이미 알던 단서의 단계는 지워지지 않는다.
    public class ClueStateStoreTests
    {
        private static ClueDefinition Clue(string id) =>
            new ClueDefinition(new ClueId(id), ClueKind.Poster, id, new CluePositionRatio(0.5f), MemoryColor.Red);

        private static RoomDefinition Room(string id, params string[] clueIds)
        {
            var clues = new List<ClueDefinition>();
            foreach (var c in clueIds) clues.Add(Clue(c));
            return new RoomDefinition(
                new MemoryRoomId(id), clues, null, Array.Empty<DialogueLineDefinition>());
        }

        private static (ClueStateStore store, EventBus bus) Make(params RoomDefinition[] rooms)
        {
            var bus = new EventBus(new NoOpEventExceptionHandler());
            var store = new ClueStateStore(rooms, bus);
            store.Seed(0);
            return (store, bus);
        }

        [Test]
        public void 시드하면_그_방_단서가_전부_Available이다()
        {
            var (store, _) = Make(Room("room-1", "clue-1", "clue-2"));

            Assert.AreEqual(ClueState.Available, store.GetState(new ClueId("clue-1")));
            Assert.AreEqual(ClueState.Available, store.GetState(new ClueId("clue-2")));
        }

        [Test]
        public void 이_방에_없는_단서를_물으면_예외지만_TryGetState는_false다()
        {
            var (store, _) = Make(Room("room-1", "clue-1"));

            Assert.Throws<ArgumentException>(() => store.GetState(new ClueId("clue-없음")));
            Assert.IsFalse(store.TryGetState(new ClueId("clue-없음"), out _));
        }

        [Test]
        public void 합법_전이는_통과한다()
        {
            var (store, _) = Make(Room("room-1", "a", "b"));

            store.SetState(new ClueId("a"), ClueState.Collected);
            store.SetState(new ClueId("a"), ClueState.Extracted);

            store.SetState(new ClueId("b"), ClueState.Collected);
            store.SetState(new ClueId("b"), ClueState.UsedInDialogue);

            Assert.AreEqual(ClueState.Extracted, store.GetState(new ClueId("a")));
            Assert.AreEqual(ClueState.UsedInDialogue, store.GetState(new ClueId("b")));
        }

        [Test]
        public void 되돌아가거나_건너뛰는_전이는_예외다()
        {
            var (store, _) = Make(Room("room-1", "a"));
            var a = new ClueId("a");

            Assert.Throws<InvalidOperationException>(() => store.SetState(a, ClueState.Extracted)); // Available→Extracted

            store.SetState(a, ClueState.Collected);
            store.SetState(a, ClueState.UsedInDialogue);
            Assert.Throws<InvalidOperationException>(() => store.SetState(a, ClueState.Extracted)); // UsedInDialogue→Extracted
            Assert.Throws<InvalidOperationException>(() => store.SetState(a, ClueState.Available)); // Extracted 이후는 종착
        }

        [Test]
        public void 손에_든_단서는_버릴_수_있고_버린_뒤는_종착이다()
        {
            var (store, _) = Make(Room("room-1", "a"));
            var a = new ClueId("a");

            store.SetState(a, ClueState.Collected);
            store.SetState(a, ClueState.Discarded);

            Assert.AreEqual(ClueState.Discarded, store.GetState(a));
            Assert.Throws<InvalidOperationException>(() => store.SetState(a, ClueState.Collected));
            Assert.Throws<InvalidOperationException>(() => store.SetState(a, ClueState.Extracted));
        }

        [Test]
        public void 아직_손에_없는_단서는_버릴_수_없다()
        {
            var (store, _) = Make(Room("room-1", "a"));

            Assert.Throws<InvalidOperationException>(
                () => store.SetState(new ClueId("a"), ClueState.Discarded)); // Available→Discarded
        }

        [Test]
        public void 방이_시작되면_새_방_단서가_Available로_더해지고_이미_아는_단서_단계는_유지된다()
        {
            var (store, bus) = Make(Room("room-1", "a"), Room("room-2", "b"));
            store.SetState(new ClueId("a"), ClueState.Collected);

            bus.Publish(new RoomStartedEvent(new MemoryRoomId("room-2"), 1));

            Assert.AreEqual(ClueState.Available, store.GetState(new ClueId("b")));
            Assert.AreEqual(
                ClueState.Collected, store.GetState(new ClueId("a")),
                "런 전체에 걸쳐 누적되므로 앞 방에서 집은 단서는 방이 바뀌어도 그대로 손에 남는다.");
        }

        [Test]
        public void 방을_거쳐도_다른_방_단서로_대화에_쓸_수_있는_상태로_이어진다()
        {
            var (store, bus) = Make(Room("room-1", "a"), Room("room-2", "b"), Room("room-3", "c"));
            store.SetState(new ClueId("a"), ClueState.Collected);

            bus.Publish(new RoomStartedEvent(new MemoryRoomId("room-2"), 1));
            bus.Publish(new RoomStartedEvent(new MemoryRoomId("room-3"), 2));

            // room-1의 단서가 room-3에 이르러서도 여전히 손에 있다 — 다른 방의
            // ClueSelection 줄에 답으로 낼 수 있는 상태가 계속 유지된다는 뜻이다.
            Assert.AreEqual(ClueState.Collected, store.GetState(new ClueId("a")));
            Assert.AreEqual(ClueState.Available, store.GetState(new ClueId("c")));
        }
    }
}
