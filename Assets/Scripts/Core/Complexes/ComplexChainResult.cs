using System;
using System.Collections.Generic;

namespace GameName.Core.Complexes
{
    // 컴플렉스 체인을 통과시킨 결과 — 최종 태그와, 거기까지의 단계별 스냅샷.
    //
    // FinalTags는 감정 반응 판정(→ 안정 축 이동)이 실제로 보는 값이다. 원본
    // 단서 태그가 아니라 체인을 통과한 이 값이 기준이라는 것이 이번 개편의
    // 핵심이다.
    //
    // Steps는 해석 로그 UI 전용이다 — 게임 규칙은 FinalTags만 보고, 로그가
    // 없어도(활성 컴플렉스 0개) 결과는 성립한다(Steps가 빈 목록).
    public sealed class ComplexChainResult
    {
        public IReadOnlyList<StoryTag> SourceTags { get; }
        public IReadOnlyList<StoryTag> FinalTags { get; }
        public IReadOnlyList<ComplexChainStep> Steps { get; }

        public ComplexChainResult(
            IReadOnlyList<StoryTag> sourceTags,
            IReadOnlyList<StoryTag> finalTags,
            IReadOnlyList<ComplexChainStep> steps)
        {
            SourceTags = sourceTags ?? throw new ArgumentNullException(nameof(sourceTags));
            FinalTags = finalTags ?? throw new ArgumentNullException(nameof(finalTags));
            Steps = steps ?? throw new ArgumentNullException(nameof(steps));
        }
    }
}
