using System;
using System.Collections.Generic;

namespace GameName.Core.Complexes
{
    // IComplexChainResolver 기본 구현. 활성 컴플렉스를 우선순위 순서대로
    // 하나씩 적용하며, 앞 컴플렉스의 출력을 다음 컴플렉스의 입력으로 넘긴다.
    //
    // ── 한 컴플렉스 안의 적용 순서 (잠정) ─────────────────────────────────
    // 규칙을 적은 순서에 결과가 딸려가지 않도록 종류별로 모아서 처리한다:
    //   1. 이 컴플렉스의 거부(Reject) 패턴을 체인 전체에 걸치는 거부 목록에
    //      더한다.
    //   2. 거부 목록·삭제(Remove) 패턴에 걸리는 입력 태그를 걷어낸다.
    //   3. 남은 태그를 변환(Transform)한다 — 걸리는 첫 규칙의 결과로 바꾼다.
    //   4. 추가(Add)·증폭(Amplify): 그 규칙의 조건이 이 컴플렉스의 입력
    //      스냅샷에 걸리면 결과 태그를 덧붙인다.
    //   5. 거부 목록에 걸리는 태그는 결과에도 남지 못한다 — 앞 컴플렉스가
    //      거부한 태그를 뒤 컴플렉스가 다시 만들어도 걷어낸다.
    //   6. 같은 태그는 하나로 — 처음 등장 순서를 지킨다.
    // 이 정밀한 우선순위는 밸런싱하며 바뀔 수 있다. 지금은 "종류가 서로
    // 충돌해도 결정적인 결과가 나온다"만 보장한다.
    public sealed class ComplexChainResolver : IComplexChainResolver
    {
        public ComplexChainResult Resolve(
            IReadOnlyList<StoryTag> sourceTags,
            IReadOnlyList<ComplexDefinition> complexesInPriorityOrder)
        {
            if (sourceTags == null) throw new ArgumentNullException(nameof(sourceTags));
            if (complexesInPriorityOrder == null)
                throw new ArgumentNullException(nameof(complexesInPriorityOrder));

            var source = Dedupe(sourceTags);
            var current = source;
            var steps = new List<ComplexChainStep>(complexesInPriorityOrder.Count);

            // 한 번 거부된 (axis, value)는 이후 어떤 컴플렉스도 되살리지 못한다.
            var rejectedPatterns = new List<TagPattern>();

            foreach (var complex in complexesInPriorityOrder)
            {
                if (complex == null)
                    throw new ArgumentException("활성 컴플렉스 목록에 null이 있다.", nameof(complexesInPriorityOrder));

                var before = current;
                var after = Apply(complex, before, rejectedPatterns);
                steps.Add(new ComplexChainStep(complex.Id, complex.Kind, before, after));
                current = after;
            }

            return new ComplexChainResult(source, current, steps);
        }

        private static IReadOnlyList<StoryTag> Apply(
            ComplexDefinition complex,
            IReadOnlyList<StoryTag> input,
            List<TagPattern> rejectedPatterns)
        {
            var rules = complex.Rules;

            foreach (var rule in rules)
            {
                if (rule.Kind == ComplexKind.Reject && !rejectedPatterns.Contains(rule.Match))
                    rejectedPatterns.Add(rule.Match);
            }

            var result = new List<StoryTag>(input.Count);

            foreach (var tag in input)
            {
                if (IsRejected(rejectedPatterns, tag) || MatchesAny(rules, ComplexKind.Remove, tag))
                    continue;

                var outTag = Transformed(rules, tag);
                if (!IsRejected(rejectedPatterns, outTag))
                    AddUnique(result, outTag);
            }

            foreach (var rule in rules)
            {
                if (rule.Kind != ComplexKind.Add && rule.Kind != ComplexKind.Amplify)
                    continue;

                if (!GuardMatches(rule.Match, input))
                    continue;

                var added = rule.Result.Value;
                if (!IsRejected(rejectedPatterns, added))
                    AddUnique(result, added);
            }

            return result;
        }

        // 걸리는 첫 변환 규칙의 결과로 바꾼다. 걸리는 규칙이 없으면 원본 그대로.
        private static StoryTag Transformed(IReadOnlyList<TagTransformRule> rules, StoryTag tag)
        {
            foreach (var rule in rules)
            {
                if (rule.Kind == ComplexKind.Transform && rule.Match.Matches(tag))
                    return rule.Result.Value;
            }

            return tag;
        }

        private static bool MatchesAny(
            IReadOnlyList<TagTransformRule> rules, ComplexKind kind, StoryTag tag)
        {
            foreach (var rule in rules)
            {
                if (rule.Kind == kind && rule.Match.Matches(tag))
                    return true;
            }

            return false;
        }

        private static bool IsRejected(List<TagPattern> rejectedPatterns, StoryTag tag)
        {
            foreach (var pattern in rejectedPatterns)
            {
                if (pattern.Matches(tag))
                    return true;
            }

            return false;
        }

        private static bool GuardMatches(TagPattern guard, IReadOnlyList<StoryTag> input)
        {
            foreach (var tag in input)
            {
                if (guard.Matches(tag))
                    return true;
            }

            return false;
        }

        private static void AddUnique(List<StoryTag> into, StoryTag tag)
        {
            if (!into.Contains(tag))
                into.Add(tag);
        }

        private static IReadOnlyList<StoryTag> Dedupe(IReadOnlyList<StoryTag> tags)
        {
            var result = new List<StoryTag>(tags.Count);
            foreach (var tag in tags)
                AddUnique(result, tag);

            return result;
        }
    }
}
