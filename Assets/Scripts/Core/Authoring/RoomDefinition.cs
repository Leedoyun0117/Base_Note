using System;
using System.Collections.Generic;
using GameName.Core.Clues;
using GameName.Core.Dialogue;
using GameName.Core.MemoryRooms;

namespace GameName.Core.Authoring
{
    // 방 하나에 대해 기획이 적어 넣은 모든 것.
    //
    // 방 치수(MemoryRoomLayout)는 여기 없다. 그것은 방을 화면에 어떻게 그릴지에
    // 대한 값이라 표시 계층의 것이고, 세 방이 같은 규격을 쓴다는 전제 위에서
    // 에셋 하나로 공유된다. 이 타입은 "그 방에 무엇이 있고 무슨 말이 오가는가"만
    // 안다 — 그래야 검증기가 Unity 없이 이 데이터만 읽고 판단할 수 있다.
    public sealed class RoomDefinition
    {
        public MemoryRoomId Id { get; }
        public IReadOnlyList<ClueDefinition> Clues { get; }

        // 방에 들어섰을 때 시작되는 대사. 대사를 아직 붙이지 않은 방이면 null이다 —
        // 껍데기만 놓고 배치부터 잡아 보는 것이 실제 저작 순서이기 때문에,
        // 대사가 비어 있다는 이유로 검증에 걸려서는 안 된다.
        public DialogueLineId? StartLineId { get; }

        public IReadOnlyList<DialogueLineDefinition> DialogueLines { get; }

        // 고정 백본 사이에 끼는 랜덤 분기 풀들. 저작·검증 시점에만 쓰인다 —
        // BranchResolver가 런 시작 시 각 풀에서 후보 하나를 뽑아 DialogueLines에
        // 병합하고, 그 뒤로는 이 목록이 빈 방(= 확정본)만 플레이에 흐른다.
        public IReadOnlyList<BranchPool> BranchPools { get; }

        public RoomDefinition(
            MemoryRoomId id,
            IReadOnlyList<ClueDefinition> clues,
            DialogueLineId? startLineId,
            IReadOnlyList<DialogueLineDefinition> dialogueLines,
            IReadOnlyList<BranchPool> branchPools = null)
        {
            Id = id;
            Clues = clues ?? throw new ArgumentNullException(nameof(clues));
            StartLineId = startLineId;
            DialogueLines = dialogueLines ?? throw new ArgumentNullException(nameof(dialogueLines));
            BranchPools = branchPools ?? Array.Empty<BranchPool>();
        }

        // 이 방에 저작된 모든 대사 줄 — 고정 라인과 분기 풀 후보 전부.
        //
        // 저작 시점 검사(선택지 조건, 단서로 답하는 줄의 성립 등)가 "대사가 붙은
        // 자리"를 빠짐없이 보게 하는 단일 출처다. 풀 후보가 늘거나 새 저작 자리가
        // 생겨도 규칙마다 순회를 다시 적지 않는다. BranchResolver로 확정된 뒤에는
        // 풀이 비어 DialogueLines와 같아진다.
        public IEnumerable<DialogueLineDefinition> EnumerateAuthoredLines()
        {
            foreach (var line in DialogueLines)
                yield return line;

            foreach (var pool in BranchPools)
            foreach (var candidate in pool.Candidates)
                yield return candidate;
        }
    }
}
