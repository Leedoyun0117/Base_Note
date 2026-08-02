using System;
using System.Collections.Generic;
using GameName.Core.MemoryRooms;

namespace GameName.Core.Clues
{
    // IMemoryRoomClueTracker 기본 구현.
    //
    // 습득 여부를 정의 목록과 별도의 HashSet(_collectedClueIds)으로 관리한다.
    // 정의 자체는 지우지 않고 "습득됨" 표시만 남기는 방식이라, 나중에
    // "인벤토리에서 방으로 되돌리기" 기능이 필요해지면 이 표시만 지우는
    // 메서드 하나를 추가하는 것으로 확장할 수 있다 — 이번에는 그 기능(되돌리기)을
    // 구현하지 않는다.
    //
    // IMemoryRoomClueLoader도 함께 구현한다 — 새 의뢰는 정의 자체가 다르므로
    // "습득 기록만 지우는 Reset"은 의미가 없다(정의가 바뀌면 습득 기록은
    // 저절로 무의미해진다). 그래서 별도 IResettable 없이 Load 하나로 정의 교체와
    // 습득 기록 초기화를 함께 처리한다.
    public sealed class MemoryRoomClueTracker : IMemoryRoomClueTracker, IMemoryRoomClueLoader
    {
        private readonly Dictionary<ClueId, ClueDefinition> _definitionsById = new Dictionary<ClueId, ClueDefinition>();
        private readonly HashSet<ClueId> _collectedClueIds = new HashSet<ClueId>();

        public MemoryRoomClueTracker(IReadOnlyList<ClueDefinition> clueDefinitions)
        {
            Load(clueDefinitions);
        }

        public void Load(IReadOnlyList<ClueDefinition> clueDefinitions)
        {
            if (clueDefinitions == null) throw new ArgumentNullException(nameof(clueDefinitions));

            _definitionsById.Clear();
            _collectedClueIds.Clear();

            foreach (var definition in clueDefinitions)
            {
                if (_definitionsById.ContainsKey(definition.Id))
                    throw new ArgumentException($"단서 식별자가 중복되었다({definition.Id}).", nameof(clueDefinitions));

                _definitionsById.Add(definition.Id, definition);
            }
        }

        public bool TryGetAvailableDefinition(ClueId clueId, out ClueDefinition definition)
        {
            if (_collectedClueIds.Contains(clueId))
            {
                definition = null;
                return false;
            }

            return _definitionsById.TryGetValue(clueId, out definition);
        }

        public bool TryGetDefinition(ClueId clueId, out ClueDefinition definition) =>
            _definitionsById.TryGetValue(clueId, out definition);

        public void MarkCollected(ClueId clueId)
        {
            if (!_definitionsById.ContainsKey(clueId))
                throw new ArgumentException($"등록되지 않은 단서({clueId})를 습득 처리할 수 없다.", nameof(clueId));

            _collectedClueIds.Add(clueId);
        }

        public IReadOnlyList<ClueInfo> GetAvailableClueInfos(MemoryRoomId roomId)
        {
            var result = new List<ClueInfo>();
            foreach (var definition in _definitionsById.Values)
            {
                if (!definition.RoomId.Equals(roomId))
                    continue;
                if (_collectedClueIds.Contains(definition.Id))
                    continue;

                result.Add(definition.ToInfo());
            }

            return result;
        }
    }
}
