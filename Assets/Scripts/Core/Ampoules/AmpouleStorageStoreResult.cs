namespace GameName.Core.Ampoules
{
    // 조향실 보관함에 앰플을 담으려는 시도의 결과.
    public sealed class AmpouleStorageStoreResult
    {
        public bool Succeeded { get; }
        public AmpouleStorageStoreFailureReason? FailureReason { get; }

        private AmpouleStorageStoreResult(bool succeeded, AmpouleStorageStoreFailureReason? failureReason)
        {
            Succeeded = succeeded;
            FailureReason = failureReason;
        }

        public static AmpouleStorageStoreResult Success() => new AmpouleStorageStoreResult(true, null);

        public static AmpouleStorageStoreResult Failure(AmpouleStorageStoreFailureReason reason) =>
            new AmpouleStorageStoreResult(false, reason);
    }
}
