using GameName.Core.Clues;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    // ClueTag의 Axis는 소유 관계 메타데이터일 뿐 정체성이 아니다 — 같은 말을
    // 가리키면 축이 달라도 같은 태그로 취급되어야 정답 판정의 교집합이 성립한다.
    public class ClueTagTests
    {
        [Test]
        public void 축을_안_주면_중심축이다()
        {
            Assert.AreEqual(ClueTagAxis.Center, new ClueTag("room1.blanket").Axis);
        }

        [Test]
        public void 팩토리가_축을_실어_준다()
        {
            Assert.AreEqual(ClueTagAxis.Center, ClueTag.Center("x").Axis);
            Assert.AreEqual(ClueTagAxis.Sub, ClueTag.Sub("x").Axis);
        }

        [Test]
        public void 같은_말이면_축이_달라도_동치다()
        {
            var center = ClueTag.Center("place.rooftop");
            var sub = ClueTag.Sub("place.rooftop");

            Assert.IsTrue(center.Equals(sub));
            Assert.IsTrue(center == sub);
            Assert.AreEqual(center.GetHashCode(), sub.GetHashCode());
        }

        [Test]
        public void 다른_말이면_다른_태그다()
        {
            Assert.IsTrue(ClueTag.Center("a") != ClueTag.Center("b"));
        }
    }
}
