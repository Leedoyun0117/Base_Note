namespace GameName.Core.MemoryRooms
{
    // 플레이어가 지금 어느 노드(기억 방/허브)에 있는지 읽기만 하는 경계.
    // 단서 수집, 분석, 시향 등 "지금 위치"가 필요한 시스템은 전부 이 인터페이스만
    // 참조한다. 위치를 바꿀 수 있는 쪽은 IPlayerLocationMover 하나뿐이어야
    // 하므로, 읽기만 하면 되는 시스템에는 쓰기 능력 자체를 아예 노출하지 않는다.
    public interface IPlayerLocation
    {
        MemoryGraphNodeId Current { get; }
    }
}
