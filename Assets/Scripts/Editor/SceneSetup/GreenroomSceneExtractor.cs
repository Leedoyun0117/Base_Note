using System.Linq;
using GameName.UI.Session;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameName.UI.Editor.SceneSetup
{
    // App 씬(현 LDY_GameScene)에 손으로 깔려 있는 그린룸 배경 아트를 별도
    // 방 씬(Room_Greenroom)으로 떼어내고, App 씬에는 RoomSceneLoader를 심는다.
    //
    // ── 왜 ────────────────────────────────────────────────────────────
    // 방마다 씬 하나로 가는 개편의 첫 수술. 부트스트랩·UI·절차적 방 껍데기(Space)
    // 는 App 씬에 남고, 배경 아트+라이트만 방 씬으로 빠져 RoomSceneLoader가
    // RoomStartedEvent에 맞춰 additive로 갈아 끼운다. GameSession은 App 씬에
    // 있어 방 전환에도 살아남으므로 Core는 무변경.
    //
    // ── 이 도구는 파괴적이다 ──────────────────────────────────────────
    // 기존 씬 빌더들은 오브젝트를 "추가"만 했지만, 이건 App 씬에서 오브젝트를
    // "덜어낸다". 그래서:
    //   · 되돌리기가 되는 부분(언페어런트·씬 이동·컴포넌트 추가)은 Undo 그룹
    //     하나로 묶는다. 단 새 씬 파일 생성과 저장 자체는 Undo 대상이 아니다 —
    //     그건 git checkout으로 되돌린다(두 .unity 파일).
    //   · 실행 후 두 씬을 전부 저장한다. 사용자가 git diff로 확인한 뒤 커밋한다
    //     (이 도구는 커밋하지 않는다).
    //   · 여러 번 돌려도 안전하다: 아트가 이미 App 씬에 없으면 추출을 건너뛰고
    //     RoomSceneLoader 배선만 맞춘다.
    //
    // ── 무엇이 그린룸 배경인가 ────────────────────────────────────────
    // 아래 10개 최상위 오브젝트(+자식 라이트). 전부 identity 부모(Space /
    // MemoryRoomScreen) 밑이라 언페어런트해도 월드 위치가 불변이고, additive로
    // 겹쳐 그려도 그대로다. 그린룸 스프라이트는 scale 1로 카메라(3.2×1.8)를 딱
    // 채우므로 별도 컨테이너가 필요 없다.
    public static class GreenroomSceneExtractor
    {
        private const string MenuPath = "GameName/방 배경 추출 — 그린룸 → Room_Greenroom";
        private const string UndoGroupName = "그린룸 방 씬 추출";

        private const string RoomSceneName = "Room_Greenroom";
        private const string RoomScenePath = "Assets/Rooms/Room_Greenroom.unity";
        private const string RoomSceneMapPath = "Assets/Settings/RoomSceneMap.asset";

        // 떼어낼 최상위 오브젝트. Space 자식 7개 + MemoryRoomScreen 자식 3개.
        // 자식(Window Light 등)은 부모를 따라 함께 옮겨진다.
        private static readonly string[] ArtRootNames =
        {
            "Window", "Clock", "Lamp", "LeftPerson", "RightPerson", "Things", "Smoke",
            "BG", "Shadow", "Global Light 2D",
        };

        [MenuItem(MenuPath)]
        public static void Run()
        {
            var app = EditorSceneManager.GetActiveScene();
            var report = new SceneSetupReport();

            var undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoGroupName);

            var gameSession = Object.FindFirstObjectByType<GameSessionBootstrap>(FindObjectsInactive.Include);
            if (gameSession == null)
            {
                report.Problem("GameSessionBootstrap을 찾지 못했습니다. App 씬(LDY_GameScene)을 열고 실행하세요.");
                Present(app.name, report, savedRoom: false);
                return;
            }

            var map = AssetDatabase.LoadAssetAtPath<RoomSceneMap>(RoomSceneMapPath);
            if (map == null)
                report.Problem($"{RoomSceneMapPath}를 찾지 못했습니다. RoomSceneLoader._map은 손으로 연결하세요.");

            // ── 1. 그린룸 아트를 App 씬에서 찾는다 ─────────────────────────
            var artRoots = ArtRootNames
                .Select(name => FindInScene(app, name))
                .Where(t => t != null)
                .ToList();

            var savedRoom = false;
            if (artRoots.Count == 0)
            {
                report.Linked("그린룸 아트가 App 씬에 없습니다 — 이미 추출된 것으로 보고 RoomSceneLoader 배선만 맞춥니다.");
            }
            else
            {
                if (artRoots.Count != ArtRootNames.Length)
                {
                    var found = artRoots.Select(t => t.name);
                    var missing = ArtRootNames.Where(n => !found.Contains(n));
                    report.Problem(
                        $"그린룸 아트 {ArtRootNames.Length}개 중 {artRoots.Count}개만 찾았습니다. " +
                        $"못 찾음: {string.Join(", ", missing)}. 중단합니다 — 계층이 예상과 다릅니다.");
                    Present(app.name, report, savedRoom: false);
                    return;
                }

                savedRoom = ExtractToRoomScene(app, artRoots, report);
                if (!savedRoom)
                {
                    Present(app.name, report, savedRoom: false);
                    return;
                }
            }

            // NewScene(Additive)이 새 씬을 활성 씬으로 바꿔 놓을 수 있다 — 이후로 만드는
            // 오브젝트(RoomSceneLoader)가 엉뚱한 씬에 들어가지 않게 App 씬을 다시 활성화한다.
            if (EditorSceneManager.GetActiveScene() != app)
                SceneManager.SetActiveScene(app);

            // ── 4. App 씬에 RoomSceneLoader 심기 ──────────────────────────
            EnsureRoomSceneLoader(app, gameSession, map, report);

            // ── 5. 두 씬 저장 ────────────────────────────────────────────
            EditorSceneManager.MarkSceneDirty(app);
            if (!EditorSceneManager.SaveScene(app))
                report.Problem("App 씬 저장에 실패했습니다. 수동으로 저장하세요.");
            else
                report.Linked($"App 씬 저장: {app.path}");

            Undo.CollapseUndoOperations(undoGroup);
            Present(app.name, report, savedRoom);
        }

        // 아트 루트들을 언페어런트 → 새 방 씬으로 이동 → 저장 → 빌드 설정 등록.
        // app을 받는 이유: NewScene이 활성 씬을 새 방 씬으로 바꿔 놓으므로, 끝나기 전에
        // 되돌려 놓아야 이 뒤 단계가 App 씬을 건드린다.
        private static bool ExtractToRoomScene(Scene app, System.Collections.Generic.List<Transform> artRoots, SceneSetupReport report)
        {
            EnsureFolder("Assets/Rooms");

            // 이미 파일이 있으면 덮어쓰지 않는다 — 두 번째 실행에서 여기까지 오면
            // 계층 상태가 꼬인 것이므로 사람이 봐야 한다.
            if (System.IO.File.Exists(RoomScenePath))
            {
                report.Problem(
                    $"{RoomScenePath}가 이미 있는데 App 씬에도 그린룸 아트가 남아 있습니다. " +
                    "두 씬 상태가 어긋났습니다 — git checkout으로 되돌리고 다시 실행하세요.");
                return false;
            }

            var room = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

            foreach (var t in artRoots)
            {
                // MoveGameObjectToScene은 루트만 받는다 — 먼저 계층에서 떼어낸다(월드 위치 보존).
                Undo.SetTransformParent(t, null, $"{UndoGroupName} — {t.name} 언페어런트");
                Undo.MoveGameObjectToScene(t.gameObject, room, $"{UndoGroupName} — {t.name} 씬 이동");
                report.Created($"{t.name} → {RoomSceneName}");
            }

            if (!EditorSceneManager.SaveScene(room, RoomScenePath))
            {
                report.Problem($"{RoomScenePath} 저장에 실패했습니다.");
                return false;
            }
            // 빌드 설정 등록이 씬 GUID를 읽으려면 임포트가 끝나 있어야 한다.
            AssetDatabase.ImportAsset(RoomScenePath, ImportAssetOptions.ForceSynchronousImport);
            report.Created($"{RoomScenePath} ({artRoots.Count}개 루트)");

            RegisterInBuildSettings(report);

            // 활성 씬을 App으로 되돌린다 — NewScene이 방 씬을 활성으로 만들어 놨을 수 있다.
            if (EditorSceneManager.GetActiveScene() != app)
                SceneManager.SetActiveScene(app);
            return true;
        }

        private static void RegisterInBuildSettings(SceneSetupReport report)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(s => s.path == RoomScenePath))
            {
                report.Linked("빌드 설정에 이미 등록됨");
                return;
            }

            scenes.Add(new EditorBuildSettingsScene(RoomScenePath, enabled: true));
            EditorBuildSettings.scenes = scenes.ToArray();
            report.Created($"빌드 설정에 {RoomScenePath} 추가");
        }

        private static void EnsureRoomSceneLoader(
            Scene app, GameSessionBootstrap gameSession, RoomSceneMap map, SceneSetupReport report)
        {
            var loader = Object.FindFirstObjectByType<RoomSceneLoader>(FindObjectsInactive.Include);
            var created = loader == null;
            if (created)
            {
                var go = new GameObject("RoomSceneLoader");
                Undo.RegisterCreatedObjectUndo(go, UndoGroupName);
                // new GameObject는 활성 씬에 생긴다 — 확실히 App 씬에 두도록 못박는다.
                if (go.scene != app)
                    Undo.MoveGameObjectToScene(go, app, $"{UndoGroupName} — RoomSceneLoader 씬 배치");
                loader = Undo.AddComponent<RoomSceneLoader>(go);
                report.Created($"{app.name}/RoomSceneLoader");
            }

            if (SerializedFieldBinder.BindObject(loader, "_gameSession", gameSession, report))
                report.Linked("RoomSceneLoader._gameSession → GameSessionBootstrap");
            if (map != null && SerializedFieldBinder.BindObject(loader, "_map", map, report))
                report.Linked($"RoomSceneLoader._map → {RoomSceneName} 매핑 에셋");
        }

        // 활성 씬 전체에서 이름으로 Transform을 찾는다(비활성 포함).
        private static Transform FindInScene(Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var found = root.GetComponentsInChildren<Transform>(includeInactive: true)
                    .FirstOrDefault(t => t.name == name);
                if (found != null)
                    return found;
            }
            return null;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            var parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            var leaf = System.IO.Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        private static void Present(string sceneName, SceneSetupReport report, bool savedRoom)
        {
            var summary = report.Summarize();
            Debug.Log($"[{UndoGroupName}] 씬: {sceneName}\n{summary}");

            var next = savedRoom
                ? "\n\n다음: 커밋하지 말고 —\n" +
                  "  1) git diff Assets/LDY_GameScene.unity Assets/Rooms/Room_Greenroom.unity\n" +
                  "  2) EditMode 테스트\n" +
                  "  3) Play — RoomSceneLoader가 Room_Greenroom을 additive로 얹는지\n" +
                  "확인한 뒤 커밋. 틀리면 git checkout으로 두 .unity 되돌리기."
                : string.Empty;

            var title = report.HasProblems ? $"{UndoGroupName} — 확인 필요" : $"{UndoGroupName} 완료";
            EditorUtility.DisplayDialog(title, summary + next, "확인");
        }
    }
}
