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
    // 단서 단계는 한 방향으로만 흐르고, 방이 바뀌면 처음부터 다시 시작한다.
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
        public void 방이_시작되면_그_방_단서로_다시_시드된다()
        {
            var (store, bus) = Make(Room("room-1", "a"), Room("room-2", "b"));
            store.SetState(new ClueId("a"), ClueState.Collected);

            bus.Publish(new RoomStartedEvent(new MemoryRoomId("room-2"), 1));

            Assert.AreEqual(ClueState.Available, store.GetState(new ClueId("b")));
            Assert.IsFalse(store.TryGetState(new ClueId("a"), out _));
        }
    }
}
