using System;
using System.Collections.Generic;
using GameName.Core.Authoring;
using GameName.Core.Dialogue;
using GameName.Core.Events;
using GameName.Core.Hiromi;
using GameName.Core.MemoryRooms;
using GameName.Core.Progression;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    // 다음 기억으로 이동은 늘 일어난다 — 히로민이 문턱 이상이면 그만큼만 쓰고
    // 곧장 이동하고, 모자라면 가진 히로민을 전부 쓰고 기회를 하나 잃는 대가로
    // 이동한다. 기회가 그걸로 바닥나면 이동 대신 런이 끝난다.
    public class MemoryMoveProcessorTests
    {
        private const int MoveThreshold = 15;

        private static RoomDefinition Room(string id) =>
            new RoomDefinition(
                new MemoryRoomId(id), Array.Empty<Clues.ClueDefinition>(), null,
                Array.Empty<DialogueLineDefinition>());

        private sealed class Fixture
        {
            public readonly EventBus Bus = new EventBus(new NoOpEventExceptionHandler());
            public readonly HiromiWallet Hiromi;
            public readonly ChanceTracker Chance;
            public readonly RunProgressor Run;
            public readonly MemoryMoveProcessor Move;
            public readonly List<RoomStartedEvent> RoomStarts = new List<RoomStartedEvent>();
            public readonly List<RunCompletedEvent> RunCompletions = new List<RunCompletedEvent>();

            public Fixture(int startingHiromi, int startingChance, params RoomDefinition[] rooms)
            {
                Hiromi = new HiromiWallet(startingHiromi, Bus);
                Chance = new ChanceTracker(startingChance, Bus);
                Run = new RunProgressor(rooms, Bus);
                Move = new MemoryMoveProcessor(MoveThreshold, Hiromi, Chance, Run);

                _ = new ChanceExhaustionListener(Bus);

                Bus.Subscribe<RoomStartedEvent>(RoomStarts.Add);
                Bus.Subscribe<RunCompletedEvent>(RunCompletions.Add);

                Run.Start();
                RoomStarts.Clear(); // Start()가 낸 첫 RoomStartedEvent는 이동과 무관하니 걷어낸다.
            }
        }

        [Test]
        public void 시작_히로민이_문턱과_같으면_바로_이동한다()
        {
            var fx = new Fixture(startingHiromi: MoveThreshold, startingChance: 2, Room("room-1"), Room("room-2"));

            var result = fx.Move.Move();

            Assert.IsFalse(result.Forced);
            Assert.IsFalse(result.RunEnded);
            Assert.AreEqual(0, fx.Hiromi.Remaining);
            Assert.AreEqual(2, fx.Chance.Remaining, "정상 이동은 기회를 쓰지 않는다.");
            Assert.AreEqual(1, fx.RoomStarts.Count);
            Assert.AreEqual(new MemoryRoomId("room-2"), fx.RoomStarts[0].RoomId);
        }

        [Test]
        public void 추출_한_번_후_남은_히로민이_문턱보다_적으면_강제_이동으로_기회를_하나_잃는다()
        {
            // 15 시작 - 추출 9 = 6, 문턱(15)보다 모자란다.
            var fx = new Fixture(startingHiromi: 15 - 9, startingChance: 2, Room("room-1"), Room("room-2"));

            var result = fx.Move.Move();

            Assert.IsTrue(result.Forced);
            Assert.IsFalse(result.RunEnded);
            Assert.AreEqual(0, fx.Hiromi.Remaining, "모자란 채로 강제 이동하면 가진 히로민을 전부 쓴다.");
            Assert.AreEqual(1, fx.Chance.Remaining, "강제 이동은 기회를 하나 잃는다.");
            Assert.AreEqual(1, fx.RoomStarts.Count, "기회가 남아 있으면 이동은 실제로 일어난다.");
        }

        [Test]
        public void 마지막_기회까지_강제_이동에_쓰면_이동하지_않고_런이_끝난다()
        {
            var fx = new Fixture(startingHiromi: 0, startingChance: 1, Room("room-1"), Room("room-2"));

            var result = fx.Move.Move();

            Assert.IsTrue(result.Forced);
            Assert.IsTrue(result.RunEnded);
            Assert.AreEqual(0, fx.Chance.Remaining);
            CollectionAssert.IsEmpty(fx.RoomStarts, "기회가 0이 되면 다음 방으로 넘어가지 않는다.");
            Assert.AreEqual(1, fx.RunCompletions.Count, "기회 소진은 런 종료로 이어진다.");
        }

        [Test]
        public void 히로민이_문턱보다_많으면_문턱만큼만_쓰고_남는다()
        {
            var fx = new Fixture(startingHiromi: 30, startingChance: 2, Room("room-1"), Room("room-2"));

            fx.Move.Move();

            Assert.AreEqual(15, fx.Hiromi.Remaining);
        }
    }
}
