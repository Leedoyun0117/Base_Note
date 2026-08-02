namespace GameName.Core.Inventory
{
    // 인벤토리 용량 수치는 밸런싱 대상이라 매직 넘버로 두지 않고 외부에서
    // 주입받는다.
    public interface IInventorySettings
    {
        int InitialCapacity { get; }
    }
}
