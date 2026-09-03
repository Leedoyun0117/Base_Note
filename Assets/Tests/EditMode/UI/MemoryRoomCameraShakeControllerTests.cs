using GameName.Core.Events;
using GameName.Core.MemoryRooms;
using GameName.Core.Trust;
using GameName.UI.MemoryRoom.Space;
using NUnit.Framework;
using UnityEngine;

namespace GameName.UI.Tests.EditMode
{
    // 카메라 흔들림 컨트롤러는 세기를 하나도 정하지 않는다 — "언제"만 정한다:
    //   · 신뢰가 한 칸 깎이는 순간 → 임펄스 한 번(0이면 강하게).
    //   · 신뢰가 문턱 이하로 내려가 있는 동안 → 지속 떨림을 켠 채로.
    // LateUpdate는 EditMode에서 돌지 않으므로 실제 오프셋이 아니라 그 두 신호가
    // 켜지고 꺼지는지를 본다(CameraShake.ContinuousActive / ImpulseActive).
    public class MemoryRoomCameraShakeControllerTests
    {
        private sealed class Fixture
        {
            public readonly EventBus Bus = new EventBus(new NoOpEventExceptionHandler());
            public readonly TrustGauge Trust;
            public readonly CameraShake Shake;
            public readonly MemoryRoomCameraShakeController Controller;

            public Fixture(int startingTrust = 3, int continuousAtOrBelow = 1)
            {
                Trust = new TrustGauge(startingTrust, Bus);
                Shake = new GameObject("CameraShake").AddComponent<CameraShake>();
                Controller = new MemoryRoomCameraShakeController(
                    Shake, Trust, Bus, continuousAtOrBelow);
            }

            public void Dispose()
            {
                Controller.Dispose();
                Object.DestroyImmediate(Shake.gameObject);
            }
        }

        [Test]
        public void 신뢰가_문턱_위면_지속_떨림은_꺼져_있다()
        {
            var fx = new Fixture(startingTrust: 3, continuousAtOrBelow: 1);
            try
            {
                Assert.IsFalse(fx.Shake.ContinuousActive);
            }
            finally
            {
                fx.Dispose();
            }
        }

        [Test]
        public void 신뢰가_문턱_이하로_내려가면_지속_떨림이_켜진다()
        {
            var fx = new Fixture(startingTrust: 3, continuousAtOrBelow: 1);
            try
            {
                fx.Trust.Decrease(2); // 3 → 1
                Assert.IsTrue(fx.Shake.ContinuousActive);
            }
            finally
            {
                fx.Dispose();
            }
        }

        [Test]
        public void 신뢰가_한_칸_깎이면_임펄스가_발동한다()
        {
            var fx = new Fixture(startingTrust: 3);
            try
            {
                fx.Trust.Decrease(1); // 3 → 2 (아직 문턱 위 — 임펄스만)
                Assert.IsTrue(fx.Shake.ImpulseActive);
                Assert.IsFalse(fx.Shake.ContinuousActive);
            }
            finally
            {
                fx.Dispose();
            }
        }

        [Test]
        public void 방이_다시_시작되면_신뢰_리셋으로_지속_떨림도_꺼진다()
        {
            var fx = new Fixture(startingTrust: 3, continuousAtOrBelow: 1);
            try
            {
                fx.Trust.Decrease(3); // 3 → 0, 지속 떨림 켜짐
                Assert.IsTrue(fx.Shake.ContinuousActive);

                fx.Bus.Publish(new RoomStartedEvent(new MemoryRoomId("room-2"), 1));

                Assert.AreEqual(3, fx.Trust.Current);
                Assert.IsFalse(fx.Shake.ContinuousActive);
            }
            finally
            {
                fx.Dispose();
            }
        }

        [Test]
        public void Dispose하면_지속_떨림을_끈다()
        {
            var fx = new Fixture(startingTrust: 1, continuousAtOrBelow: 1);
            try
            {
                Assert.IsTrue(fx.Shake.ContinuousActive);
                fx.Controller.Dispose();
                Assert.IsFalse(fx.Shake.ContinuousActive);
            }
            finally
            {
                Object.DestroyImmediate(fx.Shake.gameObject);
            }
        }
    }
}
