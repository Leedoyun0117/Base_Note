using System;
using System.Collections.Generic;
using GameName.Core.Authoring;
using GameName.Core.Events;

namespace GameName.Core.Clues
{
    // IClueStateMutator 기본 구현. 단서 하나가 지금 어느 단계(Available →
    // Collected → UsedInDialogue | Extracted)에 있는지를 든다.
    //
    // 전이의 유효성 판단이 여기 있는 이유는 IClueStateMutator 주석 그대로다 —
    // 호출자는 자신이 만든 사건만 통보하고, "Available에서 바로 Extracted로 갈
    // 수 있는가" 같은 판단은 이 구현체가 한다. 흐름은 한 방향이라 되돌아가는
    // 전이는 전부 막는다.
    //
    // 방마다 리셋된다. RoomStartedEvent를 구독해 그 방의 단서를 전부 Available로
    // 다시 시드한다 — 앞 방에서 무엇을 추출했든 다음 방 단서는 처음부터다.
    // 지갑·추출 자원과 달리 단서 단계는 방에 매인 상태이기 때문이다.
    public sealed class ClueStateStore : IClueStateMutator
    {
        private readonly IReadOnlyList<RoomDefinition> _rooms;
        private readonly Dictionary<ClueId, ClueState> _stateById = new Dictionary<ClueId, ClueState>();

        public ClueStateStore(IReadOnlyList<RoomDefinition> rooms, IEventBus eventBus)
        {
            _rooms = rooms ?? throw new ArgumentNullException(nameof(rooms));
            if (eventBus == null) throw new ArgumentNullException(nameof(eventBus));

            eventBus.Subscribe<RoomStartedEvent>(e => Seed(e.RoomIndex));
        }

        // 구성 루트가 첫 방을 시작하기 전에 직접 시드할 수 있게 열어 둔다 —
        // RoomStartedEvent 없이 상태만 확인하는 테스트도 이 경로를 쓴다.
        public void Seed(int roomIndex)
        {
            _stateById.Clear();

            if (roomIndex < 0 || roomIndex >= _rooms.Count)
                return;

            foreach (var clue in _rooms[roomIndex].Clues)
                _stateById[clue.Id] = ClueState.Available;
        }

        public ClueState GetState(ClueId clueId)
        {
            if (!_stateById.TryGetValue(clueId, out var state))
                throw new ArgumentException($"이 방에 없는 단서({clueId})의 단계를 물을 수 없다.", nameof(clueId));

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

        private static bool IsAllowedTransition(ClueState from, ClueState to)
        {
            switch (from)
            {
                case ClueState.Available:
                    return to == ClueState.Collected;
                case ClueState.Collected:
                    return to == ClueState.UsedInDialogue || to == ClueState.Extracted;
                default:
                    // UsedInDialogue, Extracted는 종착점이다.
                    return false;
            }
        }
    }
}
