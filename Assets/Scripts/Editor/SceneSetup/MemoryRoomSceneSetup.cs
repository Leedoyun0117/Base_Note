using GameName.UI.MemoryRoom;
using GameName.UI.MemoryRoom.Space;
using GameName.UI.Overlays;
using GameName.UI.Session;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GameName.UI.Editor.SceneSetup
{
    // 2D 기억 방을 씬에 한 번에 갖추는 도구.
    //
    // 손으로 하면 열 몇 단계가 되는 배선(오버레이 문서 셋, 방 오브젝트, 카메라,
    // 각 Bootstrap의 참조)을 메뉴 하나로 끝낸다. 이 도구가 만드는 것은 전부
    // 씬과 에셋일 뿐이고, 게임 규칙은 하나도 계산하지 않는다 — 런타임 코드의
    // 어떤 필드도 이 도구를 위해 public으로 열지 않았다(SerializedFieldBinder 참고).
    //
    // 설계 원칙 두 가지:
    //   1) 여러 번 돌려도 안전하다. 이미 있는 것은 다시 만들지 않고 참조만
    //      다시 이어 준다 — 기획자가 인스펙터에서 조정해 둔 값이 도구를 다시
    //      돌렸다는 이유로 되돌아가면 안 된다.
    //   2) 한 번의 되돌리기(Ctrl+Z)로 전부 취소된다. 씬을 건드리는 도구가
    //      절반만 취소되면 손으로 고치는 것보다 나쁘다.
    public static class MemoryRoomSceneSetup
    {
        private const string MenuPath = "GameName/기억 방 씬 구성";
        private const string UndoGroupName = "기억 방 씬 구성";

        [MenuItem(MenuPath)]
        public static void Run()
        {
            var scene = EditorSceneManager.GetActiveScene();
            var report = new SceneSetupReport();

            var undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoGroupName);

            // 스크립트가 사라진 컴포넌트를 먼저 걷어낸다. 남겨 두면 인스펙터에
            // "Missing script" 경고가 계속 뜨고, 그 오브젝트를 건드리는 다른
            // 도구들도 함께 시끄러워진다.
            RemoveMissingScripts(report);

            var layoutAsset = MemoryRoomLayoutAssetLocator.FindOrCreate(report);
            var overlayHost = OverlayPanelSceneBuilder.Build(report);

            var gameSession = Object.FindFirstObjectByType<GameSessionBootstrap>(FindObjectsInactive.Include);
            if (gameSession == null)
                report.Problem("GameSessionBootstrap을 찾지 못했습니다. 화면들이 같은 세션을 공유하지 못합니다.");

            SetUpMemoryRoomScreen(gameSession, overlayHost, layoutAsset, report);

            EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(undoGroup);

            Present(scene.name, report);
        }

        private static void SetUpMemoryRoomScreen(
            GameSessionBootstrap gameSession,
            OverlayPanelHost overlayHost,
            MemoryRoomLayoutAsset layoutAsset,
            SceneSetupReport report)
        {
            var memoryRoomScreen = Object.FindFirstObjectByType<MemoryRoomBootstrap>(FindObjectsInactive.Include);
            if (memoryRoomScreen == null)
            {
                report.Problem(
                    "MemoryRoomBootstrap을 찾지 못했습니다. 기억 방 화면 GameObject가 있는 씬에서 실행하세요.");
                return;
            }

            var spaceView = MemoryRoomSpaceSceneBuilder.Build(memoryRoomScreen, layoutAsset, overlayHost, report);

            Link(memoryRoomScreen, "_gameSession", gameSession, report);
            Link(memoryRoomScreen, "_spaceView", spaceView, report);
            Link(memoryRoomScreen, "_layoutAsset", layoutAsset, report);
            Link(memoryRoomScreen, "_overlayPanels", overlayHost, report);
        }

        private static void Link(Object target, string fieldName, Object value, SceneSetupReport report)
        {
            if (target == null || value == null)
                return;

            if (SerializedFieldBinder.BindObject(target, fieldName, value, report))
                report.Linked($"{target.name}.{fieldName} → {value.name}");
        }

        private static void RemoveMissingScripts(SceneSetupReport report)
        {
            var removed = 0;

            foreach (var root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (var child in root.GetComponentsInChildren<Transform>(includeInactive: true))
                    removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(child.gameObject);
            }

            if (removed > 0)
                report.Linked($"사라진 스크립트 컴포넌트 {removed}개 제거");
        }

        private static void Present(string sceneName, SceneSetupReport report)
        {
            var summary = report.Summarize();
            Debug.Log($"[{UndoGroupName}] 씬: {sceneName}\n{summary}");

            var title = report.HasProblems ? "기억 방 씬 구성 — 확인 필요" : "기억 방 씬 구성 완료";
            EditorUtility.DisplayDialog(title, summary, "확인");
        }
    }
}
