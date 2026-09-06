using System.Collections.Generic;
using GameName.Core.Clues;

namespace GameName.Core.Dialogue
{
    // 검열 키 하나를 풀려면 제시할 기억이 어느 태그를 가져야 하는지 답하는 경계.
    //
    // 이 대응은 저작 데이터(CensorKeyTagRequirement)에 그대로 적혀 있고, 해금
    // 처리기가 그 저작 목록을 직접 훑지 않도록 얇은 경계 뒤로 숨긴다 — 처리기가
    // 아는 것은 "키를 주면 필요한 태그 목록이 나온다"뿐이다. ICensorKeyColorMap과
    // 나란한 구조이지만 둘은 서로 다른 사실에 답한다: 색 맵은 "이 키가 어느
    // 색 힌트로 보이는가", 이쪽은 "실제로 무엇을 제시해야 풀리는가"다.
    public interface ICensorKeyRequiredTags
    {
        bool TryGetRequiredTags(CensorKey key, out IReadOnlyList<ClueTag> requiredTags);
    }
}
