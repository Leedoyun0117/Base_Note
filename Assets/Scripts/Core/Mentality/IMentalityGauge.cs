namespace GameName.Core.Mentality
{
    // 정신력 자원 관리 경계.
    // 0이 되면 CanAct가 false가 되어 분석·조향·단서 상호작용이 모두 막힌다는
    // 규칙을 호출부마다 재구현하지 않도록 이 계약에 고정한다.
    // Consume/Restore는 성공 여부만 돌려준다 — 변화 전/후 값이 필요한 쪽은
    // MentalityChangedEvent를 구독해서 얻는다.
    public interface IMentalityGauge
    {
        int CurrentValue { get; }
        int MaxValue { get; }
        bool CanAct { get; }

        bool Consume(int amount);
        bool Restore(int amount);
    }
}
