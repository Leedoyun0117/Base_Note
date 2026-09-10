using System;
using GameName.Core.Clues;
using GameName.Core.Events;

namespace GameName.Core.Complexes
{
    // 단서를 클릭해 그 서사를 읽는 유일한 경로. 이 한 번의 조작이 한 턴을 쓴다.
    //
    // 순서가 중요하다:
    //   1. 원본 태그를 활성 컴플렉스 체인에 통과시켜 최종 태그를 얻는다.
    //   2. ClueInterpretedEvent를 낸다 → 감정 반응이 안정 축을 민다.
    //   3. 단서를 Used로 소모한다(재사용 불가).
    //   4. ClueUsedEvent를 낸다 → 스토리 패널이 서사를 그린다.
    //   5. TurnCoordinator.AdvanceTurn() → 컴플렉스 지속 턴 감소, 그리고 이번
    //      턴의 컴플렉스 발생 굴림(방금 움직인 안정 축을 본다).
    // 2가 5보다 먼저여야, 이번 턴의 발생 확률이 이 단서로 흔들린 안정 축을
    // 반영한다.
    public sealed class ClueUseProcessor
    {
        private readonly IClueStateMutator _clueState;
        private readonly IMemoryRoomClueTracker _clueTracker;
        private readonly IComplexChainResolver _resolver;
        private readonly IActiveComplexListReader _activeComplexes;
        private readonly TurnCoordinator _turns;
        private readonly IEventBus _eventBus;

        public ClueUseProcessor(
            IClueStateMutator clueState,
            IMemoryRoomClueTracker clueTracker,
            IComplexChainResolver resolver,
            IActiveComplexListReader activeComplexes,
            TurnCoordinator turns,
            IEventBus eventBus)
        {
            _clueState = clueState ?? throw new ArgumentNullException(nameof(clueState));
            _clueTracker = clueTracker ?? throw new ArgumentNullException(nameof(clueTracker));
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            _activeComplexes = activeComplexes ?? throw new ArgumentNullException(nameof(activeComplexes));
            _turns = turns ?? throw new ArgumentNullException(nameof(turns));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        public ClueUseResult Use(ClueId clueId)
        {
            if (!_clueState.TryGetState(clueId, out var state) || state != ClueState.Available)
                return ClueUseResult.Failure(ClueUseFailureReason.NotAvailable);

            if (!_clueTracker.TryGetDefinition(clueId, out var definition))
                return ClueUseResult.Failure(ClueUseFailureReason.NotAvailable);

            var interpretation = _resolver.Resolve(
                definition.Tags, _activeComplexes.DefinitionsInPriorityOrder);
            _eventBus.Publish(ClueInterpretedEvent.From(interpretation));

            _clueState.SetState(clueId, ClueState.Used);
            _eventBus.Publish(new ClueUsedEvent(clueId, definition.DisplayName, definition.Story));

            _turns.AdvanceTurn();

            return ClueUseResult.Success();
        }
    }
}
