using System;
using GameName.Core.Events;

namespace GameName.Core.Clues
{
    // 손에 든 단서를 이 런에서 완전히 버리는 유일한 경로.
    //
    // 단서 상태가 런 전체에 걸쳐 누적되면서 인벤토리가 곧 상한이 됐다 — 새 방의
    // 단서를 집으려면 손에 있는 것을 내줘야 할 수 있다. 이 처리기가 그 자리를
    // 만든다. 추출한 단서도 손에 남으므로 함께 버릴 수 있다(방을 넘길 때 남은
    // 것을 정리하는 경로가 이걸 그대로 쓴다). 추출과 달리 아무것도 돌려주지
    // 않는다 — 그래서 실패 사유가 하나뿐이다: 지금 손에 없으면 버릴 수 없다.
    public sealed class ClueDiscardProcessor
    {
        private readonly IClueStateMutator _clueState;
        private readonly IEventBus _eventBus;

        public ClueDiscardProcessor(IClueStateMutator clueState, IEventBus eventBus)
        {
            _clueState = clueState ?? throw new ArgumentNullException(nameof(clueState));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        public ClueDiscardResult Discard(ClueId clueId)
        {
            if (!_clueState.TryGetState(clueId, out var state)
                || (state != ClueState.Collected && state != ClueState.Extracted))
            {
                return ClueDiscardResult.Failure(ClueDiscardFailureReason.NotCollected);
            }

            _clueState.SetState(clueId, ClueState.Discarded);
            _eventBus.Publish(new ClueDiscardedEvent(clueId));

            return ClueDiscardResult.Success();
        }
    }
}
