namespace GameName.Core.Ampoules
{
    // IAmpouleStorageSettings의 생성자 주입 단순 구현체.
    public sealed class AmpouleStorageSettings : IAmpouleStorageSettings
    {
        public int MaxStoredAmpoules { get; }

        public AmpouleStorageSettings(int maxStoredAmpoules)
        {
            MaxStoredAmpoules = maxStoredAmpoules;
        }
    }
}
