namespace GameName.Core.MemoryRooms
{
    // 기억 방 두 개를 잇는 세로 연결(사다리).
    // 아래쪽 방(LowerRoom)이 완전히 복원되어야만 양방향으로 통행할 수 있다.
    // "한 번 열린 사다리는 이후 제약 없이 쓸 수 있다"는 규칙은 별도로 구현할
    // 필요가 없다 — 복원 상태(IMemoryRoomRestorationTracker)가 애초에 되돌아가지
    // 않는 단조 증가 상태이기 때문에 자연히 성립한다.
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
