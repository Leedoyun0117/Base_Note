namespace GameName.Core.Clues
{
    // 단서 하나가 지금 어느 단계에 있는지.
    //
    // 이 단계들은 되돌아가지 않는 한 방향 흐름이다. 특히 UsedInDialogue와
    // Extracted를 나눠 두는 이유는 둘이 서로 다른 소모이기 때문이다 — 대사에
    // 꺼내 쓴 단서는 여전히 손에 남지만, 추출한 단서는 색만 남기고 사라진다.
    //
    // Discarded도 종착점이지만 그 둘과 또 다르다 — 플레이어가 스스로 놓은
    // 선택의 결과이지, 무언가를 얻은 대가가 아니다. 단서 상태가 런 전체에
    // 걸쳐 누적되면서 생긴 단계다: 버리지 않으면 손이 영영 안 빈다.
    public enum ClueState
    {
        Available,
        Collected,
        UsedInDialogue,
        Extracted,
        Discarded
    }
}
