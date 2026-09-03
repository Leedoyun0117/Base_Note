using GameName.Core.Memories;

namespace GameName.Core.Dialogue
{
    // 검열 키 하나가 어느 기억색으로 풀리는지 답하는 경계.
    //
    // 이 대응은 저작 원문의 토큰([[색:키:말]])에 적혀 있고, 그것을 훑는 일은
    // 1단계 저작 계층의 몫이다. 해금 처리기가 그 훑기 도구(CensorTokenIndex 등)에
    // 직접 묶이지 않도록 이 얇은 경계 뒤로 숨긴다 — 처리기가 아는 것은 "키를
    // 주면 색이 나온다"뿐이다.
    //
    // 같은 키가 서로 다른 색으로 적히는 어긋남은 1단계 CensorKeyColorConsistencyRule이
    // 저작 시점에 막으므로, 여기서는 키 하나에 색 하나가 성립한다고 본다.
    public interface ICensorKeyColorMap
    {
        bool TryGetColor(CensorKey key, out MemoryColor color);
    }
}
