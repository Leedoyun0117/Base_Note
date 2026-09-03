using System;
using GameName.Core.Clues;
using GameName.Core.Events;
using GameName.Core.Extraction;

namespace GameName.UI.Inventory
{
    // 가방에서 단서를 눌렀을 때 뜨는 패널의 Core 연동.
    //
    // 단서로 할 수 있는 것은 [기억 추출] 하나다. 단서를 대화에 쓰는 것은 이제
    // 대화 줄이 직접 "가진 단서로 답하라"(ClueSelection)로만 이뤄지고, 여기서는
    // 다루지 않는다.
    //
    // 이 컨트롤러가 하는 판단은 "버튼을 눌릴 수 있게 둘 것인가"뿐이고, 그 근거
    // (ClueState, 남은 추출 자원)는 전부 기존 리더에서 읽는다 — 처리기가 최종
    // 판정을 하므로 여기 조건이 살짝 달라도 규칙이 깨지지는 않는다.
    public sealed class ClueUsePanelController : IDisposable
    {
        private readonly IClueUsePanelView _view;
        private readonly ExtractionProcessor _extraction;
        private readonly IClueStateReader _clueState;
        private readonly IExtractionBudget _budget;
        private readonly IDisposable _colorRevealedSubscription;

        private ClueInfo _openClue;
        private bool _awaitingExtractionResult;

        // 단서를 추출해 소비했다 — 가방 격자를 다시 그려야 한다.
        public event Action ClueConsumed;

        public ClueUsePanelController(
            IClueUsePanelView view,
            ExtractionProcessor extraction,
            IClueStateReader clueState,
            IExtractionBudget budget,
            IEventBus eventBus)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _extraction = extraction ?? throw new ArgumentNullException(nameof(extraction));
            _clueState = clueState ?? throw new ArgumentNullException(nameof(clueState));
            _budget = budget ?? throw new ArgumentNullException(nameof(budget));
            if (eventBus == null) throw new ArgumentNullException(nameof(eventBus));

            _view.Extract += OnExtract;
            _view.Closed += OnClosed;

            // 추출 성공 시 어떤 색이 나왔는지는 이 사건으로만 온다(HiddenColor는 저작 진실).
            _colorRevealedSubscription = eventBus.Subscribe<MemoryColorRevealedEvent>(OnColorRevealed);
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
            var extractEnabled = state == ClueState.Collected && _budget.Remaining > 0;
            _view.SetActions(extractEnabled, ExtractReason(state));
        }

        private string ExtractReason(ClueState state)
        {
            switch (state)
            {
                case ClueState.UsedInDialogue: return "이미 대화에 답으로 썼습니다.";
                case ClueState.Extracted: return "이미 기억을 추출했습니다.";
                case ClueState.Available: return "아직 수집하지 않았습니다.";
                default:
                    return _budget.Remaining > 0 ? string.Empty : "남은 추출 자원이 없습니다.";
            }
        }

        private void OnExtract()
        {
            if (_openClue == null)
                return;

            _awaitingExtractionResult = true;
            var result = _extraction.Extract(_openClue.Id);
            _awaitingExtractionResult = false;

            if (!result.Succeeded)
            {
                RenderActions();
                return;
            }

            // 색 문구는 OnColorRevealed가 채운다(같은 호출 스택에서 이미 발행됨).
            RenderActions();
            ClueConsumed?.Invoke();
        }

        private void OnColorRevealed(MemoryColorRevealedEvent e)
        {
            if (!_awaitingExtractionResult || _openClue == null || !e.Source.Equals(_openClue.Id))
                return;

            _view.SetResult("기억이 드러났습니다 — 가방 위 표시줄에서 확인하세요.");
        }

        private void OnClosed()
        {
            _openClue = null;
            _view.Close();
        }

        public void Dispose()
        {
            _view.Extract -= OnExtract;
            _view.Closed -= OnClosed;
            _colorRevealedSubscription.Dispose();
        }
    }
}
