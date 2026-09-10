using System.Linq;
using GameName.UI.MemoryRoom;
using GameName.UI.Session;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameName.UI.Editor.SceneSetup
{
    // GameScene2를 "앞 두 방만 도는" 플레이 가능한 씬으로 채우는 도구.
    //
    // GameScene2에는 이미 손으로 놓은 것들(오버레이 문서 셋, 카메라, 전역 라이트,
    // ClockTowerFix 스프라이트)이 있고 그것들은 그대로 둔다. 이 도구가 채우는 것은
    // 빠진 뼈대 둘뿐이다 — GameSession(방 수를 2로 제한) + MemoryRoomScreen
    // (HUD·대화 패널이 붙는 UIDocument + MemoryRoomBootstrap). 나머지 배선은 기존
    // "기억 방 씬 구성" 메뉴가 그대로 한다(이 도구가 끝나면 그것을 한 번 더 돌린다).
    //
    // 여러 번 돌려도 안전하다: 이미 있는 오브젝트는 다시 만들지 않고, 방 수 제한과
    // 빌드 설정 등록만 매번 다시 맞춘다.
    public static class GameScene2Builder
    {
        private const string MenuPath = "GameName/GameScene2 2방 플레이 구성";
        private const string UndoGroupName = "GameScene2 2방 플레이 구성";

        private const string ScenePath = "Assets/LDY_GameScene2.unity";
        private const string ExpectedSceneName = "LDY_GameScene2";
        private const string MemoryRoomScreenUxmlPath = "Assets/UI/MemoryRoom/MemoryRoomScreen.uxml";

        // 앞 두 방(유년기·청소년기)만. 방 3(R)은 데이터에 그대로 남고 이 판에만
        // 안 실린다 — 마지막 방을 마치면 RunProgressor가 RunCompletedEvent를 낸다.
        private const int RoomLimit = 2;

        [MenuItem(MenuPath)]
        public static void Run()
        {
            var scene = EditorSceneManager.GetActiveScene();
            var report = new SceneSetupReport();

            if (scene.name != ExpectedSceneName)
            {
                EditorUtility.DisplayDialog(
                    "GameScene2 2방 플레이 구성 — 확인 필요",
                    $"활성 씬이 {ExpectedSceneName}이 아닙니다(지금: {scene.name}).\n" +
                    $"{ScenePath}를 열고 다시 실행하세요.",
                    "확인");
                return;
            }

            var undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoGroupName);

            var gameSession = FindOrCreateGameSession(report);
            if (gameSession != null &&
                SerializedFieldBinder.BindEnum(gameSession, "_roomLimit", RoomLimit, report))
            {
                report.Linked($"GameSession._roomLimit → {RoomLimit} (앞 두 방만)");
            }

            FindOrCreateMemoryRoomScreen(report);
            RegisterInBuildSettings(report);

            EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(undoGroup);

            Present(scene.name, report);
        }

        private static GameSessionBootstrap FindOrCreateGameSession(SceneSetupReport report)
        {
            var existing = Object.FindFirstObjectByType<GameSessionBootstrap>(FindObjectsInactive.Include);
            if (existing != null)
                return existing;

            var go = new GameObject("GameSession");
            Undo.RegisterCreatedObjectUndo(go, UndoGroupName);
            var bootstrap = Undo.AddComponent<GameSessionBootstrap>(go);
            report.Created("GameSession (GameSessionBootstrap)");
            return bootstrap;
        }

        // MemoryRoomBootstrap은 [RequireComponent(UIDocument)]라 UIDocument를 먼저
        // 붙인다. 이 UIDocument가 상단 HUD와 하단 대화 패널이 그려지는 자리다.
        private static void FindOrCreateMemoryRoomScreen(SceneSetupReport report)
        {
            var existing = Object.FindFirstObjectByType<MemoryRoomBootstrap>(FindObjectsInactive.Include);
            if (existing != null)
            {
                report.Linked("MemoryRoomScreen 이미 있음");
                return;
            }

            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(MemoryRoomScreenUxmlPath);
            if (visualTree == null)
            {
                report.Problem(
                    $"{MemoryRoomScreenUxmlPath}를 찾지 못했습니다. MemoryRoomScreen을 만들지 못했습니다.");
                return;
            }

            var panelSettings = ResolvePanelSettings(report);

            var go = new GameObject("MemoryRoomScreen");
            Undo.RegisterCreatedObjectUndo(go, UndoGroupName);

            var document = Undo.AddComponent<UIDocument>(go);
            document.panelSettings = panelSettings;
            document.visualTreeAsset = visualTree;
            document.sortingOrder = 0;

            Undo.AddComponent<MemoryRoomBootstrap>(go);
            report.Created($"MemoryRoomScreen (UIDocument {MemoryRoomScreenUxmlPath} + MemoryRoomBootstrap)");
        }

        // 프로젝트에 PanelSettings가 하나뿐이라는 전제로 그것을 찾아 쓴다 —
        // 오버레이 빌더와 같은 관례.
        private static PanelSettings ResolvePanelSettings(SceneSetupReport report)
        {
            var guids = AssetDatabase.FindAssets("t:PanelSettings");
            if (guids.Length > 0)
                return AssetDatabase.LoadAssetAtPath<PanelSettings>(
                    AssetDatabase.GUIDToAssetPath(guids[0]));

            report.Problem("PanelSettings 에셋을 찾지 못했습니다. HUD·대화 패널이 화면에 보이지 않을 수 있습니다.");
            return null;
        }

        // PlayMode 테스트가 씬을 이름으로 불러오려면 빌드 설정 목록에 있어야 한다.
        // 첫 씬(LDY_GameScene)은 그대로 두고 뒤에 덧붙인다 — 스탠드얼론 빌드는
        // 여전히 3방짜리 씬으로 시작한다.
        private static void RegisterInBuildSettings(SceneSetupReport report)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(s => s.path == ScenePath))
            {
                report.Linked("빌드 설정에 이미 등록됨");
                return;
            }

            scenes.Add(new EditorBuildSettingsScene(ScenePath, enabled: true));
            EditorBuildSettings.scenes = scenes.ToArray();
            report.Created($"빌드 설정에 {ScenePath} 추가(맨 뒤, 활성)");
        }

        private static void Present(string sceneName, SceneSetupReport report)
        {
            var summary = report.Summarize();
            Debug.Log($"[{UndoGroupName}] 씬: {sceneName}\n{summary}");

            var next =
                "\n\n다음: GameName ▸ 기억 방 씬 구성 을 한 번 실행해 나머지 배선을 잇고,\n" +
                "씬을 저장(Ctrl+S)한 뒤 Play로 확인하세요.";
            var title = report.HasProblems
                ? "GameScene2 2방 플레이 구성 — 확인 필요"
                : "GameScene2 2방 플레이 구성 완료";
            EditorUtility.DisplayDialog(title, summary + next, "확인");
        }
    }
}
