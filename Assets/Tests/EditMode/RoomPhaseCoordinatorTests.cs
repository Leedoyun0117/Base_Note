using System.Collections.Generic;
using GameName.Core.Events;
using GameName.Core.MemoryRooms;
using GameName.Core.Progression;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    // 방 국면을 조사 → 대화로 옮기고, 대화 국면에서 DialoguePhaseStartedEvent를
    // 낸다. 지금은 방이 시작되는 즉시 대화 국면으로 자동 전환한다.
    public class RoomPhaseCoordinatorTests
    {
        private sealed class Fixture
        {
            public readonly EventBus Bus = new EventBus(new NoOpEventExceptionHandler());
            public readonly RoomPhaseCoordinator Coordinator;
            public readonly List<RoomPhaseChangedEvent> PhaseChanges = new List<RoomPhaseChangedEvent>();
            public readonly List<DialoguePhaseStartedEvent> DialogueStarts = new List<DialoguePhaseStartedEvent>();

            public Fixture()
            {
                Coordinator = new RoomPhaseCoordinator(Bus);
                Bus.Subscribe<RoomPhaseChangedEvent>(PhaseChanges.Add);
                Bus.Subscribe<DialoguePhaseStartedEvent>(DialogueStarts.Add);
            }
        }

        [Test]
        public void 조립_직후에는_조사_국면이다()
        {
            Assert.AreEqual(RoomPhase.Investigation, new Fixture().Coordinator.Current);
        }

        [Test]
        public void 방이_시작되면_조사_국면_뒤_대화_국면으로_자동_전환하며_대화_시작_사건을_낸다()
        {
            var fx = new Fixture();

            fx.Bus.Publish(new RoomStartedEvent(new MemoryRoomId("room-2"), 1));

            Assert.AreEqual(RoomPhase.Dialogue, fx.Coordinator.Current);
            CollectionAssert.AreEqual(
                new[] { RoomPhase.Investigation, RoomPhase.Dialogue },
                fx.PhaseChanges.ConvertAll(e => e.Phase));

            Assert.AreEqual(1, fx.DialogueStarts.Count);
            Assert.AreEqual(new MemoryRoomId("room-2"), fx.DialogueStarts[0].RoomId);
            Assert.AreEqual(1, fx.DialogueStarts[0].RoomIndex);
        }

        [Test]
        public void BeginDialogue를_다시_불러도_사건이_겹치지_않는다()
        {
            var fx = new Fixture();
            fx.Bus.Publish(new RoomStartedEvent(new MemoryRoomId("room-1"), 0));

            fx.Coordinator.BeginDialogue();

            Assert.AreEqual(1, fx.DialogueStarts.Count, "이미 대화 국면이면 아무 일도 없다.");
        }

        [Test]
        public void 다음_방이_시작되면_다시_조사_국면부터_시작한다()
        {
            var fx = new Fixture();
            fx.Bus.Publish(new RoomStartedEvent(new MemoryRoomId("room-1"), 0));
            fx.PhaseChanges.Clear();
            fx.DialogueStarts.Clear();

            fx.Bus.Publish(new RoomStartedEvent(new MemoryRoomId("room-2"), 1));

            CollectionAssert.AreEqual(
                new[] { RoomPhase.Investigation, RoomPhase.Dialogue },
                fx.PhaseChanges.ConvertAll(e => e.Phase));
            Assert.AreEqual(new MemoryRoomId("room-2"), fx.DialogueStarts[0].RoomId);
        }
    }
}
