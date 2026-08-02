using System.Reflection;
using GameName.UI.Journal;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameName.UI.Tests.EditMode
{
    // 이 파일만 예외적으로 실제 GameObject/UIDocument를 만든다 — 이번에
    // 고친 버그(오버레이 초기 가시성)는 "언제 어떤 Unity 콜백이 불리는가"에
    // 관한 문제라, 그 타이밍을 실제로 재현하지 않고는 순수 C# 헬퍼로 뽑아낼
    // 수 없다. 다른 UI 테스트들이 전부 순수 로직만 검증하는 것과 다른
    // 이유이지, 원칙을 바꾼 것은 아니다.
    //
    // Awake를 SetActive로 자동 유발하지 않고 리플렉션으로 직접 부른다 —
    // 에디트 모드 테스트는 플레이어 루프를 돌리지 않아서, Play 모드였다면
    // Unity가 대신 불러줬을 Awake/OnEnable이 여기서는 저절로 실행되지
    // 않는다. 실제 플레이 진입 시 벌어질 일(Awake가 정확히 한 번 불림)을
    // 여기서 명시적으로 재현하는 것뿐이다.
    public class JournalVisibilityControllerTests
    {
        private static (JournalVisibilityController Controller, UIDocument Document, Object[] ToCleanUp)
            CreateHiddenController()
        {
            var panelSettings = ScriptableObject.CreateInstance<PanelSettings>();

            var documentObject = new GameObject("JournalDocument");
            var document = documentObject.AddComponent<UIDocument>();
            document.panelSettings = panelSettings;

            var controllerObject = new GameObject("JournalVisibility");
            var controller = controllerObject.AddComponent<JournalVisibilityController>();

            typeof(JournalVisibilityController)
                .GetField("_journalDocument", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(controller, document);

            InvokePrivateAwake(controller);

            return (controller, document, new Object[] { documentObject, controllerObject, panelSettings });
        }

        private static void InvokePrivateAwake(JournalVisibilityController controller)
        {
            typeof(JournalVisibilityController)
                .GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(controller, null);
        }

        [Test]
        public void Awake_시점에_기록지가_숨겨진_상태로_시작한다()
        {
            var (_, document, cleanup) = CreateHiddenController();
            try
            {
                Assert.AreEqual(DisplayStyle.None, document.rootVisualElement.style.display.value);
            }
            finally
            {
                foreach (var go in cleanup)
                    Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Initialize를_다시_호출해도_이미_열려_있던_기록지는_닫히지_않는다()
        {
            var (controller, document, cleanup) = CreateHiddenController();
            try
            {
                // Tab으로 연 상황을 흉내낸다 — Update()는 private이라 SetVisible과
                // 같은 결과를 내는 공개 경로(Initialize 재호출)만으로 이 시나리오를
                // 재현할 수는 없으므로, 리플렉션으로 private SetVisible을 직접 호출한다.
                InvokePrivateSetVisible(controller, true);
                Assert.AreEqual(DisplayStyle.Flex, document.rootVisualElement.style.display.value);

                // 화면 전환으로 다른 Bootstrap이 Initialize를 다시 부르는 상황.
                controller.Initialize(null);

                Assert.AreEqual(DisplayStyle.Flex, document.rootVisualElement.style.display.value);
            }
            finally
            {
                foreach (var go in cleanup)
                    Object.DestroyImmediate(go);
            }
        }

        private static void InvokePrivateSetVisible(JournalVisibilityController controller, bool visible)
        {
            typeof(JournalVisibilityController)
                .GetMethod("SetVisible", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(controller, new object[] { visible });
        }
    }
}
