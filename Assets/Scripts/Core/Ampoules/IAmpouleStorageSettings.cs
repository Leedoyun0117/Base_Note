namespace GameName.Core.Ampoules
{
    // 조향실에 보관할 수 있는 앰플 개수 상한. 업그레이드로 늘어날 수 있으므로
    // 매직 넘버로 코드에 두지 않고 외부에서 주입받는다.
    public interface IAmpouleStorageSettings
    {
        int MaxStoredAmpoules { get; }
    }
}
