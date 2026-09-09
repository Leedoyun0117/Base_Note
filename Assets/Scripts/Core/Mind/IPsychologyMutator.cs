namespace GameName.Core.Mind
{
    // 심리 상태를 실제로 바꿀 수 있는 경계.
    //
    // 무엇이 상태를 바꾸는지(피드백 대사, 안정 축의 극단 도달 등)는 아직
    // 진행 규칙 쪽에서 확정되지 않았다. 그 규칙이 어떻게 자리 잡든 상태를
    // 바꾸는 행위 자체는 이 한 메서드로 모은다 — TrustGauge가 "얼마나
    // 깎을지"는 밖에서 받되 변화 발행은 스스로 책임지는 것과 같은 분담이다.
    public interface IPsychologyMutator : IPsychologyReader
    {
        void SetState(PsychologyState state);
    }
}
