namespace GameName.Core.Events
{
    // 신뢰도가 움직였다는 사실.
    //
    // 이전값을 함께 싣는 이유: 듣는 쪽이 관심 있는 것은 대개 현재값이 아니라
    // 변화의 방향과 폭이다(올라갔는지, 특정 문턱을 넘었는지). 이전값이 없으면
    // 구독자마다 직전 값을 따로 들고 있어야 한다.
    public readonly struct TrustChangedEvent
    {
        public int Previous { get; }
        public int Current { get; }

        public TrustChangedEvent(int previous, int current)
        {
            Previous = previous;
            Current = current;
        }
    }
}
