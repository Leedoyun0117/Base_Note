namespace GameName.Core.Clues
{
    // 단서 읽기 시도의 결과. 실패면 단서 단계도, 턴도, 안정 축도 전혀 바뀌지 않는다.
    public sealed class ClueUseResult
    {
        public bool Succeeded { get; }
        public ClueUseFailureReason? FailureReason { get; }

        private ClueUseResult(bool succeeded, ClueUseFailureReason? failureReason)
        {
            Succeeded = succeeded;
            FailureReason = failureReason;
        }

        public static ClueUseResult Success() => new ClueUseResult(true, null);

        public static ClueUseResult Failure(ClueUseFailureReason reason) =>
            new ClueUseResult(false, reason);
    }
}
