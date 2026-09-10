using System.Linq;
using GameName.UI.MemoryRoom.Space;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameName.UI.Editor.SceneSetup
{
    // 시계면(ClockSimple) 위에 도는 시침·분침을 얹는 도구.
    //
    // 조명이 아니라 씬 콘텐츠(스프라이트 + 회전 컴포넌트)라, 조명 빌더와도
    // 메인 씬 구성과도 분리한 별도 메뉴로 둔다 — 메인 씬 구성은 손 배치
    // 스프라이트를 의도적으로 제외하고, 조명 빌더는 Light2D만 다룬다.
    //
    // 방 배경을 방 씬으로 뽑아낸 뒤로는 Clock이 방 씬(Room_Greenroom)의 루트에
    // 있고 MemoryRoomBootstrap이 없다. 그래서 계층 위치가 아니라 활성 씬 전체에서
    // 이름으로 Clock을 찾는다 — 이 도구가 필요로 하는 건 Clock 하나뿐이다.
    //
    // ── 여러 번 돌려도 안전하다 ─────────────────────────────────────────
    // Clock 스프라이트를 ClockSimple로 바꾸고 Hour/Min 자식을 이름으로 찾아
    // 없을 때만 만든다. 트랜스폼(localPosition·회전·스케일)은 만들 때 한 번만
    // 잡는다 — 손으로 미세조정한 것을 재실행이 지우면 안 되므로. sortingOrder·
    // 머티리얼·ClockHands 참조는 매번 다시 맞춘다(도구가 정하는 값).
    // 되돌리기는 한 번의 Ctrl+Z(단, Play 진입 후에는 오브젝트를 직접 지워야 한다).
    public static class MemoryRoomClockSceneBuilder
    {
        private const string MenuPath = "GameName/기억 방 시계 바늘 구성";
        private const string UndoGroupName = "기억 방 시계 바늘 구성";

        private const string ClockSimpleSpriteGuid = "eba9d17501715ce4199aedd87b566eb6";
        private const string HourSpriteGuid = "b2c577d1e0306184bad54e332afbe733";
        private const string MinSpriteGuid = "b2faeba1415acb94aa1a01481c125e4c";

        // 시계면·바늘이 어두운 방에서 묻히지 않게 갈아탈 머티리얼(Sprite-Unlit-Default).
        // 조명 빌더도 Clock을 이 머티리얼로 바꾼다 — 광원 취급 스프라이트.
        private const string UnlitMaterialGuid = "9dfc825aed78fcd4ba02077103263b40";

        // 풀프레임 바늘 스프라이트의 커스텀 피벗 (0.5, 0.8666667)을 시계면 프레임에
        // 겹치게 올리는 자식 로컬 위치. (0.8666667 - 0.5) * 1.8(프레임 높이) = 0.66.
        private static readonly Vector3 HandLocalPosition = new Vector3(0f, 0.66f, 0f);

        private const int HourSortingOrder = -79; // 시계면(-80) 앞, 분침 뒤
        private const int MinSortingOrder = -78;  // 시침 앞 (다음 레이어 Lamp는 -70)

        [MenuItem(MenuPath)]
        public static void Run()
        {
            var scene = EditorSceneManager.GetActiveScene();
            var report = new SceneSetupReport();

            var undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoGroupName);

            var clock = FindInLoadedScenes("Clock");
            if (clock == null)
            {
                report.Problem(
                    "Clock 오브젝트를 찾지 못했습니다. 그린룸 배경이 있는 씬(Room_Greenroom 또는 " +
                    "배경 추출 전 LDY_GameScene)을 열고 실행하세요.");
                Present(scene.name, report);
                return;
            }

            var unlit = LoadMaterial(UnlitMaterialGuid, report);

            Step("시계면 교체", () => SwapClockFace(clock, unlit, report), report);

            Transform hour = null;
            Transform min = null;
            Step("시침 배치",
                () => hour = BuildHand(clock, "Hour", HourSpriteGuid, HourSortingOrder, unlit, report), report);
            Step("분침 배치",
                () => min = BuildHand(clock, "Min", MinSpriteGuid, MinSortingOrder, unlit, report), report);
            Step("ClockHands", () => WireClockHands(clock, hour, min, report), report);

            // Clock이 든 씬을 더티로 — 활성 씬이 아니라(추출 후 Room_Greenroom을
            // 따로 열었을 수 있다) Clock이 실제로 있는 씬을 저장 대상으로 표시한다.
            var clockScene = clock.gameObject.scene;
            EditorSceneManager.MarkSceneDirty(clockScene);
            Undo.CollapseUndoOperations(undoGroup);
            Present(clockScene.name, report);
        }

        // 한 단계를 감싸 예외를 붙잡아 리포트로 돌린다 — 한 단계가 던져도 나머지는
        // 돌고, 완료 다이얼로그·씬 더티·되돌리기 병합은 항상 실행된다.
        private static void Step(string label, System.Action action, SceneSetupReport report)
        {
            try
            {
                action();
            }
            catch (System.Exception ex)
            {
                report.Problem($"[{label}] 실패: {ex.GetType().Name} — {ex.Message}");
                Debug.LogException(ex);
            }
        }

        // ── A. Clock 스프라이트를 바늘 없는 시계면으로 ──────────────────────
        private static void SwapClockFace(Transform clock, Material unlit, SceneSetupReport report)
        {
            var renderer = clock.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                report.Problem("Clock에 SpriteRenderer가 없어 시계면을 바꾸지 못했습니다.");
                return;
            }

            var clockSimple = LoadSprite(ClockSimpleSpriteGuid, "ClockSimple", report);
            if (clockSimple == null)
                return;

            Undo.RecordObject(renderer, UndoGroupName);
            var changed = false;

            if (renderer.sprite != clockSimple)
            {
                renderer.sprite = clockSimple;
                changed = true;
            }

            // sortingOrder(-80)는 건드리지 않는다 — 손으로 잡은 레이어 순서.
            if (unlit != null && renderer.sharedMaterial != unlit)
            {
                renderer.sharedMaterial = unlit;
                changed = true;
            }

            EditorUtility.SetDirty(renderer);
            report.Linked(changed
                ? "Clock 스프라이트 → ClockSimple (바늘 없는 시계면)"
                : "Clock 이미 ClockSimple + Unlit");
        }

        // ── B. 시침·분침 — Clock의 자식, 시계면 프레임에 겹침 ───────────────
        private static Transform BuildHand(
            Transform clock, string name, string spriteGuid, int sortingOrder, Material unlit, SceneSetupReport report)
        {
            var sprite = LoadSprite(spriteGuid, name, report);
            if (sprite == null)
                return null;

            var existing = FindImmediateChild(clock, name);
            var created = existing == null;

            GameObject go;
            if (created)
            {
                go = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(go, UndoGroupName);
                go.transform.SetParent(clock, worldPositionStays: false);
                // 트랜스폼은 생성 때만 — 이후 손으로 미세조정한 값을 재실행이 지우지 않게.
                go.transform.localPosition = HandLocalPosition;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one;
            }
            else
            {
                go = existing.gameObject;
            }

            var renderer = go.GetComponent<SpriteRenderer>();
            if (renderer == null)
                renderer = Undo.AddComponent<SpriteRenderer>(go);

            Undo.RecordObject(renderer, UndoGroupName);
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            if (unlit != null)
                renderer.sharedMaterial = unlit;
            EditorUtility.SetDirty(renderer);

            report.Linked(created
                ? $"Clock/{name} 생성 (order {sortingOrder}, local y {HandLocalPosition.y})"
                : $"Clock/{name} 값 재적용 (order {sortingOrder})");
            return go.transform;
        }

        // ── C. 회전 컴포넌트 ───────────────────────────────────────────────
        private static void WireClockHands(Transform clock, Transform hour, Transform min, SceneSetupReport report)
        {
            var hands = clock.GetComponent<ClockHands>();
            var created = hands == null;
            if (created)
                hands = Undo.AddComponent<ClockHands>(clock.gameObject);

            if (hands == null)
            {
                report.Problem("Clock에 ClockHands를 붙이지 못했습니다.");
                return;
            }

            if (hour == null)
                hour = FindImmediateChild(clock, "Hour");
            if (min == null)
                min = FindImmediateChild(clock, "Min");

            var wired = 0;
            if (hour != null && SerializedFieldBinder.BindObject(hands, "_hourHand", hour, report))
                wired++;
            if (min != null && SerializedFieldBinder.BindObject(hands, "_minuteHand", min, report))
                wired++;
            EditorUtility.SetDirty(hands);

            report.Linked(created
                ? $"Clock에 ClockHands (참조 {wired}개 연결)"
                : $"ClockHands 참조 재적용 ({wired}개 갱신)");
        }

        // ── 공통 ───────────────────────────────────────────────────────────

        private static Sprite LoadSprite(string guid, string label, SceneSetupReport report)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var sprite = string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                report.Problem($"{label} 스프라이트를 찾지 못했습니다(임포트 전이면 다시 실행).");

            return sprite;
        }

        private static Material LoadMaterial(string guid, SceneSetupReport report)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var material = string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
                report.Problem("Sprite-Unlit-Default 머티리얼을 찾지 못했습니다. 머티리얼 교체를 건너뜁니다.");

            return material;
        }

        // 열려 있는 모든 씬에서 이름으로 Transform을 찾는다(비활성 포함). Clock이
        // 씬 루트에 있든(방 씬) 다른 오브젝트 밑에 있든(구 단일 씬), Room_Greenroom을
        // 단독으로 열었든 LDY_GameScene과 함께 열었든 똑같이 찾힌다.
        private static Transform FindInLoadedScenes(string name)
        {
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                    continue;

                foreach (var root in scene.GetRootGameObjects())
                {
                    var found = root.GetComponentsInChildren<Transform>(includeInactive: true)
                        .FirstOrDefault(t => t.name == name);
                    if (found != null)
                        return found;
                }
            }
            return null;
        }

        // 바로 아래 자식만(중첩 계층의 동명 오브젝트와 섞이지 않게).
        private static Transform FindImmediateChild(Transform parent, string name)
        {
            for (var i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child.name == name)
                    return child;
            }

            return null;
        }

        private static void Present(string sceneName, SceneSetupReport report)
        {
            var summary = report.Summarize();
            Debug.Log($"[{UndoGroupName}] 씬: {sceneName}\n{summary}");

            var title = report.HasProblems ? "기억 방 시계 바늘 구성 — 확인 필요" : "기억 방 시계 바늘 구성 완료";
            EditorUtility.DisplayDialog(title, summary, "확인");
        }
    }
}
