using System;
using System.Collections.Generic;
using GameName.Core.Authoring;
using GameName.Core.Dialogue;
using GameName.Core.Events;
using GameName.Core.Hiromi;
using GameName.Core.MemoryRooms;
using GameName.Core.Progression;
using GameName.UI.Inventory;
using NUnit.Framework;

namespace GameName.UI.Tests.EditMode
{
    // 레버는 히로민이 문턱 이상이면 곧장 이동을 부르고, 모자라면 확인 팝업을
    // 띄운다 — 취소하면 아무것도 소모되지 않고, 확인해야만 실제로 이동한다.
    public class MemoryMoveLeverControllerTests
    {
        private const int MoveThreshold = 15;

        private sealed class FakeView : IMemoryMoveLeverView
        {
            public event Action MoveClicked;
            public event Action ForceConfirmed;
            public event Action ForceCancelled;

            public bool MoveEnabled = true;
            public bool ConfirmVisible;
            public string LastConfirmMessage;

            public void SetMoveEnabled(bool enabled) => MoveEnabled = enabled;

            public void ShowForceConfirm(string message)
            {
                LastConfirmMessage = message;
                ConfirmVisible = true;
            }

            public void HideForceConfirm() => ConfirmVisible = false;

            public void RaiseMoveClicked() => MoveClicked?.Invoke();
            public void RaiseForceConfirmed() => ForceConfirmed?.Invoke();
            public void RaiseForceCancelled() => ForceCancelled?.Invoke();
        }

        private static RoomDefinition Room(string id) =>
            new RoomDefinition(
                new MemoryRoomId(id), Array.Empty<GameName.Core.Clues.ClueDefinition>(), null,
                Array.Empty<DialogueLineDefinition>());

        private sealed class Fixture
        {
            public readonly EventBus Bus = new EventBus(new NoOpEventExceptionHandler());
            public readonly HiromiWallet Hiromi;
            public readonly ChanceTracker Chance;
            public readonly RunProgressor Run;
            public readonly MemoryMoveProcessor Move;
            public readonly FakeView View = new FakeView();
            public readonly MemoryMoveLeverController Controller;
            public readonly List<RoomStartedEvent> RoomStarts = new List<RoomStartedEvent>();

            public Fixture(int startingHiromi, int startingChance = 2)
            {
                Hiromi = new HiromiWallet(startingHiromi, Bus);
                Chance = new ChanceTracker(startingChance, Bus);
                Run = new RunProgressor(new[] { Room("room-1"), Room("room-2") }, Bus);
                Move = new MemoryMoveProcessor(MoveThreshold, Hiromi, Chance, Run);

                Bus.Subscribe<RoomStartedEvent>(RoomStarts.Add);
                Run.Start();
                RoomStarts.Clear();

                Controller = new MemoryMoveLeverController(View, Move, Hiromi, Chance, MoveThreshold, Bus);
            }
        }

        [Test]
        public void 히로민이_문턱_이상이면_확인_없이_곧장_이동한다()
        {
            var fx = new Fixture(startingHiromi: 15);

            fx.View.RaiseMoveClicked();

            Assert.IsFalse(fx.View.ConfirmVisible);
            Assert.AreEqual(1, fx.RoomStarts.Count);
            Assert.AreEqual(0, fx.Hiromi.Remaining);
        }

        [Test]
        public void 히로민이_모자라면_확인_팝업이_뜨고_아직_이동하지_않는다()
        {
            var fx = new Fixture(startingHiromi: 6);

            fx.View.RaiseMoveClicked();

            Assert.IsTrue(fx.View.ConfirmVisible);
            StringAssert.Contains("2", fx.View.LastConfirmMessage, "남은 기회 수를 보여줘야 한다.");
            CollectionAssert.IsEmpty(fx.RoomStarts);
            Assert.AreEqual(6, fx.Hiromi.Remaining, "확인 전에는 아무것도 소모되지 않는다.");
            Assert.AreEqual(2, fx.Chance.Remaining);
        }

        [Test]
        public void 확인_팝업을_취소하면_아무것도_소모되지_않고_방에_머문다()
        {
            var fx = new Fixture(startingHiromi: 6);
            fx.View.RaiseMoveClicked();

            fx.View.RaiseForceCancelled();

            Assert.IsFalse(fx.View.ConfirmVisible);
            CollectionAssert.IsEmpty(fx.RoomStarts);
            Assert.AreEqual(6, fx.Hiromi.Remaining);
            Assert.AreEqual(2, fx.Chance.Remaining, "취소는 기회를 쓰지 않는다.");
        }

        [Test]
        public void 확인_팝업에서_확정하면_기회를_하나_쓰고_이동한다()
        {
            var fx = new Fixture(startingHiromi: 6);
            fx.View.RaiseMoveClicked();

            fx.View.RaiseForceConfirmed();

            Assert.IsFalse(fx.View.ConfirmVisible);
            Assert.AreEqual(1, fx.Chance.Remaining);
            Assert.AreEqual(1, fx.RoomStarts.Count);
            Assert.AreEqual(0, fx.Hiromi.Remaining);
        }
    }
}
