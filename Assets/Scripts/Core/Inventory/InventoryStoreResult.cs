namespace GameName.Core.Inventory
{
    // 인벤토리에 물건을 담으려는 시도의 결과.
    public sealed class InventoryStoreResult
    {
        public bool Succeeded { get; }
        public InventoryStoreFailureReason? FailureReason { get; }

        private InventoryStoreResult(bool succeeded, InventoryStoreFailureReason? failureReason)
        {
            Succeeded = succeeded;
            FailureReason = failureReason;
        }

        public static InventoryStoreResult Success() => new InventoryStoreResult(true, null);

        public static InventoryStoreResult Failure(InventoryStoreFailureReason reason) =>
            new InventoryStoreResult(false, reason);
    }
}
