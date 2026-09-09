using GameName.Core.MemoryRooms;

namespace GameName.Core.Progression
{
    // 지금 방이 어느 국면(조사/대화)에 있는지 읽기만 하는 경계.
    //
    // 국면을 옮기는 것은 RoomPhaseCoordinator 하나뿐이고, 그 판단의 근거(조사
    // 횟수 등)를 보는 쪽은 전부 이쪽만 참조한다.
    public interface IRoomPhaseReader
    {
        RoomPhase Current { get; }
    }
}
