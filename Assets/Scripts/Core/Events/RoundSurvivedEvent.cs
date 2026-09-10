namespace GameName.Core.Events
{
    // 라운드에서 버텨야 하는 턴 수를 다 채웠다는 사실 — 라운드 클리어.
    //
    // 옛 RoomClearedEvent("대화를 다 보고 다음으로 버튼을 눌렀다")를 대체한다.
    // 이제 라운드를 넘기는 계기는 플레이어의 버튼이 아니라 "지정 턴 수를
    // 버텼다"이다. 이 사건을 받아 다음 라운드로 넘기는 것은 진행 코디네이터의
    // 몫이다(후속 단계에서 배선).
    //
    // 지금은 아무 값도 싣지 않는다 — RunCompletedEvent와 같다. 어느 라운드였는지가
    // 필요해지면(결과 화면 등) 그때 채운다.
    public readonly struct RoundSurvivedEvent
    {
    }
}
