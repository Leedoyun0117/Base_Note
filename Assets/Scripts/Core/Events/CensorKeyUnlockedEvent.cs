using GameName.Core.Dialogue;
using GameName.Core.Memories;

namespace GameName.Core.Events
{
    // 검열 하나가 풀렸다는 사실. 그 키로 가려져 있던 구간은 이 줄에 있든 다른
    // 줄에 있든 이제 전부 원문으로 돌아온다.
    //
    // 이미 풀려 있던 키를 다시 풀려 했을 때는 발행되지 않는다 — 그때는 자원도
    // 쓰이지 않고 상태도 그대로라, 알릴 "변화"가 없다.
    //
    // 소모된 색을 함께 싣는 이유는 연출과 기록 양쪽에서 "무엇을 대가로 열었는가"가
    // 필요하기 때문이다.
    public readonly struct CensorKeyUnlockedEvent
    {
        public CensorKey Key { get; }
        public MemoryColor Color { get; }

        public CensorKeyUnlockedEvent(CensorKey key, MemoryColor color)
        {
            Key = key;
            Color = color;
        }
    }
}
