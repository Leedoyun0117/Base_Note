using System;
using System.Collections.Generic;

namespace GameName.Core.Complexes
{
    // 컴플렉스 체인에서 컴플렉스 하나가 적용된 결과의 스냅샷.
    //
    // 해석 로그 UI가 "단서 원본 태그 → 각 단계 → 최종 태그"를 순서대로
    // 보여주려면, 적용기가 중간 결과를 버리지 않고 단계별로 남겨야 한다. 이
    // 타입이 그 한 단계다 — 체인 전체는 ComplexChainResult.Steps로 이어진다.
    public sealed class ComplexChainStep
    {
        public ComplexId ComplexId { get; }
        public ComplexKind Kind { get; }

        // 이 컴플렉스가 손대기 전의 태그 집합.
        public IReadOnlyList<StoryTag> TagsBefore { get; }

        // 이 컴플렉스를 적용한 뒤의 태그 집합. 다음 단계의 TagsBefore와 같다.
        public IReadOnlyList<StoryTag> TagsAfter { get; }

        public ComplexChainStep(
            ComplexId complexId,
            ComplexKind kind,
            IReadOnlyList<StoryTag> tagsBefore,
            IReadOnlyList<StoryTag> tagsAfter)
        {
            ComplexId = complexId;
            Kind = kind;
            TagsBefore = tagsBefore ?? throw new ArgumentNullException(nameof(tagsBefore));
            TagsAfter = tagsAfter ?? throw new ArgumentNullException(nameof(tagsAfter));
        }
    }
}
