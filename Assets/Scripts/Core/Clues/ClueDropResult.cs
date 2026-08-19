namespace GameName.Core.Clues
{
    // 단서를 지금 있는 방에 버리려는 시도의 결과.
    public sealed class ClueDropResult
    {
        public bool Succeeded { get; }
        public ClueDropFailureReason? FailureReason { get; }

        private ClueDropResult(bool succeeded, ClueDropFailureReason? failureReason)
        {
            Succeeded = succeeded;
            FailureReason = failureReason;
        }

        public static ClueDropResult Success() => new ClueDropResult(true, null);

        public static ClueDropResult Failure(ClueDropFailureReason reason) =>
            new ClueDropResult(false, reason);
    }
}
