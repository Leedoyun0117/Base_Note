namespace GameName.Core.Mind
{
    // 지금 심리 상태를 읽기만 하는 경계.
    // 기억 해석 규칙처럼 상태를 근거로 판단만 하는 쪽은 이쪽만 참조한다.
    public interface IPsychologyReader
    {
        PsychologyState Current { get; }
    }
}
