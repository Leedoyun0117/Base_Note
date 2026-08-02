namespace GameName.Core.Ampoules
{
    // 조향실 보관함에서 앰플을 꺼내려는 시도의 결과.
    public sealed class AmpouleStorageRemoveResult
    {
        public bool Succeeded { get; }
        public AmpouleStorageRemoveFailureReason? FailureReason { get; }

        private AmpouleStorageRemoveResult(bool succeeded, AmpouleStorageRemoveFailureReason? failureReason)
        {
            Succeeded = succeeded;
            FailureReason = failureReason;
        }

        public static AmpouleStorageRemoveResult Success() => new AmpouleStorageRemoveResult(true, null);

        public static AmpouleStorageRemoveResult Failure(AmpouleStorageRemoveFailureReason reason) =>
            new AmpouleStorageRemoveResult(false, reason);
    }
}
