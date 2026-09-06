namespace GameName.Core.Events
{
    // 히로민 보유량이 바뀌었다는 사실. 대화 1회(+3), 기억 추출(-9), 다음
    // 기억으로 이동(-15, 부족하면 가진 만큼)까지 세 계기 모두 이 사건 하나로
    // 화면에 알린다 — 계기마다 다른 사건을 만들면 화면이 셋을 따로 구독해야
    // 한다.
    public readonly struct HiromiChangedEvent
    {
        public int Previous { get; }
        public int Current { get; }

        public HiromiChangedEvent(int previous, int current)
        {
            Previous = previous;
            Current = current;
        }
    }
}
