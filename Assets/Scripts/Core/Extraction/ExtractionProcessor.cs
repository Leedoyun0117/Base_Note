using System;
using GameName.Core.Clues;
using GameName.Core.Events;
using GameName.Core.Memories;

namespace GameName.Core.Extraction
{
    // 수집한 단서를 추출해 자원 1을 쓰고 그 안의 기억색을 꺼내는 처리기.
    //
    // 부분 성공이 없다는 것이 이 처리기의 핵심 규칙이다. 자원이 부족하거나,
    // 아직 수집하지 않았거나, 이미 대화에 써 버린 단서라면 아무것도 바꾸지
    // 않고 통째로 실패한다 — 자원을 절반 쓰고 색은 안 나오는 상태 같은 것이
    // 존재하면 안 된다. 그래서 모든 검사를 먼저 통과시킨 뒤에야 변경을 시작한다.
    //
    // HiddenColor는 저작 진실이라 IMemoryRoomClueTracker의 신뢰된 조회
    // (TryGetDefinition)로만 읽는다. 이 값이 새어 나가면 "무엇을 추출할지"
    // 고르는 선택 자체가 사라진다.
    public sealed class ExtractionProcessor
    {
        private readonly IExtractionBudgetSpender _budget;
        private readonly IClueStateMutator _clueState;
        private readonly IMemoryColorWalletMutator _wallet;
        private readonly IMemoryRoomClueTracker _clueTracker;
        private readonly IEventBus _eventBus;

        public ExtractionProcessor(
            IExtractionBudgetSpender budget,
            IClueStateMutator clueState,
            IMemoryColorWalletMutator wallet,
            IMemoryRoomClueTracker clueTracker,
            IEventBus eventBus)
        {
            _budget = budget ?? throw new ArgumentNullException(nameof(budget));
            _clueState = clueState ?? throw new ArgumentNullException(nameof(clueState));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _clueTracker = clueTracker ?? throw new ArgumentNullException(nameof(clueTracker));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        public ExtractionResult Extract(ClueId clueId)
        {
            if (_budget.Remaining <= 0)
                return ExtractionResult.Failure(ExtractionFailureReason.ResourceExhausted);

            switch (_clueState.GetState(clueId))
            {
                case ClueState.Collected:
                    break;
                case ClueState.UsedInDialogue:
                    return ExtractionResult.Failure(ExtractionFailureReason.AlreadyUsedInDialogue);
                case ClueState.Extracted:
                    return ExtractionResult.Failure(ExtractionFailureReason.AlreadyExtracted);
                default:
                    return ExtractionResult.Failure(ExtractionFailureReason.NotCollected);
            }

            if (!_clueTracker.TryGetDefinition(clueId, out var definition))
                return ExtractionResult.Failure(ExtractionFailureReason.NotCollected);

            var color = definition.HiddenColor;

            _budget.Spend();
            _clueState.SetState(clueId, ClueState.Extracted);
            _wallet.Add(color, 1);

            _eventBus.Publish(new ClueExtractedEvent(clueId, _budget.Remaining));
            _eventBus.Publish(new MemoryColorRevealedEvent(color, clueId));

            return ExtractionResult.Success();
        }
    }
}
