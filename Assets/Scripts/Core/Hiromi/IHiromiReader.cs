namespace GameName.Core.Hiromi
{
    // 지금 히로민을 얼마나 갖고 있는지 읽기만 하는 경계.
    // 화면 표시, "이 행동을 할 수 있는가" 판정은 이쪽만 참조한다.
    public interface IHiromiReader
    {
        int Remaining { get; }
    }
}
