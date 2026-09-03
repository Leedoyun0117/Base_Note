using System.Collections.Generic;

namespace GameName.Core.Dialogue
{
    // ICensorUnlockRecord 기본 구현. 지금까지 풀린 검열 키를 든다.
    //
    // 방이 바뀌어도 리셋되지 않는다 — 앞 방에서 푼 사실은 뒤 방 대사에서도
    // 계속 드러나 있어야 하기 때문이다(같은 키가 여러 방에 걸쳐 언급되는 것이
    // 이 게임의 전제다).
    //
    // 키 단위로만 기록하고 색은 모른다. 무엇을 대가로 풀었는지는 해금 처리기가
    // 알고, 이 로그는 "이 키가 풀렸는가"에만 답한다 — 렌더러와 선택지 필터가
    // 그 한 가지만 묻기 때문이다.
    public sealed class CensorUnlockLog : ICensorUnlockRecord
    {
        private readonly HashSet<CensorKey> _revealed = new HashSet<CensorKey>();

        public bool IsRevealed(CensorKey key) => _revealed.Contains(key);

        public void Record(CensorKey key) => _revealed.Add(key);
    }
}
