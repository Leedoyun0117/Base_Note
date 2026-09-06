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
    // 버리기는 아무것도 돌려주지 않고 손에서 지운다 — 실패 사유는 "지금 손에
    // 없다" 하나뿐이고, 한 번 버리면 이 런 안에서는 되돌릴 방법이 없다.
    public class ClueDiscardProcessorTests
    {
        private static readonly MemoryRoomId TheRoom = new MemoryRoomId("room-1");
        private static readonly ClueId TheClue = new ClueId("clue-1");

        private sealed class Fixture
        {
            public readonly EventBus Bus = new EventBus(new NoOpEventExceptionHandler());
            public readonly ClueStateStore State;
            public readonly ClueDiscardProcessor Processor;
            public readonly List<ClueDiscardedEvent> Discarded = new List<ClueDiscardedEvent>();

            public Fixture()
            {
                var def = new ClueDefinition(
                    TheClue, ClueKind.Poster, "단서", new CluePositionRatio(0.5f), MemoryColor.Red);
                var room = new RoomDefinition(
                    TheRoom, new[] { def }, null, Array.Empty<DialogueLineDefinition>());

                State = new ClueStateStore(new[] { room }, Bus);
                State.Seed(0);
                Processor = new ClueDiscardProcessor(State, Bus);

                Bus.Subscribe<ClueDiscardedEvent>(Discarded.Add);
            }
        }

        [Test]
        public void 손에_든_단서는_버릴_수_있고_사건이_한_번_난다()
        {
            var fx = new Fixture();
            fx.State.SetState(TheClue, ClueState.Collected);

            var result = fx.Processor.Discard(TheClue);

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(ClueState.Discarded, fx.State.GetState(TheClue));
            Assert.AreEqual(1, fx.Discarded.Count);
            Assert.AreEqual(TheClue, fx.Discarded[0].ClueId);
        }

        [Test]
        public void 아직_손에_없는_단서는_버릴_수_없다()
        {
            var fx = new Fixture();

            var result = fx.Processor.Discard(TheClue);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ClueDiscardFailureReason.NotCollected, result.FailureReason);
            CollectionAssert.IsEmpty(fx.Discarded);
        }

        [Test]
        public void 이미_버린_단서를_다시_버리려_하면_회수되지_않고_거부된다()
        {
            var fx = new Fixture();
            fx.State.SetState(TheClue, ClueState.Collected);
            fx.Processor.Discard(TheClue);

            var again = fx.Processor.Discard(TheClue);

            Assert.IsFalse(again.Succeeded);
            Assert.AreEqual(ClueDiscardFailureReason.NotCollected, again.FailureReason);
            Assert.AreEqual(ClueState.Discarded, fx.State.GetState(TheClue), "버린 단서는 이 런 안에서 되돌아오지 않는다.");
            Assert.AreEqual(1, fx.Discarded.Count, "회수 시도는 새 버리기 사건을 내지 않는다.");
        }

        [Test]
        public void 이미_추출한_단서는_버릴_수_없다()
        {
            var fx = new Fixture();
            fx.State.SetState(TheClue, ClueState.Collected);
            fx.State.SetState(TheClue, ClueState.Extracted);

            var result = fx.Processor.Discard(TheClue);

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ClueDiscardFailureReason.NotCollected, result.FailureReason);
        }
    }
}
