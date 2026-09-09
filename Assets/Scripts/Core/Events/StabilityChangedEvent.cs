namespace GameName.Core.Events
{
    // 안정 축 위치가 움직였다는 사실.
    //
    // 이전값을 함께 싣는 이유는 TrustChangedEvent와 같다 — 듣는 쪽은 대개
    // 현재값이 아니라 변화의 방향과 폭(침체로 갔는지, |위치|가 문턱을 넘었는지)에
    // 관심이 있다.
    public readonly struct StabilityChangedEvent
    {
        public int Previous { get; }
        public int Current { get; }

        public StabilityChangedEvent(int previous, int current)
        {
            Previous = previous;
            Current = current;
        }
    }
}
