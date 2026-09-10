using System.Collections.Generic;

namespace GameName.Core.Complexes
{
    // 단서 원본 태그를 활성 컴플렉스 체인에 통과시켜 최종 태그를 내는 경계.
    //
    // 순수 함수다 — 상태도, 이벤트도, 무작위도 없다. 같은 입력이면 같은 출력이
    // 나오므로 게임을 켜지 않고 단위 테스트로 규칙을 못 박을 수 있다. 옛
    // ITagMatchGrader가 인터페이스였던 것과 같은 목적이지만, 여기서는 왜곡을
    // 얹는 상위 계층이 따로 없다 — 체인 그 자체가 왜곡이다.
    public interface IComplexChainResolver
    {
        ComplexChainResult Resolve(
            IReadOnlyList<StoryTag> sourceTags,
            IReadOnlyList<ComplexDefinition> complexesInPriorityOrder);
    }
}
