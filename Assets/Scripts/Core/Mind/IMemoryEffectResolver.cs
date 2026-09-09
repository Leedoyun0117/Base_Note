using System.Collections.Generic;
using GameName.Core.Clues;

namespace GameName.Core.Mind
{
    // 답으로 낸 단서가 실제로 어떤 등급으로 읽히는지 매기는 경계.
    //
    // 태그 구조만 보는 ITagMatchGrader 위에 심리 상태 × 안정 축을 얹는다 —
    // 같은 답이라도 나츠가 어떤 상태로, 얼마나 흔들린 채로 그 기억을 마주하느냐에
    // 따라 유리하게·가라앉게·뒤집어 읽힌다. DialogueProgressor는 이 결과로
    // 분기를 정하고 사건에 싣는다.
    public interface IMemoryEffectResolver
    {
        MatchGrade Resolve(
            PsychologyState psychology,
            int stabilityPosition,
            IReadOnlyList<ClueTag> questionTags,
            IReadOnlyList<ClueTag> answerTags);
    }
}
