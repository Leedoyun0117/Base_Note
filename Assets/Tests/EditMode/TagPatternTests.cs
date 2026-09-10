using GameName.Core.Complexes;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    // 태그 패턴은 축이 맞아야 걸리고, 값을 비우면 그 축 전체 와일드카드다.
    public class TagPatternTests
    {
        [Test]
        public void 정확한_값_패턴은_같은_축_같은_값에만_걸린다()
        {
            var pattern = new TagPattern(StoryTagAxis.Emotion, "그리움");

            Assert.IsTrue(pattern.Matches(StoryTag.Emotion("그리움")));
            Assert.IsFalse(pattern.Matches(StoryTag.Emotion("분노")));
            Assert.IsFalse(pattern.Matches(StoryTag.Person("그리움")));
        }

        [Test]
        public void 값을_비우면_그_축의_모든_값에_걸린다()
        {
            var pattern = TagPattern.AnyOf(StoryTagAxis.Person);

            Assert.IsTrue(pattern.IsWildcard);
            Assert.IsTrue(pattern.Matches(StoryTag.Person("유키")));
            Assert.IsTrue(pattern.Matches(StoryTag.Person("나츠")));
            Assert.IsFalse(pattern.Matches(StoryTag.Emotion("유키")));
        }

        [Test]
        public void 공백만_있는_값은_와일드카드로_친다()
        {
            Assert.IsTrue(new TagPattern(StoryTagAxis.Time, "   ").IsWildcard);
        }
    }
}
