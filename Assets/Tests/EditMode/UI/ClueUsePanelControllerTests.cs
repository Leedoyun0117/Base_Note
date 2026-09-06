using System;
using GameName.Core.Authoring;
using GameName.Core.Clues;
using GameName.Core.Events;
using GameName.Core.Extraction;
using GameName.Core.Hiromi;
using GameName.Core.Memories;
using GameName.Core.MemoryRooms;
using GameName.UI.Inventory;
using NUnit.Framework;

namespace GameName.UI.Tests.EditMode
{
    // 가방 단서 패널 컨트롤러의 규칙:
    //   · [기억 추출]은 ClueState가 Collected이고 히로민이 추출 비용(9) 이상일 때만 열린다.
    //   · [버리기]는 ClueState가 Collected이기만 하면 열린다 — 히로민과 무관하다.
    //   · 막힌 경우 사유 문구가 붙는다.
    //   · 버튼 클릭이 ExtractionProcessor/ClueDiscardProcessor를 정확히 태운다.
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
            public event Action Discard;
#pragma warning disable CS0067 // 인터페이스가 요구하지만 이 테스트에서는 발생시키지 않는 이벤트.
            public event Action Closed;
#pragma warning restore CS0067

            public int OpenCount;
            public bool ExtractEnabled;
            public string ExtractReason;
            public bool DiscardEnabled;
            public string DiscardReason;
            public string LastResult;

            public void Open(string title) => OpenCount++;
            public void Close() { }

            public void SetActions(bool extractEnabled, string extractReason)
            {
                ExtractEnabled = extractEnabled;
                ExtractReason = extractReason;
            }

            public void SetDiscardAction(bool discardEnabled, string discardReason)
            {
                DiscardEnabled = discardEnabled;
                DiscardReason = discardReason;
            }

            public void SetResult(string message) => LastResult = message;

            public void RaiseExtract() => Extract?.Invoke();
            public void RaiseDiscard() => Discard?.Invoke();
        }

        private sealed class Fixture
        {
            public readonly EventBus Bus = new EventBus(new NoOpEventExceptionHandler());
            public readonly ClueStateStore ClueState;
            public readonly HiromiWallet Hiromi;
            public readonly ExtractedMemoryStore Memories = new ExtractedMemoryStore();
            public readonly ClueUsePanelController Controller;
            public readonly FakeView View = new FakeView();
            public int ConsumedCount;

            public Fixture(int hiromi = 18)
            {
                var def = new ClueDefinition(
                    TheClue, ClueKind.Poster, "단서 하나", new CluePositionRatio(0.5f), MemoryColor.Blue);
                var room = new RoomDefinition(
                    TheRoom, new[] { def }, null, System.Array.Empty<GameName.Core.Dialogue.DialogueLineDefinition>());
                var tracker = new MemoryRoomClueTracker(new[] { new CluePlacement(TheRoom, def) });

                ClueState = new ClueStateStore(new[] { room }, Bus);
                ClueState.Seed(0);
                Hiromi = new HiromiWallet(hiromi, Bus);
                var extraction = new ExtractionProcessor(Hiromi, ClueState, Memories, tracker, Bus);
                var discard = new ClueDiscardProcessor(ClueState, Bus);

                Controller = new ClueUsePanelController(View, extraction, discard, ClueState, Hiromi, Bus);
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
            var fx = new Fixture(hiromi: 18);
            fx.ClueState.SetState(TheClue, ClueState.Collected);
            fx.Open();

            Assert.IsTrue(fx.View.ExtractEnabled);
        }

        [Test]
        public void 히로민이_추출_비용보다_적으면_사유와_함께_비활성이다()
        {
            var fx = new Fixture(hiromi: 0);
            fx.ClueState.SetState(TheClue, ClueState.Collected);
            fx.Open();

            Assert.IsFalse(fx.View.ExtractEnabled);
            StringAssert.Contains("히로민", fx.View.ExtractReason);
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
        public void 추출_버튼은_ExtractionProcessor를_태우고_히로민을_쓴다()
        {
            var fx = new Fixture(hiromi: 18);
            fx.ClueState.SetState(TheClue, ClueState.Collected);
            fx.Open();

            fx.View.RaiseExtract();

            Assert.AreEqual(ClueState.Extracted, fx.ClueState.GetState(TheClue));
            Assert.AreEqual(9, fx.Hiromi.Remaining);
            Assert.AreEqual(1, fx.ConsumedCount);
        }

        [Test]
        public void 수집되지_않은_단서는_버리기_버튼도_사유와_함께_비활성이다()
        {
            var fx = new Fixture();
            fx.Open();

            Assert.IsFalse(fx.View.DiscardEnabled);
            Assert.IsFalse(string.IsNullOrEmpty(fx.View.DiscardReason));
        }

        [Test]
        public void 수집된_단서는_히로민이_모자라도_버릴_수_있다()
        {
            var fx = new Fixture(hiromi: 0);
            fx.ClueState.SetState(TheClue, ClueState.Collected);
            fx.Open();

            Assert.IsFalse(fx.View.ExtractEnabled, "히로민이 모자라니 추출은 막혀야 한다.");
            Assert.IsTrue(fx.View.DiscardEnabled, "버리기는 히로민과 무관하게 열려야 한다.");
        }

        [Test]
        public void 버리기_버튼은_ClueDiscardProcessor를_태우고_단서를_소비한다()
        {
            var fx = new Fixture();
            fx.ClueState.SetState(TheClue, ClueState.Collected);
            fx.Open();

            fx.View.RaiseDiscard();

            Assert.AreEqual(ClueState.Discarded, fx.ClueState.GetState(TheClue));
            Assert.AreEqual(1, fx.ConsumedCount);
            Assert.IsFalse(fx.View.DiscardEnabled, "버린 뒤에는 다시 버릴 수 없다.");
            Assert.IsFalse(fx.View.ExtractEnabled, "버린 단서는 추출도 할 수 없다.");
        }

        [Test]
        public void 이미_버린_단서는_추출_버튼에_버렸다는_사유가_붙는다()
        {
            var fx = new Fixture();
            fx.ClueState.SetState(TheClue, ClueState.Collected);
            fx.ClueState.SetState(TheClue, ClueState.Discarded);
            fx.Open();

            Assert.IsFalse(fx.View.ExtractEnabled);
            StringAssert.Contains("버렸", fx.View.ExtractReason);
        }
    }
}
