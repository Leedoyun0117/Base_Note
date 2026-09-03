using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using GameName.Core.Clues;
using GameName.Core.MemoryRooms;
using GameName.UI.MemoryRoom;
using GameName.UI.MemoryRoom.Space;
using GameName.UI.Overlays;
using GameName.UI.Session;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace GameName.Tests.PlayMode
{
    // HUD가 플레이 공간을 가리지 않는지, 그리고 클릭이 씬까지 내려가는지를
    // 실제 화면 좌표로 확인한다.
    //
    // "가려지지 않는다"를 눈으로 보는 대신 UIPointerOcclusion에 묻는 이유:
    // 단서가 보이느냐와 클릭되느냐는 결국 같은 질문(그 지점에 포인터를 받는
    // UI가 있는가)이고, 그 답은 화면 캡처 없이도 정확히 잴 수 있다.
    public class MemoryRoomHudOcclusionTests
    {
        private const string SceneName = "LDY_GameScene";
        private static readonly MemoryRoomId Room1 = new MemoryRoomId("room-1");

        private GameSession _session;

        [UnitySetUp]
        public IEnumerator LoadSceneAndEnterRoom()
        {
            SceneManager.LoadScene(SceneName, LoadSceneMode.Single);
            yield return null;
            yield return null;

            _session = Object.FindFirstObjectByType<GameSessionBootstrap>(FindObjectsInactive.Include).Session;

            Assert.AreEqual(Room1, _session.CurrentRoomId, "첫 방이 room-1이 아니다.");

            // UI가 실제 크기를 잡아야 Pick이 의미 있는 답을 준다.
            yield return null;
            yield return null;
        }

        private static ScenePointerInput PointerInput() =>
            Object.FindFirstObjectByType<ScenePointerInput>(FindObjectsInactive.Include);

        private static OverlayPanelHost Host() =>
            Object.FindFirstObjectByType<OverlayPanelHost>(FindObjectsInactive.Include);

        // 씬 구성 도구가 실제로 이어 준 목록을 그대로 쓴다 — 배선이 빠져 있으면
        // 이 테스트가 먼저 깨진다.
        private static IReadOnlyList<UIDocument> WiredDocuments()
        {
            var field = typeof(ScenePointerInput)
                .GetField("_uiDocuments", BindingFlags.NonPublic | BindingFlags.Instance);
            var documents = (UIDocument[])field.GetValue(PointerInput());

            Assert.IsNotNull(documents, "ScenePointerInput에 UI 문서가 연결되어 있지 않다.");
            Assert.Greater(documents.Length, 0, "ScenePointerInput에 UI 문서가 연결되어 있지 않다.");
            return documents;
        }

        private static MemoryRoomLayout Layout()
        {
            var bootstrap = Object.FindFirstObjectByType<MemoryRoomBootstrap>(FindObjectsInactive.Include);
            var field = typeof(MemoryRoomBootstrap)
                .GetField("_layoutAsset", BindingFlags.NonPublic | BindingFlags.Instance);
            return ((MemoryRoomLayoutAsset)field.GetValue(bootstrap)).ToLayout();
        }

        // 방 안 어느 자리(비율)의 단서가 화면 어디에 오는지.
        private static Vector2 ScreenPositionOf(MemoryRoomLayout layout, float ratio, ClueKind kind)
        {
            var world = CluePlacementLayout.PositionAt(layout, kind, new CluePositionRatio(ratio));
            return Camera.main.WorldToScreenPoint(new Vector3(world.x, world.y, 0f));
        }

        private static bool IsCoveredByUI(Vector2 screenPosition) =>
            UIPointerOcclusion.IsPointerOverPickableElement(WiredDocuments(), screenPosition);

        // 대화 패널은 하단 플레이 영역에 겹치도록 설계됐다(배경은 투명, 선택지
        // 버튼·마스크 구간만 클릭을 받는다). 그래서 "방을 가린다"의 판정에서는
        // 대화 패널이 소유한 요소를 제외하고, HUD 바/안내 같은 껍데기만 본다.
        private static bool CoveredByChrome(Vector2 screenPosition)
        {
            foreach (var document in WiredDocuments())
            {
                if (document == null || !document.isActiveAndEnabled)
                    continue;

                var panel = document.rootVisualElement.panel;
                if (panel == null || document.rootVisualElement.resolvedStyle.display == DisplayStyle.None)
                    continue;

                var picked = panel.Pick(RuntimePanelUtils.ScreenToPanel(panel, screenPosition));
                if (picked == null || picked == document.rootVisualElement || picked is TemplateContainer)
                    continue;

                if (!IsWithin(picked, "dialogue-panel"))
                    return true;
            }

            return false;
        }

        private static bool IsWithin(VisualElement element, string ancestorName)
        {
            for (var current = element; current != null; current = current.parent)
            {
                if (current.name == ancestorName)
                    return true;
            }

            return false;
        }

        [UnityTest]
        public IEnumerator 오버레이가_닫혀_있으면_방_어디를_눌러도_UI가_가로채지_않는다()
        {
            var layout = Layout();

            // 방 폭을 촘촘히 훑는다 — 어느 한 자리라도 막히면 그 자리에 놓인
            // 단서는 영영 집을 수 없다.
            for (var step = 0; step <= 20; step++)
            {
                var ratio = step / 20f;

                foreach (var kind in new[] { ClueKind.Poster, ClueKind.FloorObject })
                {
                    var screenPosition = ScreenPositionOf(layout, ratio, kind);
                    Assert.IsFalse(
                        CoveredByChrome(screenPosition),
                        $"비율 {ratio:0.00}의 {kind} 자리가 HUD 껍데기에 가려져 있다.");
                }
            }

            yield return null;
        }

        // 방 폭 양 끝은 예전 좌우 패널 배치에서 가장 먼저 가려지던 자리다.
        [UnityTest]
        public IEnumerator 방_양_끝에_놓인_단서도_HUD에_가려지지_않는다()
        {
            var layout = Layout();

            foreach (var ratio in new[] { 0f, 0.02f, 0.98f, 1f })
            {
                Assert.IsFalse(
                    CoveredByChrome(ScreenPositionOf(layout, ratio, ClueKind.Poster)),
                    $"왼쪽/오른쪽 끝(비율 {ratio})의 포스터가 가려져 있다.");
                Assert.IsFalse(
                    CoveredByChrome(ScreenPositionOf(layout, ratio, ClueKind.FloorObject)),
                    $"왼쪽/오른쪽 끝(비율 {ratio})의 바닥 물건이 가려져 있다.");
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator 오버레이가_열려_있으면_씬_클릭이_막힌다()
        {
            var layout = Layout();
            var center = ScreenPositionOf(layout, 0.5f, ClueKind.FloorObject);
            Assert.IsFalse(IsCoveredByUI(center), "열기 전에는 통과해야 한다.");
            Assert.IsTrue(PointerInput().InputEnabled, "열기 전에는 씬을 조작할 수 있어야 한다.");

            Host().Show(OverlayPanel.Inventory);
            yield return null;

            // 두 겹 모두 막혀야 한다: 라우터가 알린 상태와, 화면을 덮은 요소.
            Assert.IsFalse(PointerInput().InputEnabled, "오버레이가 떴는데 씬 조작이 열려 있다.");
            Assert.IsTrue(IsCoveredByUI(center), "오버레이가 떴는데 그 아래가 클릭된다.");

            Host().Hide(OverlayPanel.Inventory);
            yield return null;

            Assert.IsTrue(PointerInput().InputEnabled, "오버레이를 닫았는데 씬 조작이 막혀 있다.");
            Assert.IsFalse(IsCoveredByUI(center), "오버레이를 닫았는데 아래가 계속 막혀 있다.");
        }

        [UnityTest]
        public IEnumerator HUD_배경은_클릭을_통과시킨다()
        {
            // 통과 규칙이 "UI를 통째로 무시한다"가 아니라 "조작 대상 위에서만
            // 막는다"임을 확인한다 — 상태 표시줄의 배경 컨테이너는 포인터를
            // 가로채지 않아야 클릭이 씬까지 내려가 단서를 집을 수 있다.
            var hudRoot = Object.FindFirstObjectByType<MemoryRoomBootstrap>(FindObjectsInactive.Include)
                .GetComponent<UIDocument>().rootVisualElement;

            // 대화 패널의 배경·본문 컨테이너도 포인터를 통과시켜야 한다 —
            // 클릭을 받는 것은 그 안의 선택지 버튼과 마스크 구간뿐이다.
            var passthrough = new[]
            {
                "screen", "hud-bar", "hud-messages",
                "dialogue-panel", "dialogue-body", "dialogue-choices",
            };

            foreach (var containerName in passthrough)
            {
                var container = hudRoot.Q<VisualElement>(containerName);
                Assert.IsNotNull(container, $"{containerName}을 찾지 못했다.");
                Assert.AreEqual(
                    PickingMode.Ignore, container.pickingMode,
                    $"{containerName}이 포인터를 가로챈다.");
            }

            yield return null;
        }
    }
}
