using GameName.Core.MemoryRooms;

namespace GameName.Core.Events
{
    // 기억 방 하나가 완전히 복원되었을 때 발행된다.
    // 정신력 회복(약 20)은 이 이벤트를 구독하는 정신력 시스템이 처리할 몫이며,
    // 이 계약은 "어떤 방이 복원됐는지"만 알린다.
    public readonly struct MemoryRoomRestoredEvent
    {
        public MemoryRoomId RoomId { get; }

        public MemoryRoomRestoredEvent(MemoryRoomId roomId)
        {
            RoomId = roomId;
        }
    }
}
