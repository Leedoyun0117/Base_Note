using System.Linq;
using GameName.UI.MemoryRoom;
using GameName.UI.MemoryRoom.Space;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GameName.UI.Editor.SceneSetup
{
    // 방 2(시계탑 옥상)의 스프라이트 레이어 + 야간 조명 + 연출을 한 번에 얹는 도구.
    //
    // 그린룸(MemoryRoomLightingSceneBuilder / MemoryRoomClockSceneBuilder)과 나누는 이유:
    // 저 둘은 그린룸 스프라이트 이름·GUID·청록 팔레트·주광=램프가 하드코딩돼 있고,
    // 방 2는 레이어 구성도, 주광(=시계면)도, 밤 팔레트도 전부 다르다. 게임 규칙·SO·
    // 방 1 구성·마스크 셰이더(DotTexture)는 하나도 건드리지 않는다.
    //
    // ── 무엇을 만드나 ──────────────────────────────────────────────────
    //  · 배경 고정 / 콘텐츠 흔들림 분리 (그린룸 방 1 패턴 그대로):
    //    - RoomArt_room2 (스위처가 껐다 켜는 고정 컨테이너, CameraShake 밖):
    //      BG + 아주 낮은 푸른 밤 Global.
    //    - RoomArt_room2_Motion (Space 자식 = CameraShake 대상):
    //      SideBuilding·MainTower·MainTowerLight·Clock·(Hour·Min)·
    //      BackIronBar·LeftPeople·RightPeople·FrontIronBar·Fly.
    //    두 컨테이너 모두 카메라 세로 뷰에 맞춰 같은 scale·위치로 정규화하므로
    //    reparent 후에도 배경과 콘텐츠가 정확히 겹친다. 스위처 room-2 항목에
    //    두 컨테이너를 모두 등록해 방 전환에 함께 켜지고 꺼진다.
    //  · 시계면 자식으로 따뜻한 Point 주광 + 느린 맥동(Light2DFlicker) — Clock이
    //    흔들림 컨테이너 안에 있으므로 빛·그림자도 콘텐츠와 함께 흔들린다.
    //  · 인물·난간에 ShadowCaster2D(trimEdge=0), 시계면·발광·바늘은 Unlit.
    //  · 시침·분침은 ClockHands로 6도 스냅 회전, 빛 주위엔 FlyWander로 벌레.
    //
    // ── 여러 번 돌려도 안전하다 ─────────────────────────────────────────
    // 오브젝트는 이름으로 찾아 없을 때만 만든다. 트랜스폼(localPosition·scale)은
    // 만들 때만 잡고(손 미세조정 보존), sortingOrder·머티리얼·라이트 수치·컴포넌트
    // 참조는 "이 도구가 정하는 값"이라 매 실행 아래 상수로 되돌린다. 조율은 상수를
    // 고치고 메뉴를 다시 누르는 흐름이 가장 빠르다. Ctrl+Z 한 번(Play 진입 후엔 직접 삭제).
    public static class ClockTowerRoomSceneBuilder
    {
        private const string MenuPath = "GameName/시계탑 방 구성";
        private const string UndoGroupName = "시계탑 방 구성";

        private const string RoomArtName = "RoomArt_room2";
        // 흔들리는 콘텐츠 레이어를 담는 컨테이너 — Space 자식이라 CameraShake가 민다.
        private const string MotionArtName = "RoomArt_room2_Motion";

        // 광원 자신·발광 레이어가 어두워지면 안 되므로 갈아탈 머티리얼(Sprite-Unlit-Default).
        private const string UnlitMaterialGuid = "9dfc825aed78fcd4ba02077103263b40";

        // 캔버스 460x230 @ PPU 100 = 네이티브 4.6 x 2.3 유닛.
        private const float CanvasWidthUnits = 4.6f;
        private const float CanvasHeightUnits = 2.3f;

        // RoomArt_room2 컨테이너를 카메라 뷰에 맞춘다 — 씬마다 카메라(ortho·위치)가
        // 달라서 상수로 못 박는다(그린룸 씬 ortho 0.9·y 0.69, GameScene2 ortho 0.63·y 0).
        // 매 실행 카메라 세로 뷰 높이에 캔버스(2.3유닛) 세로를 맞추고 카메라 정면에 중앙 정렬.
        // 결정론적이라 첫 실행이 어긋났어도 다시 눌러 self-correct 된다.

        // 시계 허브(그려진 피벗). Hour/Min 메타의 커스텀 피벗과 같은 값 —
        // (228, 101.5)px / (460, 230). 풀프레임 레이어(중앙 피벗) 대비 허브의 로컬 오프셋을
        // 여기서 되짚어, 바늘 텍스처는 다른 레이어와 정렬시키되 회전축만 허브에 둔다.
        private static readonly Vector2 HubPivot = new Vector2(0.49565f, 0.55870f);
        // 허브가 캔버스 중앙에서 떨어진 로컬 오프셋 ≈ (-0.020, +0.135).
        // 바늘 스프라이트의 피벗이 곧 허브라, 바늘 GO를 이 점에 두면 회전축이
        // 허브에 오면서 텍스처도 다른 레이어와 정확히 겹친다.
        private static Vector3 HubOffsetFromCenter => new Vector3(
            (HubPivot.x - 0.5f) * CanvasWidthUnits,
            (HubPivot.y - 0.5f) * CanvasHeightUnits, 0f);
        private static Vector3 HandTextureAlign => HubOffsetFromCenter; // 바늘 GO의 localPosition

        // ── 레이어 (뒤 → 앞) ───────────────────────────────────────────────
        //  실제 그림 관계: MainTowerLight는 탑 벽면의 따뜻한 림라이트 스트로크라
        //  시계면 '뒤'가 자연스럽다(창이 아니다). 육안 확인 후 -75로 올려도 된다.
        private struct Layer
        {
            public string Name;
            public string Guid;
            public int Order;
            public bool Unlit;
            // true면 Space 자식(흔들리는 콘텐츠), false면 RoomArt_room2 직속(고정 배경).
            public bool Motion;
        }

        private static readonly Layer[] Layers =
        {
            new Layer { Name = "BG",             Guid = "30c4884bb0a4cc244b79e1d9374e9a91", Order = -100 },
            new Layer { Name = "SideBuilding",   Guid = "41ec90c47ab7efb4095849d76954ad3c", Order = -95, Motion = true },
            new Layer { Name = "MainTower",      Guid = "1ff135b245bca304eb580f55153564bf", Order = -90, Motion = true },
            new Layer { Name = "MainTowerLight", Guid = "cfed19e4bfb0293489ab7f499f14cf69", Order = -85, Unlit = true, Motion = true },
            new Layer { Name = "Clock",          Guid = "0653399b5b4a75444928b4a5beb4c6fa", Order = -80, Unlit = true, Motion = true },
            new Layer { Name = "BackIronBar",    Guid = "46c2d6c5d09b8e846bd6a796d5b09bc8", Order = -70, Motion = true },
            new Layer { Name = "LeftPeople",     Guid = "b853ceedca137674f882def072d517ad", Order = -60, Motion = true },
            new Layer { Name = "RightPeople",    Guid = "dfa038f80b9569249a99a6c94439cee4", Order = -60, Motion = true },
            new Layer { Name = "FrontIronBar",   Guid = "9d057e0f587b0eb4485a8723ee3867cc", Order = -50, Motion = true },
        };

        private const string HourGuid = "b569a8740b88c2843abe4e4787b32f60";
        private const string MinGuid = "9ed6ca542f146bf418b850b7f6201b52";
        private const string FlyGuid = "4483ed8d1479fb84f99c8c079b1fd16b"; // 통짜 그림 — 참고용, 배치는 안 함
        private const int HourOrder = -79;
        private const int MinOrder = -78;
        private const int FlyOrder = -30;

        private static readonly string[] ShadowCasters =
            { "LeftPeople", "RightPeople", "FrontIronBar", "BackIronBar" };

        // ── 라이트 프리셋 (재실행할 때마다 되돌아가는 값) ──────────────────
        //  2D 그림자는 '그 라이트 기여분'만 깎는다 — 전역 필이 세면 도로 채워 안 보인다.
        //  그래서 시계면 주광을 세게, 밤 Global을 순수 검정만 면할 만큼만 낮게.
        private static readonly Color NightGlobalTint = new Color(0.42f, 0.52f, 0.80f);
        private const float NightGlobalIntensity = 0.12f;

        private static readonly Color ClockLightTint = new Color(1.00f, 0.86f, 0.55f); // 따뜻한 노랑 — 그린룸 청록과 대비
        private const float ClockLightIntensity = 1.7f;
        private const float ClockLightInnerRadius = 0.28f;  // 시계면 밝은 코어
        private const float ClockLightOuterRadius = 2.30f;  // 탑 벽면 + 아래 발코니·난간까지
        private const float ClockLightFalloff = 0.65f;      // 급하게 — 픽셀아트 뭉개짐 방지
        private const float ClockLightShadowIntensity = 1.0f;

        // 느린 맥동(Light2DFlicker) — 램프의 "툭 꺼짐"과 정반대. 거대 조명이 느리게 숨쉰다.
        //  · amplitude 작게, speed 느리게.
        //  · 깜빡임/정전 경로는 대기 시간을 사실상 무한으로 밀어 끈다(호흡만 남긴다).
        private const bool FlickerEnabled = true;
        private const float FlickerAmplitude = 0.06f;
        private const float FlickerSpeed = 0.22f;
        private const float FlickerIntervalSeconds = 999999f; // Idle이 영영 안 끝남 = dip/정전 없음
        private const float FlickerSpriteScale = 0.5f;
        private const float FlickerSpriteMinFactor = 0.85f;

        // 벌레(FlyWander) — 시계 빛 주위를 불규칙 궤도로.
        private const int FlyCount = 6;
        private const float FlyRadiusPx = 26f;
        private const float FlySpeed = 0.32f;

        [MenuItem(MenuPath)]
        public static void Run()
        {
            var scene = EditorSceneManager.GetActiveScene();
            var report = new SceneSetupReport();

            var undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoGroupName);

            if (Object.FindFirstObjectByType<MemoryRoomBootstrap>(FindObjectsInactive.Include) == null)
            {
                report.Problem("MemoryRoomBootstrap을 찾지 못했습니다. 기억 방 화면이 있는 씬에서 실행하세요.");
                Present(scene.name, report);
                return;
            }

            // RoomArt_room2는 씬마다 부모가 다르다 — GameScene2에선 MemoryRoomScreen 밑,
            // LDY_GameScene에선 OverlayPanels 밑(RoomArtSwitcher와 같은 오브젝트). 씬 전체에서 이름으로 찾는다.
            // 이 컨테이너는 CameraShake 밖이라 고정 배경(BG·밤 Global)만 담는다.
            var roomArt = FindInScene(RoomArtName);
            if (roomArt == null)
            {
                report.Problem($"{RoomArtName}을 찾지 못했습니다. GameScene2 2방 플레이 구성(또는 그에 준하는 RoomArtSwitcher 배선)을 먼저 실행하세요.");
                Present(scene.name, report);
                return;
            }

            // 흔들리는 콘텐츠 레이어는 Space(=CameraShake._shakeTarget) 자식으로 간다.
            // Space가 없으면 기억 방 씬 구성이 아직 안 끝난 것.
            var spaceView = Object.FindFirstObjectByType<MemoryRoomSpaceView>(FindObjectsInactive.Include);
            if (spaceView == null)
            {
                report.Problem("MemoryRoomSpaceView(Space)를 찾지 못했습니다. 먼저 GameName ▸ 기억 방 씬 구성을 실행하세요.");
                Present(scene.name, report);
                return;
            }
            var motion = EnsureMotionContainer(spaceView.transform, report);

            var unlit = LoadUnlitMaterial(report);

            // 두 컨테이너를 같은 카메라-핏 트랜스폼으로 정규화 — 배경과 콘텐츠가 정확히 겹치게.
            Step("컨테이너 정규화 (고정)", () => NormalizeContainer(roomArt, report), report);
            Step("컨테이너 정규화 (흔들림)", () => NormalizeContainer(motion, report), report);
            Step("절차적 구조 숨김", () => HideStructureRenderers(report), report);

            Transform clock = null;
            foreach (var layer in Layers)
            {
                var captured = layer;
                var target = captured.Motion ? motion : roomArt;
                var other = captured.Motion ? roomArt : motion;
                Step($"레이어 {captured.Name}", () =>
                {
                    // 이전 빌드가 다른 컨테이너에 깔아 뒀으면 옮긴다(멱등).
                    MigrateChild(other, target, captured.Name, report);
                    var t = BuildLayer(target, captured, unlit, report);
                    if (captured.Name == "Clock") clock = t;
                }, report);
            }

            Step("시침·분침 (ClockHands)", () => BuildClockHands(clock, unlit, report), report);
            Step("밤 Global Light", () => BuildNightGlobal(roomArt, report), report);
            Step("시계면 주광 + 느린 맥동", () => BuildClockLight(clock, report), report);

            foreach (var caster in ShadowCasters)
            {
                var captured = caster;
                Step($"{captured} ShadowCaster", () => ApplyShadowCaster(motion, captured, report), report);
            }

            Step("벌레 (FlyWander)", () =>
            {
                MigrateChild(roomArt, motion, "Fly", report);
                BuildFlies(motion, unlit, report);
            }, report);
            Step("다른 Global Light 토글 배선", () => WireRoom1GlobalToggle(roomArt, motion, report), report);
            Step("스위처 room-2 항목에 두 컨테이너 등록", () => WireRoom2Toggle(roomArt, motion, report), report);

            EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(undoGroup);
            Present(scene.name, report);
        }

        private static void Step(string label, System.Action action, SceneSetupReport report)
        {
            try { action(); }
            catch (System.Exception ex)
            {
                report.Problem($"[{label}] 실패: {ex.GetType().Name} — {ex.Message}");
                Debug.LogException(ex);
            }
        }

        // ── 컨테이너: 카메라 뷰에 맞춰 프레이밍 + 합성본 숨김 ────────────────
        private static void NormalizeContainer(Transform roomArt, SceneSetupReport report)
        {
            // 합성본 스프라이트(ClockTowerFix)는 레퍼런스로 남기되 렌더는 끈다 — 레이어와 이중 렌더 방지.
            var composite = roomArt.GetComponent<SpriteRenderer>();
            if (composite != null && composite.enabled)
            {
                Undo.RecordObject(composite, UndoGroupName);
                composite.enabled = false;
                EditorUtility.SetDirty(composite);
                report.Linked($"{RoomArtName}의 합성본 SpriteRenderer 비활성 (레퍼런스로 유지)");
            }

            var cam = Camera.main != null ? Camera.main : Object.FindFirstObjectByType<Camera>();
            if (cam == null || !cam.orthographic)
            {
                report.Problem($"직교 카메라를 찾지 못해 {roomArt.name} 프레이밍을 건드리지 않았습니다. 스케일을 손으로 맞추세요.");
                return;
            }

            // 캔버스 세로(2.3유닛)를 카메라 뷰 높이(2 x orthoSize)에 맞춘다. 매 실행 같은 값.
            var fit = 2f * cam.orthographicSize / CanvasHeightUnits;
            Undo.RecordObject(roomArt, UndoGroupName);
            var z = Mathf.Approximately(roomArt.localScale.z, 0f) ? 1f : roomArt.localScale.z;
            roomArt.localScale = new Vector3(fit, fit, z);

            // 카메라 정면에 중앙 정렬(월드 위치로 직접 — RectTransform 앵커·부모 흔들림에 안 휘둘리게).
            var c = cam.transform.position;
            roomArt.position = new Vector3(c.x, c.y, roomArt.position.z);
            EditorUtility.SetDirty(roomArt);

            report.Linked($"{roomArt.name} 카메라 뷰에 맞춤 (scale {fit:0.###}, 중앙 ({c.x:0.##}, {c.y:0.##})) — 프레이밍 재조정");
        }

        // ── 절차적 방 구조(벽·바닥·포스터)가 픽셀아트 뒤로 비치지 않게 ──────────
        private static void HideStructureRenderers(SceneSetupReport report)
        {
            var spaceView = Object.FindFirstObjectByType<MemoryRoomSpaceView>(FindObjectsInactive.Include);
            if (spaceView == null)
            {
                report.Problem("MemoryRoomSpaceView를 찾지 못해 구조 렌더러 숨김을 건너뜁니다.");
                return;
            }

            var so = new SerializedObject(spaceView);
            var p = so.FindProperty("_hideStructureRenderers");
            if (p == null)
            {
                report.Problem("MemoryRoomSpaceView._hideStructureRenderers 필드를 찾지 못했습니다.");
                return;
            }
            if (!p.boolValue)
            {
                p.boolValue = true;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(spaceView);
                report.Linked("MemoryRoomSpaceView._hideStructureRenderers → true (절차적 벽이 시계탑 아트 뒤로 안 비치게)");
            }
        }

        // ── 스프라이트 레이어 한 장 ────────────────────────────────────────
        private static Transform BuildLayer(Transform roomArt, Layer layer, Material unlit, SceneSetupReport report)
        {
            var sprite = LoadSprite(layer.Guid, layer.Name, report);
            if (sprite == null)
                return null;

            var existing = FindImmediateChild(roomArt, layer.Name);
            var created = existing == null;
            GameObject go;
            if (created)
            {
                go = new GameObject(layer.Name);
                Undo.RegisterCreatedObjectUndo(go, UndoGroupName);
                go.transform.SetParent(roomArt, worldPositionStays: false);
                go.transform.localPosition = Vector3.zero;   // 전 레이어 동일 피벗(중앙) → localPos 0에서 정렬
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one;
            }
            else
            {
                go = existing.gameObject;
            }

            var sr = GetOrAdd<SpriteRenderer>(go);
            Undo.RecordObject(sr, UndoGroupName);
            sr.sprite = sprite;
            sr.sortingOrder = layer.Order;
            if (layer.Unlit && unlit != null)
                sr.sharedMaterial = unlit;
            EditorUtility.SetDirty(sr);

            report.Linked(created
                ? $"{layer.Name} 생성 (order {layer.Order}{(layer.Unlit ? ", Unlit" : "")})"
                : $"{layer.Name} 값 재적용 (order {layer.Order})");
            return go.transform;
        }

        // ── 시침·분침 — Clock 자식, 회전축은 허브 / 텍스처는 다른 레이어와 정렬 ──
        private static void BuildClockHands(Transform clock, Material unlit, SceneSetupReport report)
        {
            if (clock == null)
            {
                report.Problem("Clock 레이어가 없어 바늘을 배치하지 못했습니다.");
                return;
            }

            var hour = BuildHand(clock, "Hour", HourGuid, HourOrder, unlit, report);
            var min = BuildHand(clock, "Min", MinGuid, MinOrder, unlit, report);

            var hands = clock.GetComponent<ClockHands>();
            var created = hands == null;
            if (created)
                hands = Undo.AddComponent<ClockHands>(clock.gameObject);

            var wired = 0;
            if (hour != null && SerializedFieldBinder.BindObject(hands, "_hourHand", hour, report)) wired++;
            if (min != null && SerializedFieldBinder.BindObject(hands, "_minuteHand", min, report)) wired++;
            // 분침 6도 스냅(60분할), 시침 연속 — 그린룸 시계와 같은 규칙(컴포넌트 기본값과 동일하나 명시).
            var so = new SerializedObject(hands);
            SetFloat(so, "_minuteSnapDegrees", 6f);
            SetFloat(so, "_hourSnapDegrees", 0f);
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(hands);

            report.Linked(created ? $"Clock에 ClockHands (참조 {wired})" : $"ClockHands 재적용 ({wired})");
        }

        private static Transform BuildHand(
            Transform clock, string name, string guid, int order, Material unlit, SceneSetupReport report)
        {
            var sprite = LoadSprite(guid, name, report);
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
                go.transform.localPosition = HandTextureAlign; // 허브 피벗 보정 — 텍스처를 다른 레이어에 맞춘다
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one;
            }
            else
            {
                go = existing.gameObject;
            }

            var sr = GetOrAdd<SpriteRenderer>(go);
            Undo.RecordObject(sr, UndoGroupName);
            sr.sprite = sprite;
            sr.sortingOrder = order;
            if (unlit != null)
                sr.sharedMaterial = unlit;
            EditorUtility.SetDirty(sr);

            report.Linked(created ? $"Clock/{name} 생성 (order {order})" : $"Clock/{name} 재적용");
            return go.transform;
        }

        // ── 밤 Global — RoomArt_room2 자식이라 방 전환에 맞춰 같이 꺼진다 ──────
        private static void BuildNightGlobal(Transform roomArt, SceneSetupReport report)
        {
            var light = EnsureLight("Night Global Light 2D", roomArt, Vector3.zero, out var created);
            Undo.RecordObject(light, UndoGroupName);
            light.lightType = Light2D.LightType.Global;
            light.color = NightGlobalTint;
            light.intensity = NightGlobalIntensity;
            light.blendStyleIndex = 0;
            EditorUtility.SetDirty(light);
            report.Linked(created
                ? $"RoomArt_room2/Night Global Light 2D 생성 (intensity {NightGlobalIntensity})"
                : $"Night Global Light 2D 재적용 (intensity {NightGlobalIntensity})");
        }

        // ── 시계면 주광 (Clock 자식, 위치는 허브) + 느린 맥동 ──────────────────
        private static void BuildClockLight(Transform clock, SceneSetupReport report)
        {
            if (clock == null)
            {
                report.Problem("Clock 레이어가 없어 주광을 만들지 못했습니다.");
                return;
            }

            var light = EnsureLight("Clock Light", clock, HubOffsetFromCenter, out var created);
            Undo.RecordObject(light, UndoGroupName);
            light.lightType = Light2D.LightType.Point;
            light.color = ClockLightTint;
            light.intensity = ClockLightIntensity;
            light.pointLightInnerRadius = ClockLightInnerRadius;
            light.pointLightOuterRadius = ClockLightOuterRadius;
            light.falloffIntensity = ClockLightFalloff;
            light.shadowsEnabled = true;
            light.shadowIntensity = ClockLightShadowIntensity;
            light.shadowSoftness = 0f;
            light.overlapOperation = Light2D.OverlapOperation.Additive;
            light.blendStyleIndex = 0;
            EditorUtility.SetDirty(light);

            var flicker = GetOrAdd<Light2DFlicker>(light.gameObject);
            Undo.RecordObject(flicker, UndoGroupName);
            var so = new SerializedObject(flicker);
            SetBool(so, "_enabled", FlickerEnabled);
            SetFloat(so, "_amplitude", FlickerAmplitude);
            SetFloat(so, "_speed", FlickerSpeed);
            SetFloat(so, "_intervalMinSeconds", FlickerIntervalSeconds);
            SetFloat(so, "_intervalMaxSeconds", FlickerIntervalSeconds);
            SetFloat(so, "_spriteFlickerScale", FlickerSpriteScale);
            SetFloat(so, "_spriteMinFactor", FlickerSpriteMinFactor);
            so.ApplyModifiedProperties();

            // 시계면 스프라이트(Unlit)도 호흡을 살짝 따라가게 — 빛만 맥동하는 어색함 완화.
            var clockSr = clock.GetComponent<SpriteRenderer>();
            if (clockSr != null)
                SerializedFieldBinder.BindObject(flicker, "_spriteRenderer", clockSr, report);
            EditorUtility.SetDirty(flicker);

            report.Linked(created
                ? $"Clock/Clock Light 생성 (Point, intensity {ClockLightIntensity}, outer {ClockLightOuterRadius}) + 느린 맥동"
                : "Clock Light + Light2DFlicker 재적용");
        }

        // ── ShadowCaster 2D — 시계 빛에 인물·난간 실루엣이 아래로 드리움 ────────
        private static void ApplyShadowCaster(Transform roomArt, string spriteName, SceneSetupReport report)
        {
            var target = FindImmediateChild(roomArt, spriteName);
            if (target == null)
            {
                report.Problem($"{spriteName} 레이어가 없어 그림자 캐스터를 붙이지 못했습니다.");
                return;
            }

            var caster = target.GetComponent<ShadowCaster2D>();
            var created = caster == null;
            if (created)
                caster = Undo.AddComponent<ShadowCaster2D>(target.gameObject);

            Undo.RecordObject(caster, UndoGroupName);
            caster.castingOption = ShadowCaster2D.ShadowCastingOptions.CastShadow;

            // ── 그린룸에서 겪은 trimEdge 함정 ───────────────────────────────
            // SpriteRenderer 프로바이더는 트림 폭을 "스프라이트 전체 bounds"(4.6x2.3)의
            // 5%(≈0.115유닛 ≈ 11px)로 잡는다. 방 2 인물은 프레임 안 22px 폭 형상이라
            // 11px를 깎으면 실루엣이 통째로 사라져 그림자 메쉬가 빈다. 0으로 되돌린다 —
            // 픽셀아트 그림자는 DotTexture가 어차피 포스터라이즈하므로 거친 윤곽이면 족하다.
            var vtx = 0;
            var boundR = 0f;
            try
            {
                caster.trimEdge = 0f;
                caster.enabled = false;
                caster.enabled = true;
                caster.Update();
                vtx = caster.mesh != null ? caster.mesh.vertexCount : 0;
                boundR = caster.boundingSphere.radius;
            }
            catch (System.NullReferenceException) { /* 메쉬 미생성 — 다음 프레임 Update가 만든다 */ }

            var serialized = new SerializedObject(caster);
            var layers = serialized.FindProperty("m_ApplyToSortingLayers");
            if (layers != null && layers.isArray)
            {
                layers.arraySize = 1;
                layers.GetArrayElementAtIndex(0).intValue = 0; // Default
                serialized.ApplyModifiedProperties();
            }
            EditorUtility.SetDirty(caster);

            var source = serialized.FindProperty("m_ShadowCastingSource");
            var diag = $" [source={source?.intValue}, meshVtx={vtx}, boundR={boundR:0.00}]";
            report.Linked(created
                ? $"{spriteName}에 ShadowCaster2D (trimEdge=0){diag}"
                : $"{spriteName} ShadowCaster2D 재적용 (trimEdge=0){diag}");
        }

        // ── 벌레 — 시계 빛 주위를 맴돈다 ───────────────────────────────────
        private static void BuildFlies(Transform roomArt, Material unlit, SceneSetupReport report)
        {
            var existing = FindImmediateChild(roomArt, "Fly");
            var created = existing == null;
            GameObject go;
            if (created)
            {
                go = new GameObject("Fly");
                Undo.RegisterCreatedObjectUndo(go, UndoGroupName);
                go.transform.SetParent(roomArt, worldPositionStays: false);
                go.transform.localPosition = Vector3.zero;
                go.transform.localScale = Vector3.one;
            }
            else
            {
                go = existing.gameObject;
            }

            var wander = GetOrAdd<FlyWander>(go);
            Undo.RecordObject(wander, UndoGroupName);
            var so = new SerializedObject(wander);
            SetBool(so, "_enabled", true);
            SetInt(so, "_count", FlyCount);
            // 무리 중심 = 시계 허브(컨테이너 로컬). 빛 주위를 맴도는 것으로 읽힌다.
            var c = so.FindProperty("_center");
            if (c != null) c.vector2Value = new Vector2(HubOffsetFromCenter.x, HubOffsetFromCenter.y);
            SetFloat(so, "_radiusPx", FlyRadiusPx);
            SetFloat(so, "_speed", FlySpeed);
            SetInt(so, "_sortingOrder", FlyOrder);
            so.ApplyModifiedProperties();
            if (unlit != null)
                SerializedFieldBinder.BindObject(wander, "_unlitMaterial", unlit, report);
            EditorUtility.SetDirty(wander);

            report.Linked(created ? $"RoomArt_room2/Fly 생성 (FlyWander, {FlyCount}마리, order {FlyOrder})" : "Fly/FlyWander 재적용");
        }

        // ── 다른 Global Light가 방 2에서 꺼지도록 배선 ─────────────────────────
        // RoomArtSwitcher는 목록의 오브젝트를 SetActive만 한다. URP 2D는 blend
        // style당 Global Light 하나만 쓰고 나머지엔 경고를 뱉는다. 밤 Global은
        // RoomArt_room2 자식이라 방 2에서만 켜지지만, 방 1용(또는 씬 상주) Global이
        // 항상 켜져 있으면 방 2에서 둘이 겹친다. 그래서 RoomArt_room2 밖에 있는
        // 모든 Global Light를 스위처의 room-1 항목에 넣어 방 2로 넘어갈 때 꺼지게
        // 한다(그린룸 씬에서 검증된 패턴 — Global Light가 Room-1 Objects 멤버). 방 1
        // 아트·라이트 값은 안 건드리고, 스위처 토글 목록에 등록만 한다.
        private static void WireRoom1GlobalToggle(Transform roomArt, Transform motion, SceneSetupReport report)
        {
            var switcher = Object.FindFirstObjectByType<RoomArtSwitcher>(FindObjectsInactive.Include);
            if (switcher == null)
            {
                report.Problem("RoomArtSwitcher를 찾지 못했습니다.");
                return;
            }

            // 방 2 컨테이너(고정·흔들림) 서브트리 '밖'에 있는 Global Light 전부
            // (밤 Global은 RoomArt_room2 자식이라 자연히 제외됨).
            var others = Object.FindObjectsByType<Light2D>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(l => l.lightType == Light2D.LightType.Global
                            && !l.transform.IsChildOf(roomArt)
                            && !l.transform.IsChildOf(motion))
                .Select(l => l.gameObject)
                .ToList();
            if (others.Count == 0)
            {
                report.Linked("RoomArt_room2 밖에 Global Light가 없습니다 — 방 1 토글 배선 불필요.");
                return;
            }

            var so = new SerializedObject(switcher);
            var rooms = so.FindProperty("_rooms");
            if (rooms == null || !rooms.isArray)
            {
                report.Problem("RoomArtSwitcher._rooms를 찾지 못했습니다.");
                return;
            }

            // room-1 항목을 찾거나 만든다.
            SerializedProperty entry = null;
            for (var i = 0; i < rooms.arraySize; i++)
            {
                var id = rooms.GetArrayElementAtIndex(i).FindPropertyRelative("RoomId").stringValue ?? "";
                if (id.Trim().ToLowerInvariant() == "room-1")
                {
                    entry = rooms.GetArrayElementAtIndex(i);
                    break;
                }
            }

            var createdEntry = entry == null;
            if (createdEntry)
            {
                rooms.arraySize++;
                entry = rooms.GetArrayElementAtIndex(rooms.arraySize - 1);
                entry.FindPropertyRelative("RoomId").stringValue = "room-1";
                entry.FindPropertyRelative("Objects").arraySize = 0;
            }

            var objs = entry.FindPropertyRelative("Objects");
            var added = 0;
            foreach (var go in others)
            {
                var present = false;
                for (var k = 0; k < objs.arraySize; k++)
                    if (objs.GetArrayElementAtIndex(k).objectReferenceValue == go) { present = true; break; }
                if (present)
                    continue;

                objs.arraySize++;
                objs.GetArrayElementAtIndex(objs.arraySize - 1).objectReferenceValue = go;
                added++;
            }

            if (createdEntry || added > 0)
            {
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(switcher);
                report.Linked($"RoomArtSwitcher room-1 항목{(createdEntry ? " 생성" : "")}에 Global Light {added}개 등록 " +
                    $"({string.Join(", ", others.Select(g => g.name))}) — 방 2에서 겹치지 않도록");
            }
            else
            {
                report.Linked("바깥 Global Light가 이미 RoomArtSwitcher room-1에 등록돼 있습니다.");
            }
        }

        // ── 스위처 room-2 항목에 고정·흔들림 컨테이너 둘 다 등록 ────────────────
        // 예전엔 RoomArt_room2 하나만 껐다 켰다. 콘텐츠가 Space 밑 별도 컨테이너로
        // 빠졌으니, 방 전환 때 둘이 함께 켜지고 꺼져야 한다.
        private static void WireRoom2Toggle(Transform roomArt, Transform motion, SceneSetupReport report)
        {
            var switcher = Object.FindFirstObjectByType<RoomArtSwitcher>(FindObjectsInactive.Include);
            if (switcher == null)
            {
                report.Problem("RoomArtSwitcher를 찾지 못했습니다.");
                return;
            }

            var so = new SerializedObject(switcher);
            var rooms = so.FindProperty("_rooms");
            if (rooms == null || !rooms.isArray)
            {
                report.Problem("RoomArtSwitcher._rooms를 찾지 못했습니다.");
                return;
            }

            SerializedProperty entry = null;
            for (var i = 0; i < rooms.arraySize; i++)
            {
                var id = (rooms.GetArrayElementAtIndex(i).FindPropertyRelative("RoomId").stringValue ?? "")
                    .Trim().ToLowerInvariant();
                if (id == "room-2")
                {
                    entry = rooms.GetArrayElementAtIndex(i);
                    break;
                }
            }

            var createdEntry = entry == null;
            if (createdEntry)
            {
                rooms.arraySize++;
                entry = rooms.GetArrayElementAtIndex(rooms.arraySize - 1);
                entry.FindPropertyRelative("RoomId").stringValue = "room-2";
                entry.FindPropertyRelative("Objects").arraySize = 0;
            }

            var objs = entry.FindPropertyRelative("Objects");
            var want = new[] { roomArt.gameObject, motion.gameObject };
            var added = 0;
            foreach (var go in want)
            {
                var present = false;
                for (var k = 0; k < objs.arraySize; k++)
                    if (objs.GetArrayElementAtIndex(k).objectReferenceValue == go) { present = true; break; }
                if (present)
                    continue;

                objs.arraySize++;
                objs.GetArrayElementAtIndex(objs.arraySize - 1).objectReferenceValue = go;
                added++;
            }

            if (createdEntry || added > 0)
            {
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(switcher);
                report.Linked($"RoomArtSwitcher room-2 항목{(createdEntry ? " 생성" : "")}에 컨테이너 {added}개 등록 " +
                    $"({RoomArtName}, {MotionArtName})");
            }
            else
            {
                report.Linked($"RoomArtSwitcher room-2에 두 컨테이너가 이미 등록돼 있습니다.");
            }
        }

        // ── 흔들리는 콘텐츠 컨테이너 (Space 자식) ──────────────────────────────
        private static Transform EnsureMotionContainer(Transform space, SceneSetupReport report)
        {
            var existing = FindImmediateChild(space, MotionArtName);
            if (existing != null)
                return existing;

            var go = new GameObject(MotionArtName);
            Undo.RegisterCreatedObjectUndo(go, UndoGroupName);
            go.transform.SetParent(space, worldPositionStays: false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            report.Created($"{space.name}/{MotionArtName} (흔들리는 시계탑 콘텐츠 컨테이너)");
            return go.transform;
        }

        // 레이어가 예전 컨테이너에 있으면 새 컨테이너로 옮긴다(월드 위치 보존 —
        // 두 컨테이너는 같은 트랜스폼으로 정규화돼 있어 로컬 오프셋도 그대로다).
        private static void MigrateChild(Transform fromParent, Transform toParent, string name, SceneSetupReport report)
        {
            if (fromParent == null || toParent == null)
                return;
            var child = FindImmediateChild(fromParent, name);
            if (child == null)
                return;

            Undo.SetTransformParent(child, toParent, $"{UndoGroupName} — {name} 이동");
            report.Linked($"{name}: {fromParent.name} → {toParent.name} 로 이동");
        }

        // ── 공통 ────────────────────────────────────────────────────────
        private static void SetFloat(SerializedObject so, string path, float v)
        {
            var p = so.FindProperty(path);
            if (p != null) p.floatValue = v;
        }

        private static void SetInt(SerializedObject so, string path, int v)
        {
            var p = so.FindProperty(path);
            if (p != null) p.intValue = v;
        }

        private static void SetBool(SerializedObject so, string path, bool v)
        {
            var p = so.FindProperty(path);
            if (p != null) p.boolValue = v;
        }

        // get-or-add는 명시적으로 — `??`는 C# 참조 비교라 Unity의 오버로드된 null을
        // 못 걸러 AddComponent를 건너뛸 수 있다(그린룸 빌더와 같은 관례).
        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            return c != null ? c : Undo.AddComponent<T>(go);
        }

        private static Light2D EnsureLight(string name, Transform parent, Vector3 localPos, out bool created)
        {
            var existing = FindImmediateChild(parent, name);
            if (existing != null)
            {
                created = false;
                return GetOrAdd<Light2D>(existing.gameObject);
            }

            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, UndoGroupName);
            go.transform.SetParent(parent, worldPositionStays: false);
            go.transform.localPosition = localPos;
            created = true;
            return Undo.AddComponent<Light2D>(go);
        }

        private static Sprite LoadSprite(string guid, string label, SceneSetupReport report)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var sprite = string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                report.Problem($"{label} 스프라이트를 찾지 못했습니다(임포트 전이면 저장 후 다시 실행).");
            return sprite;
        }

        private static Material LoadUnlitMaterial(SceneSetupReport report)
        {
            var path = AssetDatabase.GUIDToAssetPath(UnlitMaterialGuid);
            var mat = string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
                report.Problem("Sprite-Unlit-Default 머티리얼을 찾지 못했습니다. 시계면·발광·바늘 Unlit 교체를 건너뜁니다.");
            return mat;
        }

        // 활성 씬 전체에서 이름으로 Transform을 찾는다(비활성 포함).
        private static Transform FindInScene(string name)
        {
            foreach (var root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
            {
                var found = root.GetComponentsInChildren<Transform>(includeInactive: true)
                    .FirstOrDefault(t => t.name == name);
                if (found != null)
                    return found;
            }
            return null;
        }

        private static Transform FindImmediateChild(Transform parent, string name)
        {
            for (var i = 0; i < parent.childCount; i++)
                if (parent.GetChild(i).name == name)
                    return parent.GetChild(i);
            return null;
        }

        private static void Present(string sceneName, SceneSetupReport report)
        {
            var summary = report.Summarize();
            Debug.Log($"[{UndoGroupName}] 씬: {sceneName}\n{summary}");
            var title = report.HasProblems ? "시계탑 방 구성 — 확인 필요" : "시계탑 방 구성 완료";
            EditorUtility.DisplayDialog(title, summary, "확인");
        }
    }
}
