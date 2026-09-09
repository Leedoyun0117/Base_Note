using System;
using System.Collections.Generic;
using GameName.Core.Authoring;
using GameName.Core.Events;

namespace GameName.Core.Clues
{
    // IClueStateMutator 기본 구현. 단서 하나가 지금 어느 단계(Available →
    // Collected → UsedInDialogue | Extracted | Discarded)에 있는지를 든다.
    //
    // 전이의 유효성 판단이 여기 있는 이유는 IClueStateMutator 주석 그대로다 —
    // 호출자는 자신이 만든 사건만 통보하고, "Available에서 바로 Extracted로 갈
    // 수 있는가" 같은 판단은 이 구현체가 한다. 흐름은 한 방향이라 되돌아가는
    // 전이는 전부 막는다.
    //
    // 런 전체에 걸쳐 누적된다. RoomStartedEvent를 구독해 그 방의 단서를 새로
    // Available로 더할 뿐, 앞 방에서 이미 알고 있던 단서의 단계는 건드리지
    // 않는다 — 다른 방(기억)의 단서를 지금 대화에 쓸 수 있어야 하기 때문이다.
    // 방을 실패해 못 모은 단서는 그대로 없는 채로 넘어간다(Collected가 안 됐을
    // 뿐 Available로는 남아, 이론상 다음 방에서도 여전히 "그 방에 있었다"는
    // 사실 자체는 유지된다 — 다만 그 방을 다시 방문할 방법이 없어 실제로는
    // 집을 수 없다).
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
        //
        // 이미 알고 있는 단서(이 방을 다시 시드하는 비정상 재진입, 또는 다른
        // 방에서 이미 등록된 단서 id가 우연히 겹치는 저작 오류)는 건드리지
        // 않는다 — 새로 등장하는 단서만 Available로 더한다.
        public void Seed(int roomIndex)
        {
            if (roomIndex < 0 || roomIndex >= _rooms.Count)
                return;

            foreach (var clue in _rooms[roomIndex].Clues)
            {
                if (!_stateById.ContainsKey(clue.Id))
                    _stateById[clue.Id] = ClueState.Available;
            }
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
                    return to == ClueState.UsedInDialogue || to == ClueState.Extracted || to == ClueState.Discarded;
                case ClueState.Extracted:
                    // 추출해도 손에는 남는다 — 답으로 내밀거나(대화), 방을 넘길
                    // 때 버려진다.
                    return to == ClueState.UsedInDialogue || to == ClueState.Discarded;
                default:
                    // UsedInDialogue, Discarded는 종착점이다.
                    return false;
            }
        }
    }
}
