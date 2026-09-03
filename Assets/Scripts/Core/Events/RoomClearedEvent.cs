using GameName.Core.MemoryRooms;

namespace GameName.Core.Events
{
    // 방 하나를 끝냈다는 사실.
    public readonly struct RoomClearedEvent
    {
        public MemoryRoomId RoomId { get; }

        public RoomClearedEvent(MemoryRoomId roomId)
        {
            RoomId = roomId;
        }
    }
}
