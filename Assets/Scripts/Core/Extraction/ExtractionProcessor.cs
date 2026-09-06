using System;
using GameName.Core.Clues;
using GameName.Core.Events;
using GameName.Core.Hiromi;
using GameName.Core.Memories;

namespace GameName.Core.Extraction
{
    // 수집한 단서를 추출해 히로민 9를 쓰고 그 안의 기억을 꺼내는 처리기.
    //
    // 부분 성공이 없다는 것이 이 처리기의 핵심 규칙이다. 히로민이 부족하거나,
    // 아직 수집하지 않았거나, 이미 대화에 써 버린 단서라면 아무것도 바꾸지
    // 않고 통째로 실패한다 — 히로민을 절반 쓰고 기억은 안 나오는 상태 같은 것이
    // 존재하면 안 된다. 그래서 모든 검사를 먼저 통과시킨 뒤에야 변경을 시작한다.
    //
    // HiddenColor·Tags는 저작 진실이라 IMemoryRoomClueTracker의 신뢰된 조회
    // (TryGetDefinition)로만 읽는다. 색이 새어 나가면 "무엇을 추출할지" 고르는
    // 선택 자체가 사라진다.
    public sealed class ExtractionProcessor
    {
        // 대화 한 번(+3)의 세 배 — 추출 한 번이 대화 세 번어치 히로민과 맞먹는다.
        // public인 이유: 화면(가방 패널)이 "지금 추출할 수 있는가"를 지갑
        // 잔액만 보고 미리 판단하려면 이 값을 알아야 하고, 그 값이 여기와
        // 어긋나면 버튼은 열려 있는데 실제로는 막히는 어긋남이 생긴다.
        public const int HiromiCost = 9;

        private readonly IHiromiMutator _hiromi;
        private readonly IClueStateMutator _clueState;
        private readonly IExtractedMemoryStoreMutator _memories;
        private readonly IMemoryRoomClueTracker _clueTracker;
        private readonly IEventBus _eventBus;

        public ExtractionProcessor(
            IHiromiMutator hiromi,
            IClueStateMutator clueState,
            IExtractedMemoryStoreMutator memories,
            IMemoryRoomClueTracker clueTracker,
            IEventBus eventBus)
        {
            _hiromi = hiromi ?? throw new ArgumentNullException(nameof(hiromi));
            _clueState = clueState ?? throw new ArgumentNullException(nameof(clueState));
            _memories = memories ?? throw new ArgumentNullException(nameof(memories));
            _clueTracker = clueTracker ?? throw new ArgumentNullException(nameof(clueTracker));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        public ExtractionResult Extract(ClueId clueId)
        {
            if (_hiromi.Remaining < HiromiCost)
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

            _hiromi.Spend(HiromiCost);
            _clueState.SetState(clueId, ClueState.Extracted);
            _memories.Add(new ExtractedMemory(clueId, color, definition.Tags));

            _eventBus.Publish(new ClueExtractedEvent(clueId));
            _eventBus.Publish(new MemoryColorRevealedEvent(color, clueId));

            return ExtractionResult.Success();
        }
    }
}
