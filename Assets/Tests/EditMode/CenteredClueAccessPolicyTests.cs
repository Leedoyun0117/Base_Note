using GameName.Core.Clues;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    // 방 가운데를 기준으로 좌우 대칭, 경계에 걸친 자리는 포함.
    public class CenteredClueAccessPolicyTests
    {
        private static readonly CenteredClueAccessPolicy Policy = new CenteredClueAccessPolicy();

        private static bool Accessible(float position, float visibleRatio) =>
            Policy.IsAccessible(new CluePositionRatio(position), visibleRatio);

        [Test]
        public void 가시_비율_1이면_방_전체가_접근_가능하다()
        {
            Assert.IsTrue(Accessible(0.0f, 1.0f));
            Assert.IsTrue(Accessible(0.5f, 1.0f));
            Assert.IsTrue(Accessible(1.0f, 1.0f));
        }

        [Test]
        public void 가시_비율_0_5면_0_25에서_0_75_구간만_접근_가능하다()
        {
            Assert.IsTrue(Accessible(0.5f, 0.5f));
            Assert.IsFalse(Accessible(0.2f, 0.5f));
            Assert.IsFalse(Accessible(0.8f, 0.5f));
        }

        [Test]
        public void 정확히_경계에_걸친_자리는_접근_가능하다()
        {
            // v=0.5 → 구간 [0.25, 0.75]. 두 끝값 다 포함되어야 한다.
            Assert.IsTrue(Accessible(0.25f, 0.5f));
            Assert.IsTrue(Accessible(0.75f, 0.5f));
        }

        [Test]
        public void 가시_비율_0_75면_경계는_0_125와_0_875다()
        {
            Assert.IsTrue(Accessible(0.125f, 0.75f));
            Assert.IsTrue(Accessible(0.875f, 0.75f));
            Assert.IsFalse(Accessible(0.1f, 0.75f));
            Assert.IsFalse(Accessible(0.9f, 0.75f));
        }

        [Test]
        public void 가시_비율_0이면_어떤_자리도_접근_불가다()
        {
            Assert.IsFalse(Accessible(0.5f, 0f));
        }
    }
}
