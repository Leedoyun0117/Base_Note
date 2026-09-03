using GameName.Core.Clues;
using GameName.Core.MemoryRooms;

namespace GameName.Core.Events
{
    // 방에 놓여 있던 단서를 손에 넣었다는 사실.
    // RoomId는 단서를 집어 든 방이다 — 화면은 이 값을 보고 그 방만 다시 그린다.
    public readonly struct ClueCollectedEvent
    {
        public ClueId ClueId { get; }
        public MemoryRoomId RoomId { get; }

        public ClueCollectedEvent(ClueId clueId, MemoryRoomId roomId)
        {
            ClueId = clueId;
            RoomId = roomId;
        }
    }
}
