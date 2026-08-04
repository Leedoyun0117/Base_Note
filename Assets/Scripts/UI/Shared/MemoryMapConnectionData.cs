using GameName.Core.MemoryRooms;

namespace GameName.UI.Shared
{
    // 지도가 연결선 하나를 그리는 데 필요한 정보만 모은 화면 전용 값.
    public readonly struct MemoryMapConnectionData
    {
        public MemoryGraphCoordinate From { get; }
        public MemoryGraphCoordinate To { get; }
        public bool IsLadder { get; }
        public bool IsLocked { get; }

        public MemoryMapConnectionData(
            MemoryGraphCoordinate from, MemoryGraphCoordinate to, bool isLadder, bool isLocked)
        {
            From = from;
            To = to;
            IsLadder = isLadder;
            IsLocked = isLocked;
        }
    }
}
