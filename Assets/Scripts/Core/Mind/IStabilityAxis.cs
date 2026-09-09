namespace GameName.Core.Mind
{
    // 안정 축을 실제로 움직일 수 있는 경계.
    //
    // 신뢰(ITrustGauge)와 달리 양방향이다 — 피드백 대사가 나츠를 침체 쪽으로도,
    // 흥분 쪽으로도 밀 수 있다. 얼마나 밀지는 그 대사의 저작 데이터가 정하고,
    // 축 자신은 양 끝(침체 하한 ~ 흥분 상한)으로 자르는 것과 변화 이벤트
    // 발행만 책임진다.
    public interface IStabilityAxis : IStabilityReader
    {
        void Shift(int delta);
    }
}
