using System;
using GameName.Core.Clues;
using GameName.Core.Events;

namespace GameName.UI.Inventory
{
    // 가방에서 단서를 눌렀을 때 뜨는 패널의 Core 연동.
    //
    // 가방에서 단서로 할 수 있는 것은 [버리기] 하나다. 기억 추출은 이제 대화 줄
    // (ClueSelection)에서 그 자리에 하고, 단서를 대화에 답으로 내미는 것도 그
    // 줄이 직접 다룬다.
    //
    // 이 컨트롤러가 하는 판단은 "버튼을 눌릴 수 있게 둘 것인가"뿐이고, 그 근거
    // (ClueState)는 기존 리더에서 읽는다 — 처리기가 최종 판정을 하므로 여기
    // 조건이 살짝 달라도 규칙이 깨지지는 않는다.
    public sealed class ClueUsePanelController : IDisposable
    {
        private readonly IClueUsePanelView _view;
        private readonly ClueDiscardProcessor _discard;
        private readonly IClueStateReader _clueState;

        private ClueInfo _openClue;

        // 단서를 버려 소비했다 — 가방 격자를 다시 그려야 한다.
        public event Action ClueConsumed;

        public ClueUsePanelController(
            IClueUsePanelView view,
            ClueDiscardProcessor discard,
            IClueStateReader clueState,
            IEventBus eventBus)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _discard = discard ?? throw new ArgumentNullException(nameof(discard));
            _clueState = clueState ?? throw new ArgumentNullException(nameof(clueState));
            if (eventBus == null) throw new ArgumentNullException(nameof(eventBus));

            _view.Discard += OnDiscard;
            _view.Closed += OnClosed;
        }

        public void Open(ClueInfo clue)
        {
            _openClue = clue ?? throw new ArgumentNullException(nameof(clue));
            var title = string.IsNullOrEmpty(clue.DisplayName)
                ? (clue.Kind == ClueKind.Poster ? "포스터 단서" : "물건 단서")
                : clue.DisplayName;
            _view.Open(title);
            RenderActions();
        }

        private void RenderActions()
        {
            if (_openClue == null)
                return;

            _clueState.TryGetState(_openClue.Id, out var state);

            // 손에 있는 단서(Collected·Extracted)면 버릴 수 있다.
            var discardEnabled = state == ClueState.Collected || state == ClueState.Extracted;
            _view.SetDiscardAction(discardEnabled, discardEnabled ? string.Empty : NotInHandReason(state));
        }

        private static string NotInHandReason(ClueState state)
        {
            switch (state)
            {
                case ClueState.UsedInDialogue: return "이미 대화에 답으로 썼습니다.";
                case ClueState.Discarded: return "이미 버렸습니다.";
                default: return "아직 수집하지 않았습니다.";
            }
        }

        private void OnDiscard()
        {
            if (_openClue == null)
                return;

            var result = _discard.Discard(_openClue.Id);
            if (!result.Succeeded)
            {
                RenderActions();
                return;
            }

            _view.SetResult("이 단서를 버렸습니다 — 이번 판에서는 다시 찾을 수 없습니다.");
            RenderActions();
            ClueConsumed?.Invoke();
        }

        private void OnClosed()
        {
            _openClue = null;
            _view.Close();
        }

        public void Dispose()
        {
            _view.Discard -= OnDiscard;
            _view.Closed -= OnClosed;
        }
    }
}
