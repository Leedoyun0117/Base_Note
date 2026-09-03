using GameName.Core.MemoryRooms;

namespace GameName.Core.Events
{
    // 방 하나를 실패로 닫았다는 사실.
    //
    // 실패 사유를 아직 싣지 않는 것은 사유의 종류가 정해지지 않았기 때문이다.
    // 사유가 확정되면 별도 열거형을 만들어 여기에 더한다 — 지금 임의로
    // 만들어 두면 실제 규칙과 어긋난 채로 굳는다.
    public readonly struct RoomFailedEvent
    {
        public MemoryRoomId RoomId { get; }

        public RoomFailedEvent(MemoryRoomId roomId)
        {
            RoomId = roomId;
        }
    }
}
