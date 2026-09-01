using System;
using System.Collections.Generic;
using GameName.Core.MemoryRooms;

namespace GameName.Core.Clues
{
    // IMemoryRoomClueTracker 기본 구현.
    //
    // 상태를 두 사전으로 나눠 든다:
    //   _definitionsById  — "이 식별자가 무엇인가"(Load 전까지 불변)
    //   _roomIdByClueId   — "지금 어느 방에 놓여 있는가"(플레이 중에 바뀜)
    //
    // 습득 여부를 별도 HashSet으로 두지 않는다. 습득했다는 것은 곧 "어느 방에도
    // 놓여 있지 않다"는 뜻이므로, 배치 사전에서 빠지는 것 하나로 같은 사실을
    // 표현할 수 있다. 두 자료구조로 나누면 "습득되었는데 방에도 있다" 같은
    // 모순 상태가 표현 가능해지고, 그 모순을 막는 책임이 이 타입 여기저기로
    // 흩어진다 — 애초에 표현할 수 없게 만드는 쪽을 택한다.
    //
    // IMemoryRoomClueLoader도 함께 구현한다 — 단서 구성을 바꾼다는 것은 정의
    // 자체가 달라진다는 뜻이라 "습득 기록만 지우는" 초기화로는 표현할 수 없다.
    // 그래서 Load 하나로 정의 교체와 배치 초기화를 함께 처리한다.
    public sealed class MemoryRoomClueTracker : IMemoryRoomClueTracker, IMemoryRoomClueLoader
    {
        private readonly Dictionary<ClueId, ClueDefinition> _definitionsById = new Dictionary<ClueId, ClueDefinition>();
        private readonly Dictionary<ClueId, MemoryRoomId> _roomIdByClueId = new Dictionary<ClueId, MemoryRoomId>();

        // 조회 결과 순서를 등록 순서로 고정하기 위한 목록. 사전만으로 순회하면
        // 항목을 지웠다 넣을 때마다 순서가 달라질 수 있는데, 방 안의 단서 목록
        // 순서는 화면에서 배치 위치를 정하는 데 쓰이므로(왼쪽부터 균등 분배)
        // 단서를 하나 집었다 버렸다는 이유로 나머지 단서가 제자리를 옮기면
        // 안 된다.
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

        public bool TryGetPlacedClue(ClueId clueId, out ClueDefinition definition, out MemoryRoomId roomId)
        {
            if (!_roomIdByClueId.TryGetValue(clueId, out roomId))
            {
                definition = null;
                return false;
            }

            // 배치 사전의 키는 항상 정의 사전의 키 부분집합이므로(Load와
            // PlaceInRoom 둘 다 등록 여부를 먼저 확인한다) 여기서 실패할 수 없다.
            return _definitionsById.TryGetValue(clueId, out definition);
        }

        public bool TryGetDefinition(ClueId clueId, out ClueDefinition definition) =>
            _definitionsById.TryGetValue(clueId, out definition);

        public void MarkCollected(ClueId clueId)
        {
            if (!_definitionsById.ContainsKey(clueId))
                throw new ArgumentException($"등록되지 않은 단서({clueId})를 습득 처리할 수 없다.", nameof(clueId));

            _roomIdByClueId.Remove(clueId);
        }

        public void PlaceInRoom(ClueId clueId, MemoryRoomId roomId)
        {
            if (!_definitionsById.ContainsKey(clueId))
                throw new ArgumentException($"등록되지 않은 단서({clueId})를 방에 놓을 수 없다.", nameof(clueId));

            // 원래 있던 방과 같은지 따지지 않는다 — 어느 방이든 그 방 소속으로
            // 재배정되는 것이 지금의 규칙이다.
            _roomIdByClueId[clueId] = roomId;
        }

        public IReadOnlyList<ClueInfo> GetAvailableClueInfos(MemoryRoomId roomId)
        {
            var result = new List<ClueInfo>();
            foreach (var clueId in _clueIdsInLoadOrder)
            {
                if (!_roomIdByClueId.TryGetValue(clueId, out var placedRoomId))
                    continue;
                if (!placedRoomId.Equals(roomId))
                    continue;

                result.Add(_definitionsById[clueId].ToInfo());
            }

            return result;
        }
    }
}
