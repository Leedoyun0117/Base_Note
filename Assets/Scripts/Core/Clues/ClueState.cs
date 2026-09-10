namespace GameName.Core.Clues
{
    // 단서 하나가 지금 어느 단계에 있는지.
    //
    // 3차 개편에서 흐름이 단순해졌다: 방에 놓여 있고(Available), 클릭해 그 서사를
    // 읽으면 소모된다(Used). 재사용은 없다 — 한 번 읽은 단서는 그 라운드에서
    // 다시 쓸 수 없다.
    //
    // 라운드마다 리셋되지 않는다 — 상태는 ClueStateStore가 RoomStartedEvent로
    // 그 라운드 단서를 새로 Available로 시드하며 관리한다.
    public enum ClueState
    {
        Available,
        Used
    }
}
