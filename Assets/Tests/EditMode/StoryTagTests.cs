using System;
using GameName.Core.Complexes;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    // 서사 태그는 축까지 봐서 같고 다름을 가른다 — 옛 ClueTag가 값으로만
    // 비교하던 것과 다른 점이다.
    public class StoryTagTests
    {
        [Test]
        public void 축과_값이_모두_같아야_같은_태그다()
        {
            Assert.AreEqual(StoryTag.Emotion("그리움"), new StoryTag(StoryTagAxis.Emotion, "그리움"));
        }

        [Test]
        public void 값이_같아도_축이_다르면_다른_태그다()
        {
            Assert.AreNotEqual(StoryTag.Person("여름"), StoryTag.Time("여름"));
        }

        [Test]
        public void 팩토리는_해당_축의_태그를_만든다()
        {
            Assert.AreEqual(StoryTagAxis.Person, StoryTag.Person("유키").Axis);
            Assert.AreEqual(StoryTagAxis.Emotion, StoryTag.Emotion("후회").Axis);
            Assert.AreEqual(StoryTagAxis.Time, StoryTag.Time("유년기").Axis);
        }

        [Test]
        public void 빈_값은_거부한다()
        {
            Assert.Throws<ArgumentException>(() => new StoryTag(StoryTagAxis.Person, " "));
        }
    }
}
