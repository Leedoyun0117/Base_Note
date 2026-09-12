using System.Collections;
using GameName.UI.MemoryRoom;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace GameName.Tests.PlayMode
{
    // 플레이 중에는 안정 축 게이지(채움+마커)가 목표치로 뚝 끊기지 않고
    // DOTween으로 0.35초에 걸쳐 미끄러진다. EditMode(플레이 아님)에서는 즉시
    // 반영되므로 "부드럽게 움직인다"는 플레이 모드에서만 확인할 수 있다.
    public class StabilityGaugeAnimationTests
    {
        private static VisualElement MakeHudRoot()
        {
            var root = new VisualElement();
            foreach (var name in new[]
                { "hud-key-hints", "hud-message", "hud-trust", "hud-stability-value", "hud-chance" })
                root.Add(new Label { name = name });

            root.Add(new VisualElement { name = "hud-stability-fill" });
            root.Add(new VisualElement { name = "hud-stability-baseline" });
            root.Add(new VisualElement { name = "hud-stability-marker" });
            return root;
        }

        private static float FillWidthPercent(VisualElement root) =>
            root.Q<VisualElement>("hud-stability-fill").style.width.value.value;

        private static float MarkerLeftPercent(VisualElement root) =>
            root.Q<VisualElement>("hud-stability-marker").style.left.value.value;

        [UnityTest]
        public IEnumerator 채움과_마커가_한_프레임에_목표로_튀지_않고_트윈_시간에_걸쳐_도달한다()
        {
            var root = MakeHudRoot();
            var view = new MemoryRoomHudView(root);

            view.SetStability(0, -100, 100);    // 시작: 중앙, 채움 폭 0, 마커도 중앙
            view.SetStability(-40, -100, 100);  // 목표: -100..100에서 -40 = 채움 폭 20%, 마커 30%

            // SetStability가 돌아온 직후 — 트윈은 아직 한 번도 안 굴렀으니
            // style은 여전히 시작값이어야 한다. 즉시 목표를 써버리면 실패.
            Assert.AreEqual(0f, FillWidthPercent(root), 0.01f,
                "SetStability가 채움을 즉시 목표로 옮겼다 — 트윈이 안 걸렸다.");
            Assert.AreEqual(50f, MarkerLeftPercent(root), 0.01f,
                "SetStability가 마커를 즉시 목표로 옮겼다 — 트윈이 안 걸렸다.");

            // 트윈이 도는 중 — 목표에 아직 못 미치지만 시작값에서는 벗어났다.
            yield return new WaitForSeconds(0.12f);
            var midWidth = FillWidthPercent(root);
            var midMarkerLeft = MarkerLeftPercent(root);
            Assert.Greater(midWidth, 0f, "채움이 전혀 안 움직였다.");
            Assert.Less(midWidth, 20f, "채움이 목표로 즉시 튀었다.");
            Assert.Less(midMarkerLeft, 50f, "마커가 전혀 안 움직였다.");
            Assert.Greater(midMarkerLeft, 30f, "마커가 목표로 즉시 튀었다.");

            // 트윈 시간(0.35s)이 충분히 지나면 목표에 도달한다.
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(20f, FillWidthPercent(root), 0.5f, "트윈이 끝났는데 채움이 목표 폭이 아니다.");
            Assert.AreEqual(30f, MarkerLeftPercent(root), 0.5f, "트윈이 끝났는데 마커가 목표 자리가 아니다.");

            view.CancelStabilityAnimation();
        }

        [UnityTest]
        public IEnumerator 연속으로_값이_바뀌면_마지막_목표로_수렴한다()
        {
            var root = MakeHudRoot();
            var view = new MemoryRoomHudView(root);

            view.SetStability(0, -100, 100);
            view.SetStability(-20, -100, 100); // 채움 폭 10%, 마커 40%
            yield return new WaitForSeconds(0.1f);
            view.SetStability(-60, -100, 100); // 채움 폭 30%, 마커 20% — 이전 트윈을 죽이고 여기서 다시
            yield return new WaitForSeconds(0.6f);

            Assert.AreEqual(30f, FillWidthPercent(root), 0.5f,
                "트윈이 겹쳐 채움이 마지막 목표에 수렴하지 못했다.");
            Assert.AreEqual(20f, MarkerLeftPercent(root), 0.5f,
                "트윈이 겹쳐 마커가 마지막 목표에 수렴하지 못했다.");

            view.CancelStabilityAnimation();
        }

        [UnityTest]
        public IEnumerator 색이_흰색에서_목표_방향색으로_부드럽게_진해진다()
        {
            var root = MakeHudRoot();
            var view = new MemoryRoomHudView(root);
            var marker = root.Q<VisualElement>("hud-stability-marker");

            view.SetStability(0, -100, 100);
            yield return new WaitForSeconds(0.5f); // 트윈 종료 대기 — 0은 흰색.

            var whiteColor = marker.style.backgroundColor.value;
            Assert.AreEqual(1f, whiteColor.r, 0.05f, "0(안정)인데 흰색이 아니다.");
            Assert.AreEqual(1f, whiteColor.b, 0.05f, "0(안정)인데 흰색이 아니다.");

            view.SetStability(80, -100, 100); // 거의 완전 빨강으로 이동

            var midColor = marker.style.backgroundColor.value;
            Assert.AreEqual(whiteColor.r, midColor.r, 0.01f,
                "SetStability가 색을 즉시 목표로 옮겼다 — 트윈이 안 걸렸다.");

            yield return new WaitForSeconds(0.5f); // 트윈 종료 대기

            var finalColor = marker.style.backgroundColor.value;
            Assert.Greater(finalColor.r, finalColor.b, "양수 극단에 가까운데 빨강 쪽이 아니다.");
            Assert.Less(finalColor.r + finalColor.g + finalColor.b, whiteColor.r + whiteColor.g + whiteColor.b,
                "양수 극단에 가까워졌는데 흰색보다 진해지지 않았다.");

            view.CancelStabilityAnimation();
        }
    }
}
