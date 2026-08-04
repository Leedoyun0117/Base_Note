using GameName.Core.Clues;
using GameName.Core.MemoryRooms;

namespace GameName.Core.Events
{
    // 인벤토리의 단서를 원래 방에 되돌려놓았을 때 발행된다. 그 방의 단서
    // 목록을 보여주는 화면(기억 방 화면)이 이 이벤트를 구독해 다시 주울 수
    // 있는 목록에 반영한다.
    public readonly struct ClueReturnedEvent
    {
        public ClueId ClueId { get; }
        public MemoryRoomId RoomId { get; }

        public ClueReturnedEvent(ClueId clueId, MemoryRoomId roomId)
        {
            ClueId = clueId;
            RoomId = roomId;
        }
    }
}
