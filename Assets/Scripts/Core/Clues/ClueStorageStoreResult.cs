namespace GameName.Core.Clues
{
    // 보관대에 단서를 담으려는 시도의 결과.
    public sealed class ClueStorageStoreResult
    {
        public bool Succeeded { get; }
        public ClueStorageStoreFailureReason? FailureReason { get; }

        private ClueStorageStoreResult(bool succeeded, ClueStorageStoreFailureReason? failureReason)
        {
            Succeeded = succeeded;
            FailureReason = failureReason;
        }

        public static ClueStorageStoreResult Success() => new ClueStorageStoreResult(true, null);

        public static ClueStorageStoreResult Failure(ClueStorageStoreFailureReason reason) =>
            new ClueStorageStoreResult(false, reason);
    }
}
