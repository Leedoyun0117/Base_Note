using GameName.Core.MemoryRooms;

namespace GameName.Core.Events
{
    // 플레이어의 현재 위치가 실제로 바뀌었을 때 발행된다. 이동이 "성공"으로
    // 처리되어도 출발지와 목적지가 같으면 위치가 바뀐 게 아니므로 발행되지
    // 않는다. UI가 위치를 매 프레임 폴링하지 않고 이 이벤트만 구독해 갱신할
    // 수 있게 하기 위한 것이다.
    public readonly struct MemoryRoomMoveCompletedEvent
    {
        public MemoryGraphNodeId PreviousPosition { get; }
        public MemoryGraphNodeId NewPosition { get; }

        public MemoryRoomMoveCompletedEvent(MemoryGraphNodeId previousPosition, MemoryGraphNodeId newPosition)
        {
            PreviousPosition = previousPosition;
            NewPosition = newPosition;
        }
    }
}
