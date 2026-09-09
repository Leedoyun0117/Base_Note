namespace GameName.Core.Clues
{
    // 단서 하나가 지금 어느 단계에 있는지.
    //
    // 흐름은 거의 한 방향이다. UsedInDialogue와 Extracted를 나눠 두는 이유는
    // 둘이 서로 다른 소모이기 때문이다 — 추출은 대화 중에 히로민을 써서 그
    // 기억(과 색)을 복원도에 올리는 것이고, 답으로 내미는 것은 별개다.
    //
    // 그래서 Extracted는 종착점이 아니다: 추출한 단서도 아직 손에 있어 그
    // 대화 줄에 답으로 낼 수 있고(Extracted → UsedInDialogue), 안 내고 방을
    // 넘기면 버려진다(Extracted → Discarded). 실제로 손에서 나가는 순간은
    // UsedInDialogue·Discarded 둘뿐이다.
    //
    // Discarded는 플레이어가 스스로 놓은 선택의 결과다 — 무언가를 얻은 대가가
    // 아니다. 단서 상태가 런 전체에 걸쳐 누적되면서 생긴 단계다: 버리지 않으면
    // 손이 영영 안 빈다.
    public enum ClueState
    {
        Available,
        Collected,
        UsedInDialogue,
        Extracted,
        Discarded
    }
}
