using GameName.Core.MemoryRooms;

namespace GameName.Core.Events
{
    // 방에서 보이는 범위가 바뀌었다는 사실. 값은 방 가로 길이에 대한 0~1 비율이다.
    //
    // TrustChangedEvent와 따로 두는 이유: 신뢰도가 움직여도 가시 범위는 그대로일
    // 수 있고(계단식 정책), 반대로 신뢰도 외의 사유로 범위가 바뀔 수도 있다.
    // 화면을 다시 그려야 하는 순간은 신뢰도가 아니라 이쪽이다.
    public readonly struct VisibilityChangedEvent
    {
        public MemoryRoomId RoomId { get; }
        public float PreviousRatio { get; }
        public float CurrentRatio { get; }

        public VisibilityChangedEvent(MemoryRoomId roomId, float previousRatio, float currentRatio)
        {
            RoomId = roomId;
            PreviousRatio = previousRatio;
            CurrentRatio = currentRatio;
        }
    }
}
