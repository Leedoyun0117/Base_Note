namespace GameName.Core.Ampoules
{
    // 보관함↔인벤토리 이동 시도의 결과.
    public sealed class AmpouleTransferResult
    {
        public bool Succeeded { get; }
        public AmpouleTransferFailureReason? FailureReason { get; }

        private AmpouleTransferResult(bool succeeded, AmpouleTransferFailureReason? failureReason)
        {
            Succeeded = succeeded;
            FailureReason = failureReason;
        }

        public static AmpouleTransferResult Success() => new AmpouleTransferResult(true, null);

        public static AmpouleTransferResult Failure(AmpouleTransferFailureReason reason) =>
            new AmpouleTransferResult(false, reason);
    }
}
