namespace GameName.Core.MemoryRooms
{
    // 이동 실패 사유. 호출부가 사유별로 다른 피드백을 줄 수 있도록 구분한다.
    public enum MemoryGraphMoveFailureReason
    {
        NoConnection,
        LadderLocked,
        InsufficientMentality
    }
}
