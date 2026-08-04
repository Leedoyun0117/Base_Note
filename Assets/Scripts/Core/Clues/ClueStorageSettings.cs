namespace GameName.Core.Clues
{
    // IClueStorageSettings의 생성자 주입 단순 구현체.
    public sealed class ClueStorageSettings : IClueStorageSettings
    {
        public int MaxStoredClues { get; }

        public ClueStorageSettings(int maxStoredClues)
        {
            MaxStoredClues = maxStoredClues;
        }
    }
}
