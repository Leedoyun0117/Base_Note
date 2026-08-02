namespace GameName.Core.FinalCrafting
{
    // 최종 조향 시도의 결과.
    public sealed class FinalCraftingResult
    {
        public bool Succeeded { get; }
        public FinalCraftingFailureReason? FailureReason { get; }

        private FinalCraftingResult(bool succeeded, FinalCraftingFailureReason? failureReason)
        {
            Succeeded = succeeded;
            FailureReason = failureReason;
        }

        public static FinalCraftingResult Success() => new FinalCraftingResult(true, null);
        public static FinalCraftingResult Failure(FinalCraftingFailureReason reason) =>
            new FinalCraftingResult(false, reason);
    }
}
