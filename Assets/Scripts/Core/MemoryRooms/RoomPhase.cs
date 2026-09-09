namespace GameName.Core.MemoryRooms
{
    // 방 하나 안에서 지금 어느 국면인지.
    //
    // 한 방은 두 국면으로 갈린다: 먼저 기억 방을 조사해 단서를 모으고
    // (Investigation), 그다음 유키와 마주 앉아 그 단서로 답한다(Dialogue).
    // 이 둘을 나눠 두는 이유는 조사에 횟수 제한을 걸고(방당 N회) 대화는 그
    // 뒤에만 시작되게 하기 위해서다 — 그 제한 자체는 이 개편의 다음 단계에서
    // 붙고, 여기서는 국면만 가른다.
    public enum RoomPhase
    {
        Investigation,
        Dialogue
    }
}
