namespace GameName.Core.Clues
{
    // 분석실 보관대에 놓아둘 수 있는 단서 개수 상한. 업그레이드 등으로 늘어날
    // 가능성에 대비해 매직 넘버로 코드에 두지 않고 외부에서 주입받는다 —
    // IAmpouleStorageSettings와 같은 이유다.
    public interface IClueStorageSettings
    {
        int MaxStoredClues { get; }
    }
}
