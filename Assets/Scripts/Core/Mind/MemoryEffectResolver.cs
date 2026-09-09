using System;
using System.Collections.Generic;
using GameName.Core.Clues;

namespace GameName.Core.Mind
{
    // IMemoryEffectResolver 기본 구현. 태그만 보는 raw 등급(ITagMatchGrader)을
    // 얻은 뒤, 심리 상태 × 안정 축으로 그 등급을 밀거나 뒤집는다.
    //
    // 왜곡의 크기는 안정 축이 중심에서 얼마나 벗어났는지로 정한다: 자유 폭
    // 이내면 왜곡이 없고(등급이 그대로 나간다), 넘어설수록 한 칸, 두 칸까지
    // 움직인다. 방향은 심리 상태가 정한다:
    //   · 낙관  — 유리한 쪽으로(등급 올림)
    //   · 우울  — 가라앉은 쪽으로(등급 내림)
    //   · 광기  — 뒤집어(완전적합 ↔ 무관, 높음 ↔ 낮음, 부분은 그대로)
    //
    // 자유 폭·칸 폭은 RunDefinition 데이터다. 심리 상태별 방향 규칙은 데이터가
    // 아니라 여기 코드로 둔다 — 상태가 늘거나 규칙이 바뀌면 화면 표현도 함께
    // 바뀌므로 밸런싱 수치처럼 흘려보낼 값이 아니다.
    public sealed class MemoryEffectResolver : IMemoryEffectResolver
    {
        private const int MaxSteps = 2;

        private readonly ITagMatchGrader _grader;
        private readonly int _freeBand;
        private readonly int _step;

        public MemoryEffectResolver(ITagMatchGrader grader, int distortionFreeBand, int distortionStep)
        {
            _grader = grader ?? throw new ArgumentNullException(nameof(grader));
            if (distortionFreeBand < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(distortionFreeBand), distortionFreeBand, "자유 폭은 음수일 수 없다.");
            if (distortionStep < 1)
                throw new ArgumentOutOfRangeException(
                    nameof(distortionStep), distortionStep, "칸 폭은 1 이상이어야 한다.");

            _freeBand = distortionFreeBand;
            _step = distortionStep;
        }

        public MatchGrade Resolve(
            PsychologyState psychology,
            int stabilityPosition,
            IReadOnlyList<ClueTag> questionTags,
            IReadOnlyList<ClueTag> answerTags)
        {
            var raw = _grader.Grade(questionTags, answerTags);

            var over = Math.Abs(stabilityPosition) - _freeBand;
            var steps = over <= 0 ? 0 : Math.Min(MaxSteps, (over + _step / 2) / _step);
            if (steps == 0)
                return raw;

            switch (psychology)
            {
                case PsychologyState.Optimism:
                    return Shift(raw, steps);
                case PsychologyState.Melancholy:
                    return Shift(raw, -steps);
                case PsychologyState.Mania:
                    return (MatchGrade)((int)MatchGrade.Exact - (int)raw);
                default:
                    return raw;
            }
        }

        private static MatchGrade Shift(MatchGrade grade, int delta)
        {
            var next = (int)grade + delta;
            if (next < (int)MatchGrade.None) next = (int)MatchGrade.None;
            else if (next > (int)MatchGrade.Exact) next = (int)MatchGrade.Exact;
            return (MatchGrade)next;
        }
    }
}
