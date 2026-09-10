using GameName.UI.MemoryRoom.Space;
using NUnit.Framework;

namespace GameName.UI.Tests.EditMode
{
    // 바늘 각도 계산만 검증한다. 실제 회전(Transform·Time)은 Update의 몫이고,
    // 여기서 확인할 것은 흐른 시간이 스냅된 시계방향 각도로 옳게 바뀌는가다.
    public class ClockHandsTests
    {
        private const float Tolerance = 1e-3f;

        [Test]
        public void 시작은_그려진_포즈_그대로다_0도()
        {
            Assert.AreEqual(0f, ClockHands.SnappedAngleDegrees(0f, 60f, 6f), Tolerance);
        }

        [Test]
        public void 한_바퀴_시간이_흐르면_360도()
        {
            Assert.AreEqual(360f, ClockHands.SnappedAngleDegrees(60f, 60f, 6f), Tolerance);
        }

        [Test]
        public void 각도는_스냅_단위로_떨어진다()
        {
            // 60초/바퀴에서 7초 = 42도. 6도 배수라 그대로.
            Assert.AreEqual(42f, ClockHands.SnappedAngleDegrees(7f, 60f, 6f), Tolerance);

            // 6.5초 = 39도 → 가장 가까운 6도 배수(36 vs 42) = 36.
            Assert.AreEqual(36f, ClockHands.SnappedAngleDegrees(6.5f, 60f, 6f), Tolerance);
        }

        [Test]
        public void 시침은_분침의_12분의_1_속도다()
        {
            // 같은 흐른 시간에 대해, 분침 60초/바퀴와 시침 720초/바퀴를 비교.
            const float elapsed = 120f; // 분침 2바퀴 = 720도
            var minute = ClockHands.SnappedAngleDegrees(elapsed, 60f, 0f);
            var hour = ClockHands.SnappedAngleDegrees(elapsed, 60f * 12f, 0f);

            Assert.AreEqual(720f, minute, Tolerance);
            Assert.AreEqual(60f, hour, Tolerance);
            Assert.AreEqual(minute / 12f, hour, Tolerance);
        }

        [Test]
        public void 스냅_0이하면_스냅하지_않는다()
        {
            Assert.AreEqual(39f, ClockHands.SnappedAngleDegrees(6.5f, 60f, 0f), Tolerance);
        }

        [Test]
        public void 한_바퀴_시간이_0이면_각도_0()
        {
            Assert.AreEqual(0f, ClockHands.SnappedAngleDegrees(10f, 0f, 6f), Tolerance);
        }
    }
}
