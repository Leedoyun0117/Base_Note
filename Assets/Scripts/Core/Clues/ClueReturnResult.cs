namespace GameName.Core.Clues
{
    // 단서를 방에 되돌려놓으려는 시도의 결과.
    public sealed class ClueReturnResult
    {
        public bool Succeeded { get; }
        public ClueReturnFailureReason? FailureReason { get; }

        private ClueReturnResult(bool succeeded, ClueReturnFailureReason? failureReason)
        {
            Succeeded = succeeded;
            FailureReason = failureReason;
        }

        public static ClueReturnResult Success() => new ClueReturnResult(true, null);

        public static ClueReturnResult Failure(ClueReturnFailureReason reason) =>
            new ClueReturnResult(false, reason);
    }
}
