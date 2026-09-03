using System;
using GameName.Core.Authoring;
using GameName.Core.Clues;
using GameName.Core.Events;
using GameName.Core.Extraction;
using GameName.Core.Memories;
using GameName.Core.MemoryRooms;
using GameName.UI.Inventory;
using NUnit.Framework;

namespace GameName.UI.Tests.EditMode
{
    // 가방 단서 패널 컨트롤러의 규칙:
    //   · [기억 추출]은 ClueState가 Collected이고 남은 추출 자원이 있을 때만 열린다.
    //   · 막힌 경우 사유 문구가 붙는다.
    //   · 버튼 클릭이 ExtractionProcessor를 정확히 태운다.
    //
    // 단서를 대화에 쓰는 것은 대화 줄의 ClueSelection으로만 이뤄지고 이 패널에는
    // 없다. View는 인터페이스(IClueUsePanelView)로 대체한다.
    public class ClueUsePanelControllerTests
    {
        private static readonly MemoryRoomId TheRoom = new MemoryRoomId("room-1");
        private static readonly ClueId TheClue = new ClueId("clue-1");

        private sealed class FakeView : IClueUsePanelView
        {
            public event Action Extract;
#pragma warning disable CS0067 // 인터페이스가 요구하지만 이 테스트에서는 발생시키지 않는 이벤트.
            public event Action Closed;
#pragma warning restore CS0067

            public int OpenCount;
            public bool ExtractEnabled;
            public string ExtractReason;
            public string LastResult;

            public void Open(string title) => OpenCount++;
            public void Close() { }

            public void SetActions(bool extractEnabled, string extractReason)
            {
                ExtractEnabled = extractEnabled;
                ExtractReason = extractReason;
            }

            public void SetResult(string message) => LastResult = message;

            public void RaiseExtract() => Extract?.Invoke();
        }

        private sealed class Fixture
        {
            public readonly EventBus Bus = new EventBus(new NoOpEventExceptionHandler());
            public readonly ClueStateStore ClueState;
            public readonly ExtractionBudget Budget;
            public readonly MemoryColorWallet Wallet = new MemoryColorWallet();
            public readonly ClueUsePanelController Controller;
            public readonly FakeView View = new FakeView();
            public int ConsumedCount;

            public Fixture(int budget = 3)
            {
                var def = new ClueDefinition(
                    TheClue, ClueKind.Poster, "단서 하나", new CluePositionRatio(0.5f), MemoryColor.Blue);
                var room = new RoomDefinition(
                    TheRoom, new[] { def }, null, System.Array.Empty<GameName.Core.Dialogue.DialogueLineDefinition>());
                var tracker = new MemoryRoomClueTracker(new[] { new CluePlacement(TheRoom, def) });

                ClueState = new ClueStateStore(new[] { room }, Bus);
                ClueState.Seed(0);
                Budget = new ExtractionBudget(budget);
                var extraction = new ExtractionProcessor(Budget, ClueState, Wallet, tracker, Bus);

                Controller = new ClueUsePanelController(View, extraction, ClueState, Budget, Bus);
                Controller.ClueConsumed += () => ConsumedCount++;
            }

            public void Open() =>
                Controller.Open(new ClueInfo(TheClue, ClueKind.Poster, "단서 하나", new CluePositionRatio(0.5f)));
        }

        [Test]
        public void 수집되지_않은_단서는_추출_버튼이_사유와_함께_비활성이다()
        {
            var fx = new Fixture();
            fx.Open();

            Assert.IsFalse(fx.View.ExtractEnabled);
            Assert.IsFalse(string.IsNullOrEmpty(fx.View.ExtractReason));
        }

        [Test]
        public void 수집된_단서는_추출할_수_있다()
        {
            var fx = new Fixture(budget: 3);
            fx.ClueState.SetState(TheClue, ClueState.Collected);
            fx.Open();

            Assert.IsTrue(fx.View.ExtractEnabled);
        }

        [Test]
        public void 남은_추출_자원이_0이면_사유와_함께_비활성이다()
        {
            var fx = new Fixture(budget: 0);
            fx.ClueState.SetState(TheClue, ClueState.Collected);
            fx.Open();

            Assert.IsFalse(fx.View.ExtractEnabled);
            StringAssert.Contains("자원", fx.View.ExtractReason);
        }

        [Test]
        public void 이미_대화에_답으로_쓴_단서는_사유와_함께_비활성이다()
        {
            var fx = new Fixture();
            fx.ClueState.SetState(TheClue, ClueState.Collected);
            fx.ClueState.SetState(TheClue, ClueState.UsedInDialogue);
            fx.Open();

            Assert.IsFalse(fx.View.ExtractEnabled);
            StringAssert.Contains("대화", fx.View.ExtractReason);
        }

        [Test]
        public void 이미_추출한_단서는_사유와_함께_비활성이다()
        {
            var fx = new Fixture();
            fx.ClueState.SetState(TheClue, ClueState.Collected);
            fx.ClueState.SetState(TheClue, ClueState.Extracted);
            fx.Open();

            Assert.IsFalse(fx.View.ExtractEnabled);
            StringAssert.Contains("추출", fx.View.ExtractReason);
        }

        [Test]
        public void 추출_버튼은_ExtractionProcessor를_태우고_자원을_쓴다()
        {
            var fx = new Fixture(budget: 3);
            fx.ClueState.SetState(TheClue, ClueState.Collected);
            fx.Open();

            fx.View.RaiseExtract();

            Assert.AreEqual(ClueState.Extracted, fx.ClueState.GetState(TheClue));
            Assert.AreEqual(2, fx.Budget.Remaining);
            Assert.AreEqual(1, fx.ConsumedCount);
        }
    }
}
