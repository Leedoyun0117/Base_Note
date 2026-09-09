using System.Collections.Generic;
using GameName.Core.Clues;
using GameName.Core.Events;
using GameName.Core.MemoryRooms;
using GameName.Core.Progression;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    // 조사 국면에서 단서 수집을 세고, 방당 한도에 닿으면 대화 국면으로 넘긴다.
    public class RoomInvestigationCounterTests
    {
        private static readonly MemoryRoomId Room = new MemoryRoomId("room-1");

        private sealed class Fixture
        {
            public readonly EventBus Bus = new EventBus(new NoOpEventExceptionHandler());
            public readonly RoomPhaseCoordinator Phase;
            public readonly List<DialoguePhaseStartedEvent> DialogueStarts = new List<DialoguePhaseStartedEvent>();

            // ReSharper disable once NotAccessedField.Local — 구독을 살려 두기 위한 보관.
            private readonly RoomInvestigationCounter _counter;

            public Fixture(int perRoom)
            {
                Phase = new RoomPhaseCoordinator(Bus);
                _counter = new RoomInvestigationCounter(Phase, perRoom, Bus);
                Bus.Subscribe<DialoguePhaseStartedEvent>(DialogueStarts.Add);
            }

            public void StartRoom(string id, int index) =>
                Bus.Publish(new RoomStartedEvent(new MemoryRoomId(id), index));

            public void Collect(string clueId) =>
                Bus.Publish(new ClueCollectedEvent(new ClueId(clueId), Room));
        }

        [Test]
        public void 한도만큼_수집하면_대화_국면으로_넘어간다()
        {
            var fx = new Fixture(perRoom: 3);
            fx.StartRoom("room-1", 0);

            fx.Collect("a");
            fx.Collect("b");
            Assert.AreEqual(RoomPhase.Investigation, fx.Phase.Current, "아직 한도 전이다.");

            fx.Collect("c");

            Assert.AreEqual(RoomPhase.Dialogue, fx.Phase.Current);
            Assert.AreEqual(1, fx.DialogueStarts.Count);
        }

        [Test]
        public void 대화_국면에서_들어온_수집은_세지_않는다()
        {
            var fx = new Fixture(perRoom: 1);
            fx.StartRoom("room-1", 0);
            fx.Collect("a"); // 한도 도달 → 대화 국면

            fx.Collect("b"); // 이미 대화 국면 — 무시

            Assert.AreEqual(1, fx.DialogueStarts.Count, "대화 국면 전환은 한 번뿐이다.");
        }

        [Test]
        public void 한도가_0이면_방이_시작되자마자_대화_국면이다()
        {
            var fx = new Fixture(perRoom: 0);

            fx.StartRoom("room-1", 0);

            Assert.AreEqual(RoomPhase.Dialogue, fx.Phase.Current);
            Assert.AreEqual(1, fx.DialogueStarts.Count);
        }

        [Test]
        public void 다음_방이_시작되면_카운트가_다시_0부터다()
        {
            var fx = new Fixture(perRoom: 2);
            fx.StartRoom("room-1", 0);
            fx.Collect("a");
            fx.Collect("b"); // room-1 대화 국면
            fx.DialogueStarts.Clear();

            fx.StartRoom("room-2", 1);
            fx.Collect("c");
            Assert.AreEqual(RoomPhase.Investigation, fx.Phase.Current, "새 방에서 한 번만 집었으니 아직 조사 국면.");

            fx.Collect("d");
            Assert.AreEqual(RoomPhase.Dialogue, fx.Phase.Current);
            Assert.AreEqual(new MemoryRoomId("room-2"), fx.DialogueStarts[0].RoomId);
        }
    }
}
