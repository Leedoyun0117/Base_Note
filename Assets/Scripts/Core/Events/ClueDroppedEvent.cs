using GameName.Core.Clues;
using GameName.Core.MemoryRooms;

namespace GameName.Core.Events
{
    // 인벤토리에 있던 단서가 방에 버려졌다는 사실.
    //
    // RoomId는 "원래 있던 방"이 아니라 "방금 놓인 방"이다 — 버리기로 소속이
    // 재배정되므로 이 값이 곧 그 단서의 새 소속이다. 화면은 이 이벤트를 듣고
    // 그 방을 다시 그린다.
    public readonly struct ClueDroppedEvent
    {
        public ClueId ClueId { get; }
        public MemoryRoomId RoomId { get; }

        public ClueDroppedEvent(ClueId clueId, MemoryRoomId roomId)
        {
            ClueId = clueId;
            RoomId = roomId;
        }
    }
}
