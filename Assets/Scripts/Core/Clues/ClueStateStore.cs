using System;
using System.Collections.Generic;
using GameName.Core.Authoring;
using GameName.Core.Events;

namespace GameName.Core.Clues
{
    // IClueStateMutator 기본 구현. 단서 하나가 지금 어느 단계(Available → Used)에
    // 있는지를 든다.
    //
    // 전이의 유효성 판단이 여기 있다 — 호출자는 자신이 만든 사건만 통보하고,
    // "이미 읽은 단서를 다시 읽을 수 있는가" 같은 판단은 이 구현체가 한다.
    // 흐름은 한 방향(Available → Used)뿐이라 되돌아가는 전이는 전부 막는다.
    //
    // RoomStartedEvent를 구독해 그 라운드의 단서를 새로 Available로 시드한다.
    // 이전 라운드에서 이미 알던 단서의 단계는 건드리지 않는다(라운드 사이에
    // 단서 id가 겹치는 저작 오류를 조용히 덮지 않기 위해서다).
    public sealed class ClueStateStore : IClueStateMutator
    {
        private readonly IReadOnlyList<RoomDefinition> _rounds;
        private readonly Dictionary<ClueId, ClueState> _stateById = new Dictionary<ClueId, ClueState>();

        public ClueStateStore(IReadOnlyList<RoomDefinition> rounds, IEventBus eventBus)
        {
            _rounds = rounds ?? throw new ArgumentNullException(nameof(rounds));
            if (eventBus == null) throw new ArgumentNullException(nameof(eventBus));

            eventBus.Subscribe<RoomStartedEvent>(e => Seed(e.RoomIndex));
        }

        // 구성 루트가 첫 라운드를 시작하기 전에 직접 시드할 수 있게 열어 둔다 —
        // RoomStartedEvent 없이 상태만 확인하는 테스트도 이 경로를 쓴다.
        public void Seed(int roundIndex)
        {
            if (roundIndex < 0 || roundIndex >= _rounds.Count)
                return;

            foreach (var clue in _rounds[roundIndex].Clues)
            {
                if (!_stateById.ContainsKey(clue.Id))
                    _stateById[clue.Id] = ClueState.Available;
            }
        }

        public ClueState GetState(ClueId clueId)
        {
            if (!_stateById.TryGetValue(clueId, out var state))
                throw new ArgumentException($"이 라운드에 없는 단서({clueId})의 단계를 물을 수 없다.", nameof(clueId));

            return state;
        }

        public bool TryGetState(ClueId clueId, out ClueState state) =>
            _stateById.TryGetValue(clueId, out state);

        public void SetState(ClueId clueId, ClueState state)
        {
            var current = GetState(clueId);
            if (!IsAllowedTransition(current, state))
            {
                throw new InvalidOperationException(
                    $"단서 {clueId}의 단계를 {current}에서 {state}로 옮길 수 없다.");
            }

            _stateById[clueId] = state;
        }

        private static bool IsAllowedTransition(ClueState from, ClueState to) =>
            from == ClueState.Available && to == ClueState.Used;
    }
}
