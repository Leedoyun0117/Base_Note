namespace GameName.Core.Events
{
    // 라운드 안에서 턴이 하나 진행됐다는 사실.
    //
    // 컴플렉스 지속 턴 감소(ActiveComplexList), 컴플렉스 발생 굴림
    // (ComplexSpawnListener) 등 "턴이 흘렀다"에 반응하는 쪽이 전부 이 사건을
    // 듣는다. 무엇이 턴을 넘기는가(단서 사용? 명시적 턴 종료?)는 TurnCoordinator를
    // 부르는 상위 계층의 결정이고, 이 사건은 그 결과만 알린다.
    public readonly struct TurnAdvancedEvent
    {
        // 이번에 진행된 뒤의 턴 번호(1부터).
        public int Turn { get; }

        // 이 라운드에서 버텨야 하는 총 턴 수.
        public int TurnsToSurvive { get; }

        public TurnAdvancedEvent(int turn, int turnsToSurvive)
        {
            Turn = turn;
            TurnsToSurvive = turnsToSurvive;
        }
    }
}
