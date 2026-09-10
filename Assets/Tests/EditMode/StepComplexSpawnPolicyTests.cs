using System;
using System.Collections.Generic;
using GameName.Core.Complexes;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    // 발생 확률 정책은 |안정 위치|를 표에서 찾고, 빈칸은 그 이하 가장 큰
    // 키로 내림하며, 부호는 무시한다.
    public class StepComplexSpawnPolicyTests
    {
        private static StepComplexSpawnPolicy Policy() =>
            new StepComplexSpawnPolicy(new Dictionary<int, float>
            {
                { 0, 0.0f },
                { 20, 0.1f },
                { 60, 0.5f },
            });

        [Test]
        public void 표에_있는_값은_그대로_돌려준다()
        {
            Assert.AreEqual(0.1f, Policy().SpawnChance(20));
        }

        [Test]
        public void 침체_쪽_음수도_절댓값으로_조회한다()
        {
            Assert.AreEqual(0.5f, Policy().SpawnChance(-60));
        }

        [Test]
        public void 표의_빈칸은_그_이하_가장_큰_키로_내림한다()
        {
            Assert.AreEqual(0.1f, Policy().SpawnChance(35));
        }

        [Test]
        public void 표의_최대_키를_넘으면_그_값으로_고정한다()
        {
            Assert.AreEqual(0.5f, Policy().SpawnChance(100));
        }

        [Test]
        public void 확률은_0에서_1_사이로_잘린다()
        {
            var policy = new StepComplexSpawnPolicy(new Dictionary<int, float> { { 0, 5f } });
            Assert.AreEqual(1f, policy.SpawnChance(0));
        }

        [Test]
        public void 빈_표는_거부한다()
        {
            Assert.Throws<ArgumentException>(
                () => new StepComplexSpawnPolicy(new Dictionary<int, float>()));
        }
    }
}
