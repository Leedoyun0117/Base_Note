using GameName.UI.MemoryRoom.Space;
using NUnit.Framework;
using UnityEngine;

namespace GameName.UI.Tests.EditMode
{
    // 위치 계산만 검증한다. 실제 점 생성·이동(SpriteRenderer·Time)은 Update의 몫이고,
    // 여기서 확인할 것은 SmokeDrift와 같은 원칙 — 시간 항 하나로만 결정되는 순수
    // 함수이며(프레임마다 랜덤 아님), 결과가 픽셀 격자에 스냅되고, 마리마다 궤도가
    // 다르다 — 가 지켜지는가다.
    public class FlyWanderTests
    {
        private const float R = 26f;
        private const float Speed = 0.32f;
        private const float Jitter = 3f;

        [Test]
        public void 같은_시각_같은_마리면_늘_같은_자리다()
        {
            var a = FlyWander.LocalOffset(12.5f, 2, R, Speed, Jitter, 1f);
            var b = FlyWander.LocalOffset(12.5f, 2, R, Speed, Jitter, 1f);
            Assert.AreEqual(a, b);
        }

        [Test]
        public void 시간이_흐르면_자리가_바뀐다()
        {
            var a = FlyWander.LocalOffset(0f, 0, R, Speed, Jitter, 1f);
            var b = FlyWander.LocalOffset(3f, 0, R, Speed, Jitter, 1f);
            Assert.AreNotEqual(a, b);
        }

        [Test]
        public void 마리마다_궤도가_다르다()
        {
            var f0 = FlyWander.LocalOffset(4f, 0, R, Speed, Jitter, 1f);
            var f1 = FlyWander.LocalOffset(4f, 1, R, Speed, Jitter, 1f);
            var f2 = FlyWander.LocalOffset(4f, 2, R, Speed, Jitter, 1f);
            Assert.AreNotEqual(f0, f1);
            Assert.AreNotEqual(f1, f2);
        }

        [Test]
        public void 결과는_픽셀_격자에_스냅된다()
        {
            // 스냅 1px = 유닛으로 0.01. 좌표가 0.01의 배수여야 한다.
            for (var t = 0f; t < 5f; t += 0.37f)
            {
                var p = FlyWander.LocalOffset(t, 3, R, Speed, Jitter, 1f);
                Assert.AreEqual(0f, Mathf.Repeat(p.x * 100f + 0.5f, 1f) - 0.5f, 1e-3f, $"x @ t={t}");
                Assert.AreEqual(0f, Mathf.Repeat(p.y * 100f + 0.5f, 1f) - 0.5f, 1e-3f, $"y @ t={t}");
            }
        }

        [Test]
        public void 스냅_0이면_스냅하지_않는다()
        {
            var snapped = FlyWander.LocalOffset(2.3f, 1, R, Speed, Jitter, 1f);
            var raw = FlyWander.LocalOffset(2.3f, 1, R, Speed, Jitter, 0f);
            // 대부분의 시각에서 원시값은 1px 격자에 딱 안 떨어진다 — 둘이 같으면 스냅이 안 걸린 것.
            Assert.AreNotEqual(snapped, raw);
        }

        [Test]
        public void 반경_안에_머문다()
        {
            // wander(0.7) + orbit(0.45) + jitter 성분을 합쳐도 중심에서 크게 벗어나지 않는다.
            // 대략 (0.7 + 0.45) * R + jitter ≈ 33px → 유닛 0.33. 여유 있게 0.45 유닛으로 잡는다.
            for (var t = 0f; t < 20f; t += 0.5f)
            for (var i = 0; i < 6; i++)
            {
                var p = FlyWander.LocalOffset(t, i, R, Speed, Jitter, 1f);
                Assert.LessOrEqual(p.magnitude, 0.45f, $"fly {i} @ t={t} 벗어남: {p}");
            }
        }
    }
}
