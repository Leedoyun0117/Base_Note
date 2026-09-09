using GameName.Core.MemoryRooms;

namespace GameName.Core.Events
{
    // 방의 국면이 바뀌었다는 사실(조사 → 대화).
    //
    // 방이 시작될 때 조사 국면으로 한 번, 대화가 시작될 때 대화 국면으로 한 번
    // 발행된다. 화면(상단 바의 "조사 중"/"대화 중" 표시)과 조사 횟수 제한이
    // 이 사실을 구독한다.
    public readonly struct RoomPhaseChangedEvent
    {
        public RoomPhase Phase { get; }

        public RoomPhaseChangedEvent(RoomPhase phase)
        {
            Phase = phase;
        }
    }
}
