using System.Collections.Generic;
using GameName.Core.Authoring;
using GameName.Core.Clues;
using GameName.Core.Dialogue;
using GameName.Core.MemoryRooms;
using GameName.UI.MemoryRoom.Space;
using UnityEngine;

namespace GameName.UI.Authoring
{
    // 방 하나의 저작 데이터를 에셋으로 들고 있는 자리.
    //
    // 방 치수(_layout)는 여기서 참조하지만 RoomDefinition으로는 넘기지 않는다.
    // 치수는 방을 화면에 어떻게 그릴지에 대한 값이라 표시 계층에 남아야 하고,
    // Core로 넘어가면 GameName.Core가 UnityEngine에 묶인다. 참조를 이 에셋에
    // 두는 것은 씬 구성 도구가 "이 방을 그릴 때 쓸 치수"를 한 곳에서 찾게 하기
    // 위해서다.
    [CreateAssetMenu(menuName = "GameName/Room Definition", fileName = "RoomDefinition")]
    public sealed class RoomDefinitionAsset : ScriptableObject
    {
        [SerializeField] private string _roomId;
        [SerializeField] private MemoryRoomLayoutAsset _layout;
        [SerializeField] private List<ClueDefinitionAsset> _clues = new List<ClueDefinitionAsset>();

        [Header("대사")]
        // 방에 들어섰을 때 시작되는 대사. 아직 대사를 붙이지 않았으면 비워 둔다.
        [SerializeField] private string _startLineId;
        [SerializeField] private List<DialogueLineDefinitionAsset> _lines =
            new List<DialogueLineDefinitionAsset>();

        // 고정 백본 사이에 끼는 랜덤 분기 풀. 각 풀은 런 시작 시 후보 하나로
        // 확정되어 위 _lines에 병합된다(BranchResolver). 백본 라인은 풀의 슬롯
        // id로 Next를 건다.
        [SerializeField] private List<BranchPoolAsset> _branchPools = new List<BranchPoolAsset>();

        public MemoryRoomLayoutAsset Layout => _layout;

        public MemoryRoomId RoomId => new MemoryRoomId(_roomId);

        public RoomDefinition ToDefinition()
        {
            var clues = new List<ClueDefinition>(_clues.Count);
            foreach (var clue in _clues)
            {
                // 인스펙터에서 비어 있는 칸은 눈에 그대로 보이므로, 그것 하나
                // 때문에 방 전체를 못 읽게 만들지 않는다.
                if (clue != null)
                    clues.Add(clue.ToDefinition());
            }

            var lines = new List<DialogueLineDefinition>(_lines.Count);
            foreach (var line in _lines)
            {
                if (line != null)
                    lines.Add(line.ToDefinition());
            }

            var branchPools = new List<BranchPool>(_branchPools.Count);
            foreach (var pool in _branchPools)
            {
                if (pool != null)
                    branchPools.Add(pool.ToDefinition());
            }

            return new RoomDefinition(
                RoomId, clues, AuthoredIds.OptionalLine(_startLineId), lines, branchPools);
        }
    }
}
