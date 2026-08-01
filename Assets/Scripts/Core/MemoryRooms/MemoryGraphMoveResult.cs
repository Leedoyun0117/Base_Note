namespace GameName.Core.MemoryRooms
{
    // 이동 시도 결과. 성공/실패와 실패 사유를 함께 담는다.
    public sealed class MemoryGraphMoveResult
    {
        public bool Succeeded { get; }
        public MemoryGraphMoveFailureReason? FailureReason { get; }

        private MemoryGraphMoveResult(bool succeeded, MemoryGraphMoveFailureReason? failureReason)
        {
            Succeeded = succeeded;
            FailureReason = failureReason;
        }

        public static MemoryGraphMoveResult Success() => new MemoryGraphMoveResult(true, null);

        public static MemoryGraphMoveResult Failure(MemoryGraphMoveFailureReason reason) =>
            new MemoryGraphMoveResult(false, reason);
    }
}
