namespace GameName.Core.Complexes
{
    // 지금 라운드의 턴 진행 상황을 읽기만 하는 경계.
    //
    // 턴을 넘기는 것은 TurnCoordinator 하나뿐이고, "지금 몇 턴째인가"를 근거로
    // 판단·표시만 하는 쪽(HUD, 컴플렉스 발생 굴림 등)은 전부 이쪽만 참조한다.
    public interface ITurnReader
    {
        // 지금까지 진행된 턴 수(0부터 시작, 첫 AdvanceTurn 후 1).
        int CurrentTurn { get; }

        // 이 라운드에서 버텨야 하는 총 턴 수.
        int TurnsToSurvive { get; }

        // 버텨야 하는 턴 수를 이미 채웠는가.
        bool Survived { get; }
    }
}
