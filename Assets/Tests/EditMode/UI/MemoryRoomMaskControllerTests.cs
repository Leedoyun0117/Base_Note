using System.Collections.Generic;
using GameName.Core.Events;
using GameName.Core.Trust;
using GameName.UI.MemoryRoom.Space;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace GameName.UI.Tests.EditMode
{
    // 마스크 컨트롤러는 가시 비율을 직접 계산하지 않는다 — IVisibilityPolicy에
    // 그대로 위임하고, "언제 다시 계산하는가"(방 시작·신뢰 변화)와 "그 결과를
    // 누구에게 전하는가"(마스크 뷰 + VisibleRatioChanged)만 책임진다. 이 위임을
    // 고정하는 것이 이 테스트의 목적이다.
    public class MemoryRoomMaskControllerTests
    {
        // 계단식이라 선형이 아니다 — 컨트롤러가 몰래 보간하지 않는지 함께 잡힌다.
        private static readonly Dictionary<int, float> Table = new Dictionary<int, float>
        {
            { 3, 1.0f }, { 2, 0.75f }, { 1, 0.5f }, { 0, 0.5f },
        };

        // 마스크 뷰는 이제 UI Toolkit 요소다 — 씬 오브젝트 대신 이름 붙은
        // VisualElement 두 장이 있는 root를 손으로 짜서 넘긴다.
        private static VisualElement MakeMaskRoot()
        {
            var root = new VisualElement();
            root.Add(new VisualElement { name = "mask-left" });
            root.Add(new VisualElement { name = "mask-right" });
            return root;
        }

        private sealed class Fixture
        {
            public readonly EventBus Bus = new EventBus(new NoOpEventExceptionHandler());
            public readonly TrustGauge Trust;
            public readonly StepVisibilityPolicy Policy = new StepVisibilityPolicy(Table);
            public readonly MemoryRoomMaskView View;
            public readonly MemoryRoomMaskController Controller;
            public readonly List<float> Forwarded = new List<float>();

            public Fixture(int startingTrust = 3)
            {
                Trust = new TrustGauge(startingTrust, Bus);
                View = new MemoryRoomMaskView(MakeMaskRoot());

                Controller = new MemoryRoomMaskController(View, Policy, Trust, Bus);
                Controller.VisibleRatioChanged += Forwarded.Add;
            }
        }

        [Test]
        public void 생성_직후_현재_신뢰의_정책_결과를_뷰에_적용한다()
        {
            var fx = new Fixture(startingTrust: 3);

            Assert.AreEqual(Table[3], fx.View.LastAppliedRatio);
            Assert.AreEqual(Table[3], fx.Controller.CurrentRatio);
        }

        [Test]
        public void 신뢰_각_단계의_정책_비율이_뷰와_구독자에게_그대로_전달된다()
        {
            var fx = new Fixture(startingTrust: 3);

            fx.Trust.Decrease(1); // 3 → 2
            Assert.AreEqual(Table[2], fx.View.LastAppliedRatio);
            Assert.AreEqual(Table[2], fx.Controller.CurrentRatio);
            Assert.AreEqual(Table[2], fx.Forwarded[fx.Forwarded.Count - 1]);

            fx.Trust.Decrease(1); // 2 → 1
            Assert.AreEqual(Table[1], fx.View.LastAppliedRatio);

            fx.Trust.Decrease(1); // 1 → 0
            Assert.AreEqual(0, fx.Trust.Current);
            Assert.AreEqual(Table[0], fx.View.LastAppliedRatio);

            // 컨트롤러가 넘긴 값은 전부 정책이 낸 값이어야 한다(자체 계산 금지).
            foreach (var forwarded in fx.Forwarded)
                Assert.Contains(forwarded, new[] { 1.0f, 0.75f, 0.5f });
        }

        [Test]
        public void 방이_다시_시작되면_그_방의_신뢰로_비율을_다시_적용한다()
        {
            var fx = new Fixture(startingTrust: 3);

            fx.Trust.Decrease(2); // 3 → 1
            Assert.AreEqual(Table[1], fx.View.LastAppliedRatio);

            fx.Bus.Publish(new RoomStartedEvent(new GameName.Core.MemoryRooms.MemoryRoomId("room-2"), 1));

            // TrustGauge가 시작값으로 리셋 → 정책 비율도 3짜리로 돌아온다.
            Assert.AreEqual(3, fx.Trust.Current);
            Assert.AreEqual(Table[3], fx.View.LastAppliedRatio);
        }
    }
}
