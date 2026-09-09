using System.Collections.Generic;

namespace GameName.Core.Clues
{
    // 질문 태그와 답 태그의 적합도를 등급으로 매기는 판정기.
    //
    // 인터페이스로 두는 이유: 심리 상태·안정 축 조합이 등급을 뒤집는 규칙([10])이
    // 이 판정을 감싸 다시 매길 수 있어야 한다. 지금 구현(TagMatchGrader)은 태그
    // 구조만 보는 밑바탕이다.
    public interface ITagMatchGrader
    {
        MatchGrade Grade(IReadOnlyList<ClueTag> questionTags, IReadOnlyList<ClueTag> answerTags);
    }
}
