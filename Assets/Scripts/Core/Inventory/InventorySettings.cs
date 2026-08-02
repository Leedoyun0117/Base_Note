namespace GameName.Core.Inventory
{
    // IInventorySettings의 생성자 주입 단순 구현체.
    public sealed class InventorySettings : IInventorySettings
    {
        public int InitialCapacity { get; }

        public InventorySettings(int initialCapacity)
        {
            InitialCapacity = initialCapacity;
        }
    }
}
