namespace GameName.Core.MemoryRooms
{
    // 플레이어 위치를 실제로 바꿀 수 있는 경계.
    // 이동이 성공했을 때만 호출되어야 하므로, MemoryRoomMovementProcessor 외의
    // 다른 시스템에는 이 인터페이스를 주입하지 않는다 — 다른 시스템은 읽기
    // 전용인 IPlayerLocation만 받는다. IPlayerLocation을 상속하는 이유는 이동
    // 처리기 스스로도 "지금 위치"를 읽어야 하기 때문에, 읽기/쓰기 능력을 하나의
    // 타입으로 함께 받기 위함이다.
    public interface IPlayerLocationMover : IPlayerLocation
    {
        void MoveTo(MemoryGraphNodeId nodeId);
    }
}
