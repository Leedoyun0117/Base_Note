namespace GameName.Core.Clues
{
    // 보관대↔인벤토리 이동 시도의 결과.
    public sealed class ClueTransferResult
    {
        public bool Succeeded { get; }
        public ClueTransferFailureReason? FailureReason { get; }

        private ClueTransferResult(bool succeeded, ClueTransferFailureReason? failureReason)
        {
            Succeeded = succeeded;
            FailureReason = failureReason;
        }

        public static ClueTransferResult Success() => new ClueTransferResult(true, null);

        public static ClueTransferResult Failure(ClueTransferFailureReason reason) =>
            new ClueTransferResult(false, reason);
    }
}
