using System.Collections.Generic;
using GameName.Core.Complexes;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    // 체인 적용기는 순수 함수다 — 활성 컴플렉스를 우선순위 순서대로 하나씩
    // 적용하고, 앞 출력이 다음 입력이 되며, 단계별 스냅샷을 남긴다.
    public class ComplexChainResolverTests
    {
        private readonly ComplexChainResolver _resolver = new ComplexChainResolver();

        private static ComplexDefinition Complex(
            string id, int priority, ComplexKind kind, params TagTransformRule[] rules) =>
            new ComplexDefinition(new ComplexId(id), priority, durationTurns: 3, kind, rules);

        private static IReadOnlyList<StoryTag> Tags(params StoryTag[] tags) => tags;

        [Test]
        public void 활성_컴플렉스가_없으면_최종_태그는_원본과_같고_단계는_비어_있다()
        {
            var source = Tags(StoryTag.Person("유키"), StoryTag.Emotion("그리움"));

            var result = _resolver.Resolve(source, new List<ComplexDefinition>());

            CollectionAssert.AreEqual(source, result.FinalTags);
            Assert.IsEmpty(result.Steps);
        }

        [Test]
        public void 원본_태그의_중복은_제거된다()
        {
            var result = _resolver.Resolve(
                Tags(StoryTag.Emotion("그리움"), StoryTag.Emotion("그리움")),
                new List<ComplexDefinition>());

            CollectionAssert.AreEqual(Tags(StoryTag.Emotion("그리움")), result.FinalTags);
        }

        [Test]
        public void 변환형은_걸린_태그를_결과_태그로_바꾼다()
        {
            var complex = Complex("c1", 0, ComplexKind.Transform,
                TagTransformRule.Transform(
                    new TagPattern(StoryTagAxis.Emotion, "그리움"), StoryTag.Emotion("분노")));

            var result = _resolver.Resolve(
                Tags(StoryTag.Person("유키"), StoryTag.Emotion("그리움")),
                new[] { complex });

            CollectionAssert.AreEqual(
                Tags(StoryTag.Person("유키"), StoryTag.Emotion("분노")), result.FinalTags);
        }

        [Test]
        public void 삭제형은_걸린_태그를_지운다()
        {
            var complex = Complex("c1", 0, ComplexKind.Remove,
                TagTransformRule.Remove(TagPattern.AnyOf(StoryTagAxis.Time)));

            var result = _resolver.Resolve(
                Tags(StoryTag.Emotion("그리움"), StoryTag.Time("유년기")),
                new[] { complex });

            CollectionAssert.AreEqual(Tags(StoryTag.Emotion("그리움")), result.FinalTags);
        }

        [Test]
        public void 추가형은_조건이_입력에_걸리면_결과_태그를_더한다()
        {
            var complex = Complex("c1", 0, ComplexKind.Add,
                TagTransformRule.Add(
                    new TagPattern(StoryTagAxis.Person, "유키"), StoryTag.Emotion("죄책감")));

            var withGuard = _resolver.Resolve(Tags(StoryTag.Person("유키")), new[] { complex });
            CollectionAssert.Contains(withGuard.FinalTags, StoryTag.Emotion("죄책감"));

            var withoutGuard = _resolver.Resolve(Tags(StoryTag.Person("나츠")), new[] { complex });
            CollectionAssert.DoesNotContain(withoutGuard.FinalTags, StoryTag.Emotion("죄책감"));
        }

        [Test]
        public void 거부형은_태그를_지우고_그_뒤_추가를_막는다()
        {
            var reject = Complex("c1", 0, ComplexKind.Reject,
                TagTransformRule.Reject(new TagPattern(StoryTagAxis.Emotion, "그리움")));
            var addBack = Complex("c2", 1, ComplexKind.Add,
                TagTransformRule.Add(TagPattern.AnyOf(StoryTagAxis.Person), StoryTag.Emotion("그리움")));

            var result = _resolver.Resolve(
                Tags(StoryTag.Person("유키"), StoryTag.Emotion("그리움")),
                new[] { reject, addBack });

            CollectionAssert.DoesNotContain(result.FinalTags, StoryTag.Emotion("그리움"));
        }

        [Test]
        public void 체인은_앞_컴플렉스의_출력을_다음_입력으로_넘긴다()
        {
            var first = Complex("c1", 0, ComplexKind.Transform,
                TagTransformRule.Transform(
                    new TagPattern(StoryTagAxis.Emotion, "그리움"), StoryTag.Emotion("분노")));
            var second = Complex("c2", 1, ComplexKind.Transform,
                TagTransformRule.Transform(
                    new TagPattern(StoryTagAxis.Emotion, "분노"), StoryTag.Emotion("체념")));

            var result = _resolver.Resolve(Tags(StoryTag.Emotion("그리움")), new[] { first, second });

            CollectionAssert.AreEqual(Tags(StoryTag.Emotion("체념")), result.FinalTags);
            Assert.AreEqual(2, result.Steps.Count);
            CollectionAssert.AreEqual(Tags(StoryTag.Emotion("그리움")), result.Steps[0].TagsBefore);
            CollectionAssert.AreEqual(Tags(StoryTag.Emotion("분노")), result.Steps[0].TagsAfter);
            CollectionAssert.AreEqual(result.Steps[0].TagsAfter, result.Steps[1].TagsBefore);
            CollectionAssert.AreEqual(Tags(StoryTag.Emotion("체념")), result.Steps[1].TagsAfter);
        }
    }
}
