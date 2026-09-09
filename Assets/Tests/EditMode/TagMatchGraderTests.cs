using GameName.Core.Clues;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    // 태그 구조만으로 5단계를 가른다 — 중심축 일치 + 질문 곁축 전부 덮음 = 완전적합,
    // 중심축만 = 높음, 축 엇갈림 = 부분, 곁축만 = 낮음, 무겹침 = 무관.
    public class TagMatchGraderTests
    {
        private static readonly TagMatchGrader Grader = new TagMatchGrader();

        private static MatchGrade Grade(ClueTag[] question, ClueTag[] answer) =>
            Grader.Grade(question, answer);

        [Test]
        public void 중심축이_맞고_질문에_곁축이_없으면_완전적합()
        {
            Assert.AreEqual(
                MatchGrade.Exact,
                Grade(
                    new[] { ClueTag.Center("grief") },
                    new[] { ClueTag.Center("grief"), ClueTag.Sub("rooftop") }));
        }

        [Test]
        public void 중심축이_맞고_질문의_곁축을_답이_전부_덮으면_완전적합()
        {
            Assert.AreEqual(
                MatchGrade.Exact,
                Grade(
                    new[] { ClueTag.Center("grief"), ClueTag.Sub("rooftop") },
                    new[] { ClueTag.Center("grief"), ClueTag.Sub("rooftop") }));
        }

        [Test]
        public void 중심축은_맞지만_질문의_곁축을_못_덮으면_높음()
        {
            Assert.AreEqual(
                MatchGrade.High,
                Grade(
                    new[] { ClueTag.Center("grief"), ClueTag.Sub("rooftop") },
                    new[] { ClueTag.Center("grief") }));
        }

        [Test]
        public void 답의_중심축이_질문의_곁축에_걸치면_부분()
        {
            Assert.AreEqual(
                MatchGrade.Partial,
                Grade(
                    new[] { ClueTag.Center("grief"), ClueTag.Sub("rooftop") },
                    new[] { ClueTag.Center("rooftop") }));
        }

        [Test]
        public void 곁축끼리만_겹치면_낮음()
        {
            Assert.AreEqual(
                MatchGrade.Low,
                Grade(
                    new[] { ClueTag.Center("grief"), ClueTag.Sub("rooftop") },
                    new[] { ClueTag.Center("joy"), ClueTag.Sub("rooftop") }));
        }

        [Test]
        public void 겹치는_태그가_없으면_무관()
        {
            Assert.AreEqual(
                MatchGrade.None,
                Grade(new[] { ClueTag.Center("grief") }, new[] { ClueTag.Center("joy") }));
        }

        [Test]
        public void 등급은_나쁜_쪽에서_좋은_쪽_순서다()
        {
            Assert.Less((int)MatchGrade.None, (int)MatchGrade.Low);
            Assert.Less((int)MatchGrade.Low, (int)MatchGrade.Partial);
            Assert.Less((int)MatchGrade.Partial, (int)MatchGrade.High);
            Assert.Less((int)MatchGrade.High, (int)MatchGrade.Exact);
        }
    }
}
