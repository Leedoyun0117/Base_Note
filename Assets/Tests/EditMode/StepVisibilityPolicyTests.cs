using System;
using System.Collections.Generic;
using GameName.Core.Trust;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    // 신뢰 → 가시 비율 대응은 주입된 표 그대로여야 하고, 코드에 박혀서는 안 된다.
    public class StepVisibilityPolicyTests
    {
        // 스펙의 데모 표. 1과 0이 같은 값이라는 것이 "선형이 아니다"를 드러낸다.
        private static IReadOnlyDictionary<int, float> DemoTable() => new Dictionary<int, float>
        {
            { 3, 1.0f },
            { 2, 0.75f },
            { 1, 0.5f },
            { 0, 0.5f },
        };

        [Test]
        public void 표에_적힌_대응을_그대로_돌려준다()
        {
            var policy = new StepVisibilityPolicy(DemoTable());

            Assert.AreEqual(1.0f, policy.GetVisibleRatio(3));
            Assert.AreEqual(0.75f, policy.GetVisibleRatio(2));
            Assert.AreEqual(0.5f, policy.GetVisibleRatio(1));
            Assert.AreEqual(0.5f, policy.GetVisibleRatio(0));
        }

        [Test]
        public void 표의_최댓값을_넘는_신뢰도는_최댓값_칸으로_친다()
        {
            var policy = new StepVisibilityPolicy(DemoTable());

            Assert.AreEqual(1.0f, policy.GetVisibleRatio(9));
        }

        [Test]
        public void 표의_최솟값보다_작은_신뢰도는_최솟값_칸으로_친다()
        {
            var policy = new StepVisibilityPolicy(DemoTable());

            Assert.AreEqual(0.5f, policy.GetVisibleRatio(-3));
        }

        [Test]
        public void 표의_빈칸은_그_이하_가장_큰_칸으로_내림한다()
        {
            var policy = new StepVisibilityPolicy(new Dictionary<int, float>
            {
                { 4, 1.0f },
                { 2, 0.6f },
                { 0, 0.2f },
            });

            Assert.AreEqual(0.6f, policy.GetVisibleRatio(3));
            Assert.AreEqual(0.2f, policy.GetVisibleRatio(1));
        }

        [Test]
        public void 빈_표는_거부한다()
        {
            Assert.Throws<ArgumentException>(() => new StepVisibilityPolicy(new Dictionary<int, float>()));
        }
    }
}
