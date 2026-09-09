using System.Collections.Generic;
using GameName.Core.Authoring;
using UnityEngine;

namespace GameName.UI.Authoring
{
    // 한 판 전체의 저작 데이터를 에셋으로 들고 있는 자리. 이 에셋 하나가 곧
    // "이번에 플레이할 내용"이다.
    //
    // 방 개수를 여기서 막지 않는 것은 RunDefinition과 같은 이유다 — 개수는
    // 검증기의 규칙이 볼 일이고, 저작 도중에는 방 두 개짜리 에셋이 잠시 있어도
    // 된다.
    [CreateAssetMenu(menuName = "GameName/Run Definition", fileName = "RunDefinition")]
    public sealed class RunDefinitionAsset : ScriptableObject
    {
        [SerializeField] private List<RoomDefinitionAsset> _rooms = new List<RoomDefinitionAsset>();

        [Header("시작 조건")]
        [SerializeField] private int _startingTrust = 50;
        [SerializeField] private int _startingHiromi = 15;
        [SerializeField] private int _startingChance = 2;

        // 다음 기억으로 이동하는 데 드는 히로민이자 "그냥 이동해도 되는가"의
        // 문턱. 시작 히로민과 값을 맞춰 두면 런 시작 시엔 항상 이동할 수 있다.
        [SerializeField] private int _moveHiromiCost = 15;

        // 랜덤 분기 풀을 확정하는 시드. 한 판 동안 고정된다 — 같은 시드면 같은
        // 분기가 뽑힌다. 분기 풀이 없으면 아무 데도 안 쓰인다.
        [SerializeField] private int _seed;

        public IReadOnlyList<RoomDefinitionAsset> Rooms => _rooms;

        public RunDefinition ToDefinition()
        {
            var rooms = new List<RoomDefinition>(_rooms.Count);
            foreach (var room in _rooms)
            {
                if (room != null)
                    rooms.Add(room.ToDefinition());
            }

            return new RunDefinition(
                rooms, _startingTrust, _startingHiromi, _seed, _startingChance, _moveHiromiCost);
        }
    }
}
