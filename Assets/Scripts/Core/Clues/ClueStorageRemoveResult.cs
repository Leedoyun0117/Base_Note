namespace GameName.Core.Clues
{
    // 보관대에서 단서를 꺼내려는 시도의 결과.
    public sealed class ClueStorageRemoveResult
    {
        public bool Succeeded { get; }
        public ClueStorageRemoveFailureReason? FailureReason { get; }

        private ClueStorageRemoveResult(bool succeeded, ClueStorageRemoveFailureReason? failureReason)
        {
            Succeeded = succeeded;
            FailureReason = failureReason;
        }

        public static ClueStorageRemoveResult Success() => new ClueStorageRemoveResult(true, null);

        public static ClueStorageRemoveResult Failure(ClueStorageRemoveFailureReason reason) =>
            new ClueStorageRemoveResult(false, reason);
    }
}
