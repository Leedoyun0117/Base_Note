namespace GameName.Core.Clues
{
    // 단서 습득 시도의 결과.
    public sealed class ClueCollectionResult
    {
        public bool Succeeded { get; }
        public ClueCollectionFailureReason? FailureReason { get; }

        private ClueCollectionResult(bool succeeded, ClueCollectionFailureReason? failureReason)
        {
            Succeeded = succeeded;
            FailureReason = failureReason;
        }

        public static ClueCollectionResult Success() => new ClueCollectionResult(true, null);

        public static ClueCollectionResult Failure(ClueCollectionFailureReason reason) =>
            new ClueCollectionResult(false, reason);
    }
}
