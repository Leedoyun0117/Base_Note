using System.Collections.Generic;
using GameName.Core.Complexes;

namespace GameName.Core.Events
{
    // 단서 하나를 클릭해 그 서사를 읽고, 그 단서의 태그가 활성 컴플렉스 체인을
    // 통과해 최종 태그가 나왔다는 사실.
    //
    // 이 사건이 3차 개편 감정 루프의 중심이다:
    //   · 해석 로그 UI가 Steps로 "원본 → 각 단계 → 최종"을 그린다.
    //   · 감정 반응 판정이 FinalTags의 Emotion 축만 보고 안정 축을 움직인다
    //     (ClueInterpretationStabilityListener).
    //
    // 이 사건을 내는 처리기(단서 사용 처리기)는 후속 단계에서 붙는다. 지금은
    // 정의와 소비자(안정 축 이동 리스너)만 있다.
    public readonly struct ClueInterpretedEvent
    {
        // 클릭한 단서의 원본 태그.
        public IReadOnlyList<StoryTag> SourceTags { get; }

        // 체인 각 단계의 스냅샷. 활성 컴플렉스가 없으면 빈 목록이다.
        public IReadOnlyList<ComplexChainStep> Steps { get; }

        // 체인을 통과한 최종 태그. 감정 반응이 보는 값.
        public IReadOnlyList<StoryTag> FinalTags { get; }

        public ClueInterpretedEvent(
            IReadOnlyList<StoryTag> sourceTags,
            IReadOnlyList<ComplexChainStep> steps,
            IReadOnlyList<StoryTag> finalTags)
        {
            SourceTags = sourceTags;
            Steps = steps;
            FinalTags = finalTags;
        }

        public static ClueInterpretedEvent From(ComplexChainResult result) =>
            new ClueInterpretedEvent(result.SourceTags, result.Steps, result.FinalTags);
    }
}
