using System.Collections.Generic;

namespace GameName.Core.Complexes
{
    // 지금 활성인 컴플렉스들을 우선순위 순서대로 읽기만 하는 경계.
    //
    // 체인 적용기(IComplexChainResolver)에 넘길 목록, 화면의 활성 컴플렉스
    // 표시, 해석 로그가 전부 이쪽만 참조한다. 목록을 바꾸는 것은
    // ActiveComplexList 하나뿐이다.
    public interface IActiveComplexListReader
    {
        // 우선순위 오름차순(먼저 적용되는 것이 앞). 동률이면 활성화된 순서.
        IReadOnlyList<ActiveComplex> InPriorityOrder { get; }

        // 지금 담긴 컴플렉스 정의만 우선순위 순서로 — 체인 적용기에 그대로 넘긴다.
        IReadOnlyList<ComplexDefinition> DefinitionsInPriorityOrder { get; }

        int Count { get; }
        int MaxConcurrent { get; }
    }
}
