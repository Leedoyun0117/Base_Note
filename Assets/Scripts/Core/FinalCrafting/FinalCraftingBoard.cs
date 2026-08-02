using System.Collections.Generic;
using GameName.Core.Emotions;
using GameName.Core.MemoryRooms;

namespace GameName.Core.FinalCrafting
{
    // IFinalCraftingBoard / IFinalCraftingBoardWriter 기본 구현.
    // IResettable도 함께 구현한다 — 이전 의뢰에서 채운 최종 향이 새 의뢰의
    // 방(식별자가 겹칠 수 있는 다른 세계)으로 넘어가면 안 되므로, 새 의뢰
    // 시작 시 CommissionSession이 비운다.
    public sealed class FinalCraftingBoard : IFinalCraftingBoard, IFinalCraftingBoardWriter, IResettable
    {
        private readonly Dictionary<MemoryRoomId, Scent> _scentsByRoomId = new Dictionary<MemoryRoomId, Scent>();

        public bool TryGet(MemoryRoomId roomId, out Scent scent) => _scentsByRoomId.TryGetValue(roomId, out scent);

        public bool IsCompleteFor(IReadOnlyList<MemoryRoomId> roomIds)
        {
            foreach (var roomId in roomIds)
            {
                if (!_scentsByRoomId.ContainsKey(roomId))
                    return false;
            }

            return true;
        }

        public void Set(MemoryRoomId roomId, Scent scent) => _scentsByRoomId[roomId] = scent;

        public void Reset() => _scentsByRoomId.Clear();
    }
}
