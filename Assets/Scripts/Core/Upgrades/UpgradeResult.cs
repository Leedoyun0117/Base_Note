namespace GameName.Core.Upgrades
{
    // 업그레이드 구매 시도의 결과.
    public sealed class UpgradeResult
    {
        public bool Succeeded { get; }
        public UpgradeFailureReason? FailureReason { get; }
        public UpgradeOption? PurchasedOption { get; }

        private UpgradeResult(bool succeeded, UpgradeFailureReason? failureReason, UpgradeOption? purchasedOption)
        {
            Succeeded = succeeded;
            FailureReason = failureReason;
            PurchasedOption = purchasedOption;
        }

        public static UpgradeResult Success(UpgradeOption purchasedOption) =>
            new UpgradeResult(true, null, purchasedOption);

        public static UpgradeResult Failure(UpgradeFailureReason reason) =>
            new UpgradeResult(false, reason, null);
    }
}
