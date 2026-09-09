using System;
using GameName.Core.Clues;
using GameName.Core.Events;

namespace GameName.Core.Memories
{
    // 대화의 답으로 내민 단서에 딸린 추출된 기억을 그 자리에서 소모하는 리스너.
    //
    // "방 사전추출 → 대화 중 즉석 추출 → 답변 사용 → 소모"의 마지막 단계다.
    // ClueUsedInDialogueEvent를 듣는 이유: 그 답이 맞았는지(ClueAnsweredEvent)와
    // 무관하게, 넘어가기가 아니라 실제로 물건을 내밀었을 때만 이 사건이 난다 —
    // "낸 물건만 소모된다"는 규칙이 그대로 성립한다.
    //
    // 추출하지 않은 단서를 답으로 냈으면 저장소에 없어 아무 일도 하지 않는다.
    //
    // 별도 처리기가 아니라 리스너로 둔 것은 [10](심리 × 안정 조합)이 답변에
    // 따라 소모 여부 자체를 흔들 수 있어서다 — 그때 이 리스너를 감싸거나
    // 조건을 얹는다. 지금은 무조건 소모한다.
    public sealed class ExtractedMemoryConsumptionListener
    {
        private readonly IExtractedMemoryStoreMutator _memories;
        private readonly IEventBus _eventBus;

        public ExtractedMemoryConsumptionListener(
            IExtractedMemoryStoreMutator memories, IEventBus eventBus)
        {
            _memories = memories ?? throw new ArgumentNullException(nameof(memories));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));

            _eventBus.Subscribe<ClueUsedInDialogueEvent>(e => Consume(e.ClueId));
        }

        private void Consume(ClueId sourceClueId)
        {
            if (!_memories.TryGet(sourceClueId, out var memory))
                return;

            _memories.Remove(sourceClueId);
            _eventBus.Publish(new ExtractedMemoryConsumedEvent(sourceClueId, memory.Color));
        }
    }
}
