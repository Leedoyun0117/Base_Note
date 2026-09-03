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
    // 종료 사유와 무관하게 방을 한 줄로 이어 진행하고, 마지막 뒤엔 런이 끝난다.
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
        public void 방을_클리어하면_다음_방으로_넘어간다()
        {
            var fx = new Fixture(3);
            fx.Progressor.Start();

            fx.Bus.Publish(new RoomClearedEvent(new MemoryRoomId("room-1")));

            Assert.AreEqual(2, fx.Started.Count);
            Assert.AreEqual(new MemoryRoomId("room-2"), fx.Started[1].RoomId);
            Assert.AreEqual(1, fx.Started[1].RoomIndex);
        }

        [Test]
        public void 방을_실패해도_똑같이_다음_방으로_넘어간다()
        {
            var fx = new Fixture(3);
            fx.Progressor.Start();

            fx.Bus.Publish(new RoomFailedEvent(new MemoryRoomId("room-1")));

            Assert.AreEqual(2, fx.Started.Count);
            Assert.AreEqual(new MemoryRoomId("room-2"), fx.Started[1].RoomId);
        }

        [Test]
        public void 마지막_방이_닫히면_런이_끝나고_더_이상_방이_시작되지_않는다()
        {
            var fx = new Fixture(3);
            fx.Progressor.Start();

            fx.Bus.Publish(new RoomFailedEvent(new MemoryRoomId("room-1")));
            fx.Bus.Publish(new RoomClearedEvent(new MemoryRoomId("room-2")));
            fx.Bus.Publish(new RoomFailedEvent(new MemoryRoomId("room-3")));

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

        // RoomCompletionArbiter까지 함께 엮어, 방 종료 사건(대화 종료 / 신뢰 0)이
        // 그대로 다음 방 진입으로 이어지는지 본다 — 옛 RoomEntryAnnouncer(걸어서
        // 진입)를 대체하는 강제 전환 경로다.
        private sealed class DrivenFixture
        {
            public readonly EventBus Bus = new EventBus(new NoOpEventExceptionHandler());
            public readonly TrustGauge Trust;
            public readonly List<RoomStartedEvent> Started = new List<RoomStartedEvent>();

            public DrivenFixture()
            {
                var rooms = new List<RoomDefinition> { Room("room-1"), Room("room-2"), Room("room-3") };
                Trust = new TrustGauge(3, Bus);
                _ = new RoomCompletionArbiter(Trust, Bus);
                var progressor = new RunProgressor(rooms, Bus);
                Bus.Subscribe<RoomStartedEvent>(Started.Add);
                progressor.Start();
            }
        }

        [Test]
        public void 대화_종료_사건이_그대로_다음_방_진입으로_이어진다()
        {
            var fx = new DrivenFixture();

            fx.Bus.Publish(new DialogueEndedEvent(new MemoryRoomId("room-1")));

            Assert.AreEqual(2, fx.Started.Count);
            Assert.AreEqual(new MemoryRoomId("room-2"), fx.Started[1].RoomId);
        }

        [Test]
        public void 신뢰_0_도달이_그대로_다음_방_진입으로_이어진다()
        {
            var fx = new DrivenFixture();

            fx.Trust.Decrease(3); // → 0 → RoomFailedEvent → Advance

            Assert.AreEqual(2, fx.Started.Count);
            Assert.AreEqual(new MemoryRoomId("room-2"), fx.Started[1].RoomId);

            // 다음 방에서 신뢰가 3으로 리셋됐으므로 또 3번 깎아야 넘어간다.
            fx.Trust.Decrease(3);
            Assert.AreEqual(3, fx.Started.Count);
            Assert.AreEqual(new MemoryRoomId("room-3"), fx.Started[2].RoomId);
        }
    }
}
