namespace GameName.Core.MemoryRooms
{
    // 기억 방 두 개를 잇는 세로 연결(사다리). 위아래 관계를 담는 것이 열린
    // 통로(OpenConnection)와 다른 점이며, 그 방향은 화면이 사다리를 문과 다르게
    // 그리는 근거이자 LadderRespectsDepthOrderRule이 검사하는 대상이다.
    public readonly struct LadderConnection
    {
        public MemoryRoomId UpperRoom { get; }
        public MemoryRoomId LowerRoom { get; }

        public LadderConnection(MemoryRoomId upperRoom, MemoryRoomId lowerRoom)
        {
            UpperRoom = upperRoom;
            LowerRoom = lowerRoom;
        }
    }
}
