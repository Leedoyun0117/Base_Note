namespace GameName.Core.Hiromi
{
    // 지금 남은 기회를 읽기만 하는 경계.
    public interface IChanceReader
    {
        int Remaining { get; }
    }
}
