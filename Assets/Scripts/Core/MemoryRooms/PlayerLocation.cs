using System;

namespace GameName.Core.MemoryRooms
{
    // IPlayerLocationMover 기본 구현. 플레이어의 현재 위치에 대한 단일 진실
    // 원천이다. 그래프 유효성 검사는 하지 않는다 — 그건 MemoryRoomMovementProcessor가
    // 이동을 허용하기 전에 이미 끝낸 일이고, 이 타입은 오직 "지금 값이 무엇인가"만
    // 정확히 보유하는 것이 유일한 책임이다.
    public sealed class PlayerLocation : IPlayerLocationMover
    {
        public MemoryGraphNodeId Current { get; private set; }

        public PlayerLocation(MemoryGraphNodeId initialPosition)
        {
            if (string.IsNullOrWhiteSpace(initialPosition.Value))
                throw new ArgumentException("초기 위치가 유효하지 않다.", nameof(initialPosition));

            Current = initialPosition;
        }

        public void MoveTo(MemoryGraphNodeId nodeId)
        {
            Current = nodeId;
        }
    }
}
