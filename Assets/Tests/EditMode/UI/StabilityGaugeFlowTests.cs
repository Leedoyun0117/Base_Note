using System.Collections.Generic;
using GameName.Core.Events;
using GameName.Core.Mind;
using GameName.UI.MemoryRoom;
using GameName.UI.Session;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameName.UI.Tests.EditMode
{
    // "단서 사용 → 안정 축 이동 → HUD 게이지"가 실제 GameSession 배선에서 끝까지
    // 도는지 붙든다. 조각별 단위 테스트(ClueInterpretationStabilityListenerTests,
    // MemoryRoomHudControllerTests)는 다 통과하는데 게임에선 게이지가 안 움직여
    // 어느 이음매가 끊겼는지 특정하려고 통합으로 건다.
    public class StabilityGaugeFlowTests
    {
        private static GameSession NewSession() => new GameSession(DemoGameData.CreateWorldData(1));

        private static VisualElement MakeHudRoot()
        {
            var root = new VisualElement();
            foreach (var name in new[] { "hud-key-hints", "hud-message", "hud-trust", "hud-stability-value", "hud-chance" })
                root.Add(new Label { name = name });
            root.Add(new VisualElement { name = "hud-stability-fill" });
            root.Add(new VisualElement { name = "hud-stability-baseline" });
            root.Add(new VisualElement { name = "hud-stability-marker" });
            return root;
        }

        [Test]
        public void 단서를_사용하면_안정_축이_움직이고_사건이_난다()
        {
            var session = NewSession();
            var changes = new List<StabilityChangedEvent>();
            session.EventBus.Subscribe<StabilityChangedEvent>(changes.Add);

            var clue = session.ClueTracker.GetCluesInRoom(session.CurrentRoomId)[0];
            var before = session.Stability.Position;

            var result = session.ClueUse.Use(clue.Id);

            Assert.IsTrue(result.Succeeded, "단서 사용 자체가 실패했다.");
            Assert.AreNotEqual(before, session.Stability.Position, "단서를 썼는데 안정 축이 그대로다.");
            Assert.IsNotEmpty(changes, "StabilityChangedEvent가 발행되지 않았다.");
        }

        [Test]
        public void 단서를_여러_개_쓰면_안정_축이_누적으로_움직인다()
        {
            var session = NewSession();
            var clues = session.ClueTracker.GetCluesInRoom(session.CurrentRoomId);

            var positions = new List<int> { session.Stability.Position };
            for (var i = 0; i < 3 && i < clues.Count; i++)
            {
                Assert.IsTrue(session.ClueUse.Use(clues[i].Id).Succeeded);
                positions.Add(session.Stability.Position);
            }

            // 라운드1 시작 컴플렉스(가라앉다)가 감정을 후회로 바꾸므로 매 사용이
            // 축을 침체 쪽으로 민다 — 마지막 위치가 시작보다 확실히 낮아야 한다.
            Assert.Less(positions[positions.Count - 1], positions[0],
                "여러 번 썼는데 누적 이동이 없다: " + string.Join(" → ", positions));
        }

        [Test]
        public void HUD_게이지_채움과_마커가_단서_사용에_따라_갱신된다()
        {
            var session = NewSession();
            var root = MakeHudRoot();
            var view = new MemoryRoomHudView(root);
            // 구독을 살려 둔다.
            var controller = new MemoryRoomHudController(
                view, session.Turns, session.Stability, session.ActiveComplexes, session.EventBus);

            var fill = root.Q<VisualElement>("hud-stability-fill");
            var marker = root.Q<VisualElement>("hud-stability-marker");
            var beforeFillWidth = fill.style.width.value.value;
            var beforeMarkerLeft = marker.style.left.value.value;

            var clue = session.ClueTracker.GetCluesInRoom(session.CurrentRoomId)[0];
            Assert.IsTrue(session.ClueUse.Use(clue.Id).Succeeded);

            var afterFillWidth = fill.style.width.value.value;
            var afterMarkerLeft = marker.style.left.value.value;

            Assert.IsFalse(
                Mathf.Approximately(beforeFillWidth, afterFillWidth),
                $"게이지 채움이 그대로다: width {beforeFillWidth}→{afterFillWidth}");
            Assert.IsFalse(
                Mathf.Approximately(beforeMarkerLeft, afterMarkerLeft),
                $"게이지 마커가 그대로다: left {beforeMarkerLeft}→{afterMarkerLeft}");

            controller.Dispose();
        }
    }
}
