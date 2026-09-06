namespace GameName.Core.Clues
{
    // 단서 버리기 시도의 결과.
    public sealed class ClueDiscardResult
    {
        public bool Succeeded { get; }
        public ClueDiscardFailureReason? FailureReason { get; }

        private ClueDiscardResult(bool succeeded, ClueDiscardFailureReason? failureReason)
        {
            Succeeded = succeeded;
            FailureReason = failureReason;
        }

        public static ClueDiscardResult Success() => new ClueDiscardResult(true, null);

        public static ClueDiscardResult Failure(ClueDiscardFailureReason reason) =>
            new ClueDiscardResult(false, reason);
    }
}
