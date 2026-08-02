namespace GameName.Core.Inventory
{
    // 인벤토리에서 물건을 꺼내려는 시도의 결과.
    public sealed class InventoryRemoveResult
    {
        public bool Succeeded { get; }
        public InventoryRemoveFailureReason? FailureReason { get; }

        private InventoryRemoveResult(bool succeeded, InventoryRemoveFailureReason? failureReason)
        {
            Succeeded = succeeded;
            FailureReason = failureReason;
        }

        public static InventoryRemoveResult Success() => new InventoryRemoveResult(true, null);

        public static InventoryRemoveResult Failure(InventoryRemoveFailureReason reason) =>
            new InventoryRemoveResult(false, reason);
    }
}
