using System.Collections.Generic;
using GameName.Core.Authoring;
using GameName.Core.Clues;
using GameName.Core.Complexes;
using GameName.Core.MemoryRooms;
using GameName.UI.MemoryRoom.Space;
using UnityEngine;

namespace GameName.UI.Authoring
{
    // 라운드 하나의 저작 데이터를 에셋으로 들고 있는 자리.
    //
    // 방 치수(_layout)는 여기서 참조하지만 RoomDefinition으로는 넘기지 않는다 —
    // 치수는 표시 계층에 남아야 한다. 참조를 이 에셋에 두는 것은 씬 구성 도구가
    // "이 라운드를 그릴 때 쓸 치수"를 한 곳에서 찾게 하기 위해서다.
    [CreateAssetMenu(menuName = "GameName/Round Definition", fileName = "RoundDefinition")]
    public sealed class RoomDefinitionAsset : ScriptableObject
    {
        [SerializeField] private string _roomId;
        [SerializeField] private MemoryRoomLayoutAsset _layout;
        [SerializeField] private List<ClueDefinitionAsset> _clues = new List<ClueDefinitionAsset>();

        [Header("라운드 규칙")]
        [Min(1)]
        [SerializeField] private int _turnsToSurvive = 4;

        // 라운드 시작 시 걸려 있는 컴플렉스 id. 비우면 컴플렉스 없이 시작.
        [SerializeField] private string _startingComplexId;

        // 안정 축 이탈로 추가 발생할 수 있는 컴플렉스 후보 id들.
        [SerializeField] private List<string> _complexPoolIds = new List<string>();

        public MemoryRoomLayoutAsset Layout => _layout;

        public MemoryRoomId RoomId => new MemoryRoomId(_roomId);

        public RoomDefinition ToDefinition()
        {
            var clues = new List<ClueDefinition>(_clues.Count);
            foreach (var clue in _clues)
            {
                if (clue != null)
                    clues.Add(clue.ToDefinition());
            }

            ComplexId? starting = string.IsNullOrWhiteSpace(_startingComplexId)
                ? (ComplexId?)null
                : new ComplexId(_startingComplexId);

            var pool = new List<ComplexId>();
            foreach (var id in _complexPoolIds)
            {
                if (!string.IsNullOrWhiteSpace(id))
                    pool.Add(new ComplexId(id));
            }

            return new RoomDefinition(RoomId, clues, _turnsToSurvive, starting, pool);
        }
    }
}
