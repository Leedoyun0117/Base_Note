using System;
using GameName.Core.Authoring;
using GameName.Core.Clues;
using GameName.Core.Events;
using GameName.Core.Memories;
using GameName.Core.MemoryRooms;
using GameName.UI.Inventory;
using NUnit.Framework;

namespace GameName.UI.Tests.EditMode
{
    // 가방 단서 패널 컨트롤러의 규칙:
    //   · [버리기]는 ClueState가 손에 있을 때(Collected·Extracted) 열린다.
    //   · 막힌 경우 사유 문구가 붙는다.
    //   · 버튼 클릭이 ClueDiscardProcessor를 정확히 태운다.
    //
    // 기억 추출과 단서를 대화에 쓰는 것은 이제 대화 줄에서만 이뤄지고 이 패널에는
    // 없다. View는 인터페이스(IClueUsePanelView)로 대체한다.
    public class ClueUsePanelControllerTests
    {
        private static readonly MemoryRoomId TheRoom = new MemoryRoomId("room-1");
        private static readonly ClueId TheClue = new ClueId("clue-1");

        private sealed class FakeView : IClueUsePanelView
        {
            public event Action Discard;
#pragma warning disable CS0067 // 인터페이스가 요구하지만 이 테스트에서는 발생시키지 않는 이벤트.
            public event Action Closed;
#pragma warning restore CS0067

            public int OpenCount;
            public bool DiscardEnabled;
            public string DiscardReason;
            public string LastResult;

            public void Open(string title) => OpenCount++;
            public void Close() { }

            public void SetDiscardAction(bool discardEnabled, string discardReason)
            {
                DiscardEnabled = discardEnabled;
                DiscardReason = discardReason;
            }

            public void SetResult(string message) => LastResult = message;

            public void RaiseDiscard() => Discard?.Invoke();
        }

        private sealed class Fixture
        {
            public readonly EventBus Bus = new EventBus(new NoOpEventExceptionHandler());
            public readonly ClueStateStore ClueState;
            public readonly ClueUsePanelController Controller;
            public readonly FakeView View = new FakeView();
            public int ConsumedCount;

            public Fixture()
            {
                var def = new ClueDefinition(
                    TheClue, ClueKind.Poster, "단서 하나", new CluePositionRatio(0.5f), MemoryColor.Blue);
                var room = new RoomDefinition(
                    TheRoom, new[] { def }, null, System.Array.Empty<GameName.Core.Dialogue.DialogueLineDefinition>());

                ClueState = new ClueStateStore(new[] { room }, Bus);
                ClueState.Seed(0);
                var discard = new ClueDiscardProcessor(ClueState, Bus);

                Controller = new ClueUsePanelController(View, discard, ClueState, Bus);
                Controller.ClueConsumed += () => ConsumedCount++;
            }

            public void Open() =>
                Controller.Open(new ClueInfo(TheClue, ClueKind.Poster, "단서 하나", new CluePositionRatio(0.5f)));
        }

        [Test]
        public void 수집되지_않은_단서는_버리기_버튼이_사유와_함께_비활성이다()
        {
            var fx = new Fixture();
            fx.Open();

            Assert.IsFalse(fx.View.DiscardEnabled);
            Assert.IsFalse(string.IsNullOrEmpty(fx.View.DiscardReason));
        }

        [Test]
        public void 손에_든_단서는_버릴_수_있다()
        {
            var fx = new Fixture();
            fx.ClueState.SetState(TheClue, ClueState.Collected);
            fx.Open();

            Assert.IsTrue(fx.View.DiscardEnabled);
        }

        [Test]
        public void 추출한_단서도_손에_남아_버릴_수_있다()
        {
            var fx = new Fixture();
            fx.ClueState.SetState(TheClue, ClueState.Collected);
            fx.ClueState.SetState(TheClue, ClueState.Extracted);
            fx.Open();

            Assert.IsTrue(fx.View.DiscardEnabled);
        }

        [Test]
        public void 이미_대화에_답으로_쓴_단서는_사유와_함께_비활성이다()
        {
            var fx = new Fixture();
            fx.ClueState.SetState(TheClue, ClueState.Collected);
            fx.ClueState.SetState(TheClue, ClueState.UsedInDialogue);
            fx.Open();

            Assert.IsFalse(fx.View.DiscardEnabled);
            StringAssert.Contains("대화", fx.View.DiscardReason);
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
        }

        [Test]
        public void 이미_버린_단서는_버렸다는_사유가_붙는다()
        {
            var fx = new Fixture();
            fx.ClueState.SetState(TheClue, ClueState.Collected);
            fx.ClueState.SetState(TheClue, ClueState.Discarded);
            fx.Open();

            Assert.IsFalse(fx.View.DiscardEnabled);
            StringAssert.Contains("버렸", fx.View.DiscardReason);
        }
    }
}
