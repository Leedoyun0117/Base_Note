namespace GameName.Core.Trust
{
    // 현재 신뢰도를 읽기만 하는 경계.
    // 가시성 정책, 대사 조건 등 신뢰도를 근거로 판단만 하는 쪽은 전부 이쪽만
    // 참조한다.
    public interface ITrustReader
    {
        int Current { get; }
    }
}
