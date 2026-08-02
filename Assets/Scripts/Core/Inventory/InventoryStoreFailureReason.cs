namespace GameName.Core.Inventory
{
    // 담기 실패 사유. 호출부가 사유별로 다른 피드백을 줄 수 있도록 구분한다.
    public enum InventoryStoreFailureReason
    {
        Full,
        ItemTypeNotAccepted,
        Duplicate
    }
}
