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

        // 축의 하한(침체 끝)과 상한(흥분 끝). 값이 아니라 범위를 알아야 하는
        // 쪽(게이지를 폭에 매핑하는 HUD 등)이 참조한다 — -100..+100은 저작값일
        // 뿐이라 상수로 박지 않는다.
        int Min { get; }
        int Max { get; }
    }
}
