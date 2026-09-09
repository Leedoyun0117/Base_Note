namespace GameName.Core.Mind
{
    // 지금 안정 축 위치를 읽기만 하는 경계.
    //
    // 위치는 부호 있는 값이다: 음수는 침체, 0은 안정, 양수는 흥분. 신뢰 감소량
    // 계산, 히로민 회복 보너스, 기억 해석 규칙 등 위치를 근거로 판단만 하는
    // 쪽은 전부 이쪽만 참조한다.
    public interface IStabilityReader
    {
        int Position { get; }
    }
}
