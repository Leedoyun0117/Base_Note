namespace GameName.Core.Mind
{
    // 심리 상태를 실제로 바꿀 수 있는 경계.
    //
    // 무엇이 상태를 바꾸는가 — 피드백 대사가 극단으로 밀 때, 안정 축이 양 끝에
    // 닿을 때 등 — 는 이번 개편(2차) 범위에 없다. MemoryEffectResolver가 심리
    // 상태를 읽어 등급을 왜곡하는 규칙은 들어갔지만, 그 상태를 낙관에서 벗어나게
    // 만드는 전이 규칙은 별도 기획이 필요하다. 그때까지 SetState는 호출자가
    // 없고, 게임은 시작 심리(데모: 낙관)로만 돈다. 규칙이 어떻게 자리 잡든
    // 상태를 바꾸는 행위 자체는 이 한 메서드로 모은다.
    public interface IPsychologyMutator : IPsychologyReader
    {
        void SetState(PsychologyState state);
    }
}
