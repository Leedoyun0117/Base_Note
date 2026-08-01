namespace GameName.Core.Judging
{
    // IScentJudgementSettings의 단순 구현체. 값은 생성자로만 받고 이후 바뀌지
    // 않는다 — 나중에 ScriptableObject 등 다른 설정 소스로 갈아끼울 때도 판정
    // 로직은 인터페이스에만 의존하도록 유지하기 위함이다.
    public sealed class ScentJudgementSettings : IScentJudgementSettings
    {
        public double HighAccuracyThreshold { get; }

        public ScentJudgementSettings(double highAccuracyThreshold)
        {
            HighAccuracyThreshold = highAccuracyThreshold;
        }
    }
}
