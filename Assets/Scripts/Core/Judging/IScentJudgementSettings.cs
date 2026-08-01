namespace GameName.Core.Judging
{
    // 조향 판정에서 쓰이는 임계값 설정.
    // 밸런싱 대상이라 코드에 상수로 박아두지 않고 외부에서 주입받는다.
    public interface IScentJudgementSettings
    {
        // 정확도가 이 값 이상이면 PianoAndViolin 단계로 올라간다.
        // 최상위 단계(완전 일치)는 이 임계값과 무관하게 배분 값의 동등 비교로
        // 별도 판정하므로, 이 값을 1로 둔다고 해서 최상위 단계가 막히지 않는다.
        double HighAccuracyThreshold { get; }
    }
}
