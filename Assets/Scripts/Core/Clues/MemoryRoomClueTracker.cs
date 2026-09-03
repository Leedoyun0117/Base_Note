using System;
using System.Collections.Generic;
using GameName.Core.MemoryRooms;

namespace GameName.Core.Clues
{
    // IMemoryRoomClueTracker 기본 구현 — 단서 정의와 최초 배치의 카탈로그.
    //
    // 예전에는 "지금 어느 방에 있는가"가 플레이 중에 바뀌는 상태였지만(단서를
    // 아무 방에나 버릴 수 있었다), 버리기가 사라지면서 배치는 다시 불변 저작
    // 사실이 되었다. "집었는가"도 여기서 빠졌다 — 그 진실은 ClueState 하나뿐이다.
    // 그래서 이 타입은 이제 Load로 채워진 두 맵을 읽기만 한다.
    //
    // IMemoryRoomClueLoader도 함께 구현한다 — 단서 구성을 통째로 갈아 끼우는
    // 유일한 경로다.
    public sealed class MemoryRoomClueTracker : IMemoryRoomClueTracker, IMemoryRoomClueLoader
    {
        private readonly Dictionary<ClueId, ClueDefinition> _definitionsById = new Dictionary<ClueId, ClueDefinition>();
        private readonly Dictionary<ClueId, MemoryRoomId> _roomIdByClueId = new Dictionary<ClueId, MemoryRoomId>();

        // 조회 결과 순서를 등록 순서로 고정하기 위한 목록. 방 안의 단서 목록
        // 순서는 화면에서 배치 위치를 정하는 데 쓰이므로(왼쪽부터 균등 분배)
        // 흔들리면 안 된다.
        private readonly List<ClueId> _clueIdsInLoadOrder = new List<ClueId>();

        public MemoryRoomClueTracker(IReadOnlyList<CluePlacement> placements)
        {
            Load(placements);
        }

        public void Load(IReadOnlyList<CluePlacement> placements)
        {
            if (placements == null) throw new ArgumentNullException(nameof(placements));

            _definitionsById.Clear();
            _roomIdByClueId.Clear();
            _clueIdsInLoadOrder.Clear();

            foreach (var placement in placements)
            {
                var definition = placement.Clue;
                if (_definitionsById.ContainsKey(definition.Id))
                    throw new ArgumentException($"단서 식별자가 중복되었다({definition.Id}).", nameof(placements));

                _definitionsById.Add(definition.Id, definition);
                _roomIdByClueId.Add(definition.Id, placement.RoomId);
                _clueIdsInLoadOrder.Add(definition.Id);
            }
        }

        public bool TryGetDefinition(ClueId clueId, out ClueDefinition definition) =>
            _definitionsById.TryGetValue(clueId, out definition);

        public IReadOnlyList<ClueInfo> GetCluesInRoom(MemoryRoomId roomId)
        {
            var result = new List<ClueInfo>();
            foreach (var clueId in _clueIdsInLoadOrder)
            {
                if (_roomIdByClueId.TryGetValue(clueId, out var placedRoomId) && placedRoomId.Equals(roomId))
                    result.Add(_definitionsById[clueId].ToInfo());
            }

            return result;
        }
    }
}
