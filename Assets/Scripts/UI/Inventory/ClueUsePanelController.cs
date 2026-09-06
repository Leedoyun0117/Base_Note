using System;
using GameName.Core.Clues;
using GameName.Core.Events;
using GameName.Core.Extraction;
using GameName.Core.Hiromi;

namespace GameName.UI.Inventory
{
    // 가방에서 단서를 눌렀을 때 뜨는 패널의 Core 연동.
    //
    // 단서로 할 수 있는 것은 [기억 추출]과 [버리기] 둘이다. 단서를 대화에 쓰는
    // 것은 이제 대화 줄이 직접 "가진 단서로 답하라"(ClueSelection)로만 이뤄지고,
    // 여기서는 다루지 않는다.
    //
    // 이 컨트롤러가 하는 판단은 "버튼을 눌릴 수 있게 둘 것인가"뿐이고, 그 근거
    // (ClueState, 히로민 잔액)는 전부 기존 리더에서 읽는다 — 처리기가 최종
    // 판정을 하므로 여기 조건이 살짝 달라도 규칙이 깨지지는 않는다.
    public sealed class ClueUsePanelController : IDisposable
    {
        private readonly IClueUsePanelView _view;
        private readonly ExtractionProcessor _extraction;
        private readonly ClueDiscardProcessor _discard;
        private readonly IClueStateReader _clueState;
        private readonly IHiromiReader _hiromi;
        private readonly IDisposable _colorRevealedSubscription;

        private ClueInfo _openClue;
        private bool _awaitingExtractionResult;

        // 단서를 추출하거나 버려 소비했다 — 가방 격자를 다시 그려야 한다.
        public event Action ClueConsumed;

        public ClueUsePanelController(
            IClueUsePanelView view,
            ExtractionProcessor extraction,
            ClueDiscardProcessor discard,
            IClueStateReader clueState,
            IHiromiReader hiromi,
            IEventBus eventBus)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _extraction = extraction ?? throw new ArgumentNullException(nameof(extraction));
            _discard = discard ?? throw new ArgumentNullException(nameof(discard));
            _clueState = clueState ?? throw new ArgumentNullException(nameof(clueState));
            _hiromi = hiromi ?? throw new ArgumentNullException(nameof(hiromi));
            if (eventBus == null) throw new ArgumentNullException(nameof(eventBus));

            _view.Extract += OnExtract;
            _view.Discard += OnDiscard;
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
            var extractEnabled = state == ClueState.Collected && _hiromi.Remaining >= ExtractionProcessor.HiromiCost;
            _view.SetActions(extractEnabled, ExtractReason(state));

            var discardEnabled = state == ClueState.Collected;
            _view.SetDiscardAction(discardEnabled, discardEnabled ? string.Empty : NotCollectedReason(state));
        }

        private string ExtractReason(ClueState state)
        {
            switch (state)
            {
                case ClueState.UsedInDialogue: return "이미 대화에 답으로 썼습니다.";
                case ClueState.Extracted: return "이미 기억을 추출했습니다.";
                case ClueState.Discarded: return "이미 버렸습니다.";
                case ClueState.Available: return "아직 수집하지 않았습니다.";
                default:
                    return _hiromi.Remaining >= ExtractionProcessor.HiromiCost
                        ? string.Empty
                        : "히로민이 모자랍니다.";
            }
        }

        // 버리기는 히로민과 무관하다 — 손에 들고 있기만 하면 언제든 버릴 수
        // 있다. 손에 없는 이유는 추출 사유와 같은 어휘로 답한다.
        private static string NotCollectedReason(ClueState state)
        {
            switch (state)
            {
                case ClueState.UsedInDialogue: return "이미 대화에 답으로 썼습니다.";
                case ClueState.Extracted: return "이미 기억을 추출했습니다.";
                case ClueState.Discarded: return "이미 버렸습니다.";
                default: return "아직 수집하지 않았습니다.";
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
            _view.Discard -= OnDiscard;
            _view.Closed -= OnClosed;
            _colorRevealedSubscription.Dispose();
        }
    }
}
