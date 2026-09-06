namespace GameName.Core.Events
{
    // 기회가 하나 줄었다는 사실. 0에 닿으면 런이 끝난다 — 그 판정은 이 사건을
    // 듣는 별도 리스너의 몫이다(ChanceTracker 자신은 셈만 한다).
    public readonly struct ChanceChangedEvent
    {
        public int Current { get; }

        public ChanceChangedEvent(int current)
        {
            Current = current;
        }
    }
}
