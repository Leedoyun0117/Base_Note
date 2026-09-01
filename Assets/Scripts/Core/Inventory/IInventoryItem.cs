namespace GameName.Core.Inventory
{
    // 인벤토리에 담을 수 있는 물건이 갖춰야 할 최소 계약.
    // 서로 다른 타입이 이 마커만 구현하면 인벤토리는 구체 타입을
    // 몰라도 담고 꺼낼 수 있다. Category는 슬롯 정책이 종류별 규칙을 적용할 수
    // 있게 하기 위한 최소 정보다.
    public interface IInventoryItem
    {
        InventoryItemCategory Category { get; }
    }
}
