using System;
using System.Collections.Generic;
using GameName.Core.Authoring;
using GameName.Core.Clues;
using GameName.Core.Dialogue;
using GameName.Core.Events;
using GameName.Core.MemoryRooms;
using GameName.Core.Progression;
using GameName.Core.Trust;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    // 대화 완주면 다음 방으로 잇고, 신뢰 0 실패면 그 자리에서 런이 끝난다.
    // 마지막 방을 클리어해도 런이 끝난다.
    public class RunProgressorTests
    {
        private static RoomDefinition Room(string id) =>
            new RoomDefinition(
                new MemoryRoomId(id), Array.Empty<ClueDefinition>(), null,
                Array.Empty<DialogueLineDefinition>());

        private sealed class Fixture
        {
            public readonly EventBus Bus = new EventBus(new NoOpEventExceptionHandler());
            public readonly RunProgressor Progressor;
            public readonly List<RoomStartedEvent> Started = new List<RoomStartedEvent>();
            public readonly List<RunCompletedEvent> Completed = new List<RunCompletedEvent>();

            public Fixture(int roomCount)
            {
                var rooms = new List<RoomDefinition>();
                for (var i = 1; i <= roomCount; i++)
                    rooms.Add(Room($"room-{i}"));

                Progressor = new RunProgressor(rooms, Bus);
                Bus.Subscribe<RoomStartedEvent>(Started.Add);
                Bus.Subscribe<RunCompletedEvent>(Completed.Add);
            }
        }

        [Test]
        public void 시작하면_첫_방으로_들어간다()
        {
            var fx = new Fixture(3);

            fx.Progressor.Start();

            Assert.AreEqual(1, fx.Started.Count);
            Assert.AreEqual(new MemoryRoomId("room-1"), fx.Started[0].RoomId);
            Assert.AreEqual(0, fx.Started[0].RoomIndex);
        }

        [Test]
        public void Advance하면_다음_방으로_넘어간다()
        {
            var fx = new Fixture(3);
            fx.Progressor.Start();

            // "다음으로" 버튼·이동 레버는 결국 이 메서드를 부른다(MemoryMoveProcessor 경유).
            fx.Progressor.Advance();

            Assert.AreEqual(2, fx.Started.Count);
            Assert.AreEqual(new MemoryRoomId("room-2"), fx.Started[1].RoomId);
            Assert.AreEqual(1, fx.Started[1].RoomIndex);
        }

        [Test]
        public void RoomClearedEvent_단독으로는_넘어가지_않는다()
        {
            // RunProgressor는 이제 RoomClearedEvent를 직접 받지 않는다 —
            // 히로민을 치르는 이동은 RoomClearanceMoveListener의 몫이다.
            var fx = new Fixture(3);
            fx.Progressor.Start();

            fx.Bus.Publish(new RoomClearedEvent(new MemoryRoomId("room-1")));

            Assert.AreEqual(1, fx.Started.Count);
        }

        [Test]
        public void 방을_실패하면_다음_방으로_넘어가지_않고_런이_끝난다()
        {
            var fx = new Fixture(3);
            fx.Progressor.Start();

            fx.Bus.Publish(new RoomFailedEvent(new MemoryRoomId("room-1")));

            Assert.AreEqual(1, fx.Started.Count, "실패한 방을 지나 다음 방으로 데려가면 안 된다.");
            Assert.AreEqual(1, fx.Completed.Count);
        }

        [Test]
        public void 마지막_방에서_Advance하면_런이_끝나고_더_이상_방이_시작되지_않는다()
        {
            var fx = new Fixture(3);
            fx.Progressor.Start();

            fx.Progressor.Advance();
            fx.Progressor.Advance();
            fx.Progressor.Advance();

            Assert.AreEqual(3, fx.Started.Count);
            Assert.AreEqual(1, fx.Completed.Count);
        }

        [Test]
        public void 런은_한_번만_시작할_수_있다()
        {
            var fx = new Fixture(2);
            fx.Progressor.Start();

            Assert.Throws<InvalidOperationException>(() => fx.Progressor.Start());
        }

        // RoomCompletionArbiter까지 함께 엮어, 신뢰 0이 런을 끝내는지 본다.
        // (대화 완주 → 다음 방은 RoomClearanceMoveListener → MemoryMoveProcessor
        // 경로라 여기서 다루지 않는다 — RoomClearanceMoveListenerTests가 커버한다.)
        private sealed class DrivenFixture
        {
            public readonly EventBus Bus = new EventBus(new NoOpEventExceptionHandler());
            public readonly TrustGauge Trust;
            public readonly List<RoomStartedEvent> Started = new List<RoomStartedEvent>();
            public readonly List<RunCompletedEvent> Completed = new List<RunCompletedEvent>();

            public DrivenFixture()
            {
                var rooms = new List<RoomDefinition> { Room("room-1"), Room("room-2"), Room("room-3") };
                Trust = new TrustGauge(3, Bus);
                _ = new RoomCompletionArbiter(Trust, Bus);
                var progressor = new RunProgressor(rooms, Bus);
                Bus.Subscribe<RoomStartedEvent>(Started.Add);
                Bus.Subscribe<RunCompletedEvent>(Completed.Add);
                progressor.Start();
            }
        }

        [Test]
        public void 신뢰_0에_닿으면_다음_방으로_가지_않고_런이_끝난다()
        {
            var fx = new DrivenFixture();

            fx.Trust.Decrease(3); // → 0 → RoomFailedEvent → EndRun

            Assert.AreEqual(1, fx.Started.Count, "실패한 방에서 다음 방으로 넘어가면 안 된다.");
            Assert.AreEqual(1, fx.Completed.Count);
        }
    }
}
