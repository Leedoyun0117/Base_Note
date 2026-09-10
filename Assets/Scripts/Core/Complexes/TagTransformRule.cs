using System;

namespace GameName.Core.Complexes
{
    // 컴플렉스 하나 안의 규칙 한 줄. "이런 태그가 걸리면(Match) 이렇게 한다(Kind,
    // Result)".
    //
    // 조건과 결과만 담는 순수 데이터다 — 실제로 태그 집합에 적용하는 것은
    // ComplexChainResolver의 몫이다(ChoiceCondition이 IsSatisfied를 들고 있지
    // 않던 것과 같은 이유: 데이터가 실행 상태에 묶이면 저작 시점 검증이 게임을
    // 켜야 한다).
    //
    // Result가 필요한 종류: Transform(무엇으로 바꿀지), Add·Amplify(무엇을
    // 더할지). Remove·Reject는 Result가 없다(null).
    public sealed class TagTransformRule
    {
        public TagPattern Match { get; }
        public ComplexKind Kind { get; }
        public StoryTag? Result { get; }

        public TagTransformRule(TagPattern match, ComplexKind kind, StoryTag? result = null)
        {
            Match = match;
            Kind = kind;
            Result = result;

            var needsResult = kind == ComplexKind.Transform
                || kind == ComplexKind.Add
                || kind == ComplexKind.Amplify;
            if (needsResult && result == null)
                throw new ArgumentException($"{kind} 규칙에는 결과 태그가 있어야 한다.", nameof(result));
        }

        public static TagTransformRule Transform(TagPattern match, StoryTag result) =>
            new TagTransformRule(match, ComplexKind.Transform, result);

        public static TagTransformRule Remove(TagPattern match) =>
            new TagTransformRule(match, ComplexKind.Remove);

        public static TagTransformRule Add(TagPattern guard, StoryTag result) =>
            new TagTransformRule(guard, ComplexKind.Add, result);

        public static TagTransformRule Amplify(TagPattern match, StoryTag result) =>
            new TagTransformRule(match, ComplexKind.Amplify, result);

        public static TagTransformRule Reject(TagPattern match) =>
            new TagTransformRule(match, ComplexKind.Reject);
    }
}
