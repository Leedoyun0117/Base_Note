namespace GameName.Core.Clues
{
    // 단서 하나가 지금 어느 단계에 있는지.
    //
    // 이 단계들은 되돌아가지 않는 한 방향 흐름이다. 특히 UsedInDialogue와
    // Extracted를 나눠 두는 이유는 둘이 서로 다른 소모이기 때문이다 — 대사에
    // 꺼내 쓴 단서는 여전히 손에 남지만, 추출한 단서는 색만 남기고 사라진다.
    public enum ClueState
    {
        Available,
        Collected,
        UsedInDialogue,
        Extracted
    }
}
