using System.Linq;
using GameName.UI.MemoryRoom;
using GameName.UI.MemoryRoom.Space;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GameName.UI.Editor.SceneSetup
{
    // 기억 방 픽셀아트 레이어 위에 URP 2D 조명 리그를 얹는 도구.
    //
    // 스프라이트(BG·Window·Clock·Lamp·LeftPerson·RightPerson·Things·Shadow)는
    // 손으로 배치한 씬 콘텐츠라 메인 "기억 방 씬 구성" 흐름에 넣지 않고 별도
    // 메뉴로 둔다. 게임 규칙·SO·마스크 셰이더(DotTexture)는 하나도 건드리지 않는다 —
    // 이 도구가 만드는 것은 Light2D 오브젝트와 ShadowCaster2D, 그리고 광원 스프라이트의
    // 머티리얼 교체뿐이다.
    //
    // ── 여러 번 돌려도 안전하다 ─────────────────────────────────────────
    // 오브젝트는 이름으로 찾아 없을 때만 만들지만, 라이트 수치는 "이 도구가 정하는
    // 프리셋"이라 **재실행할 때마다 아래 상수 값으로 되돌린다**. 조명을 조율하는
    // 단계에서는 상수를 고치고 메뉴를 다시 누르는 흐름이 가장 빠르기 때문이다.
    // 위치(localPosition)만은 만들 때 한 번만 잡고 이후엔 건드리지 않는다 —
    // 전구·시계면에 맞춰 손으로 미세조정한 것을 지우지 않기 위함이다.
    // 되돌리기는 한 번의 Ctrl+Z(단, Play 진입 후에는 오브젝트를 직접 지워야 한다).
    //
    // ── 흔들림(CameraShake)과의 관계 ────────────────────────────────────
    // CameraShake._shakeTarget은 Space 루트다. 기능 라이트(램프·창·시계)는 아트의
    // 특정 지점(전구·창틀·시계면)에 붙은 빛이라 각 스프라이트의 자식으로 둔다 —
    // 방이 흔들리는데 빛 웅덩이가 제자리에 있으면 렌더 버그처럼 보인다. Global
    // Light는 균일 필이라 위치 의존이 없어 MemoryRoomScreen 직속(고정, BG/Shadow와
    // 같은 레이어)에 둔다.
    public static class MemoryRoomLightingSceneBuilder
    {
        private const string MenuPath = "GameName/기억 방 조명 구성";
        private const string UndoGroupName = "기억 방 조명 구성";

        // 광원 자신이 어두워지면 안 되는 스프라이트가 갈아탈 머티리얼(Sprite-Unlit-Default).
        private const string UnlitMaterialGuid = "9dfc825aed78fcd4ba02077103263b40";

        // ── 라이트 프리셋 (재실행할 때마다 되돌아가는 값) ──────────────────
        // 전부 인스펙터가 아니라 여기서 조율한다. 반경은 방 길이(_roomLength ≈ 2.42,
        // 반폭 ≈ 1.21)에 견줘 읽으면 된다.
        //
        // 2D 그림자는 "그 라이트의 기여분"만 깎는다 — 전역 필이 세면 그림자 진 곳을
        // 도로 채워 안 보인다. 그래서 램프(주광)를 세게, Global을 낮게 잡아 대비를 만든다.

        private static readonly Color GlobalTint = new Color(0.60f, 0.80f, 0.82f);
        private const float GlobalIntensity = 0.18f; // 순수 검정만 면하는 최소 필 (그림자가 묻히지 않게 낮게)

        private static readonly Color LampTint = new Color(0.62f, 0.95f, 0.90f);
        private const float LampIntensity = 1.5f;
        private const float LampInnerRadius = 0.35f;   // 테이블+양쪽 인물을 덮는 밝은 코어
        private const float LampOuterRadius = 1.90f;   // 여유 있게 양쪽 인물까지 — falloff가 가장자리를 어둡게 함
        private const float LampFalloff = 0.55f;        // 0.8은 너무 급해 인물에 닿기 전에 빛이 죽었다
        private const float LampShadowIntensity = 1.0f; // 그림자 진 곳에서 램프 기여분을 완전히 제거

        // 오래된 백열구 밝기 흔들림(Light2DFlicker).
        //  · amplitude를 크게(0.28) — DotTexture 10단계 포스터라이즈 계단(≈10%)을 넘겨야 화면에서 보임.
        //  · dip은 사각파 하드 컷 — depth 0.95(거의 꺼짐)로 duration 0.08초(짧게 "툭"),
        //    1~dipBurstMax번 연속 깜빡여 더 오래된 전구처럼.
        //  · spriteMinFactor 0 — dip에 코어까지 거의 꺼져 블룸 글로우도 함께 빠지게.
        private const bool FlickerEnabled = true;
        private const float FlickerAmplitude = 0.28f;
        private const float FlickerSpeed = 0.9f;
        private const float FlickerSpriteScale = 0.6f;
        private const float FlickerSpriteMinFactor = 0f;
        private const float FlickerDipDepth = 0.95f;
        private const float FlickerDipDuration = 0.08f;
        // 5~8초마다 "깜빡깜빡"(2회 껐다켜기), 그게 3번 반복되면 정전.
        // 무작위는 대기 시간뿐이고 횟수·정전 시점은 확정적이다.
        private const float FlickerIntervalMinSeconds = 5f;
        private const float FlickerIntervalMaxSeconds = 8f;
        private const int FlickerBlinksPerFlicker = 2;
        private const int FlickerFlickersBeforeBlackout = 3;

        // 아주 가끔(dip 중 15%) 5연속 깜빡 → 2.5초 암전 → 2초에 걸쳐 서서히 복귀.
        private const float FlickerBlackoutDepth = 1f; // 1 = 완전 암전(라이트+코어 전부 0)
        private const float FlickerBlackoutDuration = 2.5f;
        private const float FlickerRecoveryDuration = 2f;

        private static readonly Color WindowTint = new Color(0.50f, 0.70f, 0.72f);
        private const float WindowIntensity = 0.35f;
        private const float WindowFalloffSize = 0.5f;

        private static readonly Color ClockTint = new Color(0.58f, 0.85f, 0.85f);
        private const float ClockIntensity = 0.25f;
        private const float ClockInnerRadius = 0.04f;
        private const float ClockOuterRadius = 0.35f;
        private const float ClockFalloff = 0.6f;

        [MenuItem(MenuPath)]
        public static void Run()
        {
            var scene = EditorSceneManager.GetActiveScene();
            var report = new SceneSetupReport();

            var undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoGroupName);

            var screen = Object.FindFirstObjectByType<MemoryRoomBootstrap>(FindObjectsInactive.Include);
            if (screen == null)
            {
                report.Problem("MemoryRoomBootstrap을 찾지 못했습니다. 기억 방 화면이 있는 씬에서 실행하세요.");
                Present(scene.name, report);
                return;
            }

            var screenRoot = screen.transform;

            // 각 단계를 격리한다 — 한 단계가 예외를 던져도 나머지는 돌고, 완료
            // 다이얼로그·씬 더티·되돌리기 병합은 항상 실행된다. 예전엔 한 곳에서
            // 던지면 그 뒤 전부(다이얼로그 포함)가 건너뛰어져 무엇이 됐는지 알기 어려웠다.
            var unlit = LoadUnlitMaterial(report);

            Step("Global Light", () => BuildGlobalLight(screenRoot, report), report);
            Step("Lamp Light", () => BuildLampLight(screenRoot, report), report);
            Step("Window Light", () => BuildWindowLight(screenRoot, report), report);
            Step("Clock Light", () => BuildClockLight(screenRoot, report), report);
            Step("Lamp Flicker", () => ApplyLampFlicker(screenRoot, report), report);

            foreach (var caster in new[] { "LeftPerson", "RightPerson", "Things" })
                Step($"{caster} ShadowCaster", () => ApplyShadowCaster(screenRoot, caster, report), report);

            if (unlit != null)
                foreach (var lightSprite in new[] { "Lamp", "Clock" })
                    Step($"{lightSprite} Unlit", () => SwapToUnlit(screenRoot, lightSprite, unlit, report), report);

            Step("Smoke", () => ApplySmoke(screenRoot, unlit, report), report);
            Step("Shadow 비네트", () => DisableShadowVignette(screenRoot, unlit, report), report);

            EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(undoGroup);
            Present(scene.name, report);
        }

        // 한 단계를 감싸 예외를 붙잡아 리포트로 돌린다.
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

        // ── A. Global Light 2D — 고정 레이어(MemoryRoomScreen 직속) ─────────
        private static void BuildGlobalLight(Transform screenRoot, SceneSetupReport report)
        {
            var light = EnsureLight("Global Light 2D", screenRoot, Vector3.zero, out var created);

            Undo.RecordObject(light, UndoGroupName);
            light.lightType = Light2D.LightType.Global;
            light.color = GlobalTint;
            light.intensity = GlobalIntensity;
            light.blendStyleIndex = 0;
            EditorUtility.SetDirty(light);

            report.Linked(created ? "Global Light 2D 생성" : $"Global Light 2D 값 재적용 (intensity {GlobalIntensity})");
        }

        // ── B. 펜던트 램프 — 주광 Point Light (Lamp 자식, 흔들림 탑승) ─────
        private static void BuildLampLight(Transform screenRoot, SceneSetupReport report)
        {
            var lamp = FindChild(screenRoot, "Lamp");
            if (lamp == null)
            {
                report.Problem("Lamp 스프라이트를 찾지 못해 주광을 만들지 못했습니다.");
                return;
            }

            // 320x180 프레임에서 전구 글로우는 중앙 약간 아래 — 만들 때만 잡는 씨앗.
            var light = EnsureLight("Lamp Light", lamp, new Vector3(0f, -0.04f, 0f), out var created);

            Undo.RecordObject(light, UndoGroupName);
            light.lightType = Light2D.LightType.Point;
            light.color = LampTint;
            light.intensity = LampIntensity;
            light.pointLightInnerRadius = LampInnerRadius;
            light.pointLightOuterRadius = LampOuterRadius;
            light.falloffIntensity = LampFalloff;
            light.shadowsEnabled = true;
            light.shadowIntensity = LampShadowIntensity;
            light.shadowSoftness = 0f; // 그림자 경계도 계단으로
            light.overlapOperation = Light2D.OverlapOperation.Additive;
            light.blendStyleIndex = 0;
            EditorUtility.SetDirty(light);

            report.Linked(created
                ? "Lamp/Lamp Light 생성"
                : $"Lamp Light 값 재적용 (outer {LampOuterRadius}, intensity {LampIntensity}, shadowIntensity {LampShadowIntensity})");
        }

        // ── C. 창 — Freeform Light (Window 자식) ──────────────────────────
        private static void BuildWindowLight(Transform screenRoot, SceneSetupReport report)
        {
            var window = FindChild(screenRoot, "Window");
            if (window == null)
            {
                report.Problem("Window 스프라이트를 찾지 못해 창 라이트를 만들지 못했습니다.");
                return;
            }

            var light = EnsureLight("Window Light", window, Vector3.zero, out var created);

            Undo.RecordObject(light, UndoGroupName);
            light.lightType = Light2D.LightType.Freeform;
            light.color = WindowTint;
            light.intensity = WindowIntensity;
            light.shapeLightFalloffSize = WindowFalloffSize;
            light.shadowsEnabled = false;
            light.blendStyleIndex = 0;

            // 창 모양 평행사변형은 만들 때만 씨앗을 넣는다 — 정확한 윤곽은 실행 후
            // Freeform 편집 툴로 그림의 사선에 맞추므로, 재실행이 그걸 지우면 안 된다.
            if (created)
            {
                light.SetShapePath(new[]
                {
                    new Vector3(-1.00f, -0.40f, 0f),
                    new Vector3( 1.00f, -0.40f, 0f),
                    new Vector3( 1.15f,  0.60f, 0f),
                    new Vector3(-0.85f,  0.60f, 0f),
                });
                ForceShapeMeshRebuild(light);
            }

            EditorUtility.SetDirty(light);
            report.Linked(created ? "Window/Window Light 생성" : "Window Light 값 재적용 (셰이프는 유지)");
        }

        // ── D. 시계 — 아주 약한 Point Light (Clock 자식) ─────────────────
        private static void BuildClockLight(Transform screenRoot, SceneSetupReport report)
        {
            var clock = FindChild(screenRoot, "Clock");
            if (clock == null)
            {
                report.Problem("Clock 스프라이트를 찾지 못해 시계 라이트를 만들지 못했습니다.");
                return;
            }

            var light = EnsureLight("Clock Light", clock, new Vector3(0f, 0.68f, 0f), out var created);

            Undo.RecordObject(light, UndoGroupName);
            light.lightType = Light2D.LightType.Point;
            light.color = ClockTint;
            light.intensity = ClockIntensity;
            light.pointLightInnerRadius = ClockInnerRadius;
            light.pointLightOuterRadius = ClockOuterRadius;
            light.falloffIntensity = ClockFalloff;
            light.shadowsEnabled = false;
            light.blendStyleIndex = 0;
            EditorUtility.SetDirty(light);

            report.Linked(created ? "Clock/Clock Light 생성" : "Clock Light 값 재적용");
        }

        // ── E-1. 램프 플리커 (Light2DFlicker on Lamp Light) ────────────────
        private static void ApplyLampFlicker(Transform screenRoot, SceneSetupReport report)
        {
            var lampLight = FindChild(screenRoot, "Lamp Light");
            if (lampLight == null)
            {
                report.Problem("Lamp Light를 찾지 못해 플리커를 붙이지 못했습니다.");
                return;
            }

            var flicker = lampLight.GetComponent<Light2DFlicker>();
            var created = flicker == null;
            if (created)
                flicker = Undo.AddComponent<Light2DFlicker>(lampLight.gameObject);

            Undo.RecordObject(flicker, UndoGroupName);
            var so = new SerializedObject(flicker);
            var enabledProp = so.FindProperty("_enabled");
            if (enabledProp != null) enabledProp.boolValue = FlickerEnabled;
            SetFloat(so, "_amplitude", FlickerAmplitude);
            SetFloat(so, "_speed", FlickerSpeed);
            SetFloat(so, "_spriteFlickerScale", FlickerSpriteScale);
            SetFloat(so, "_spriteMinFactor", FlickerSpriteMinFactor);
            SetFloat(so, "_dipDepth", FlickerDipDepth);
            SetFloat(so, "_dipDuration", FlickerDipDuration);
            SetFloat(so, "_intervalMinSeconds", FlickerIntervalMinSeconds);
            SetFloat(so, "_intervalMaxSeconds", FlickerIntervalMaxSeconds);
            SetInt(so, "_blinksPerFlicker", FlickerBlinksPerFlicker);
            SetInt(so, "_flickersBeforeBlackout", FlickerFlickersBeforeBlackout);
            SetFloat(so, "_blackoutDepth", FlickerBlackoutDepth);
            SetFloat(so, "_blackoutDuration", FlickerBlackoutDuration);
            SetFloat(so, "_recoveryDuration", FlickerRecoveryDuration);
            so.ApplyModifiedProperties();

            // 전구 코어(Lamp 스프라이트)도 숨쉬는 떨림을 따라가도록 레퍼런스 연결.
            var lampSprite = FindChild(screenRoot, "Lamp");
            var sr = lampSprite != null ? lampSprite.GetComponent<SpriteRenderer>() : null;
            if (sr != null)
                SerializedFieldBinder.BindObject(flicker, "_spriteRenderer", sr, report);

            EditorUtility.SetDirty(flicker);
            report.Linked(created ? "Lamp Light에 Light2DFlicker" : "Light2DFlicker 값 재적용");
        }

        private static void SetFloat(SerializedObject so, string propertyPath, float value)
        {
            var property = so.FindProperty(propertyPath);
            if (property != null)
                property.floatValue = value;
        }

        private static void SetInt(SerializedObject so, string propertyPath, int value)
        {
            var property = so.FindProperty(propertyPath);
            if (property != null)
                property.intValue = value;
        }

        private static void SetBool(SerializedObject so, string propertyPath, bool value)
        {
            var property = so.FindProperty(propertyPath);
            if (property != null)
                property.boolValue = value;
        }

        // ── E-2. 김 (Smoke.png + SmokeDrift, Things에서 분리) ──────────────
        // Things.png에 구워져 있던 정지 김 픽셀을 별도 스프라이트로 뽑아 두었다
        // (Assets/Sprites/Smoke.png, 원본 Things.png에서는 지워짐). 여기서 그 스프라이트를
        // Space 아래 GameObject로 올리고 SmokeDrift로 위로 올린다.
        private static void ApplySmoke(Transform screenRoot, Material unlit, SceneSetupReport report)
        {
            var space = FindChild(screenRoot, "Space");
            var things = FindChild(screenRoot, "Things");
            if (space == null || things == null)
            {
                report.Problem("Space/Things를 찾지 못해 김을 배치하지 못했습니다.");
                return;
            }

            var smokeSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Smoke.png");
            if (smokeSprite == null)
            {
                report.Problem("Assets/Sprites/Smoke.png을 찾지 못했습니다(임포트 전이면 다시 실행).");
                return;
            }

            var existing = FindChild(screenRoot, "Smoke");
            var created = existing == null;
            GameObject go;
            if (created)
            {
                go = new GameObject("Smoke");
                Undo.RegisterCreatedObjectUndo(go, UndoGroupName);
                go.transform.SetParent(space, worldPositionStays: false);
            }
            else
            {
                go = existing.gameObject;
            }

            // 부모·위치는 생성 시뿐 아니라 매번 다시 맞춘다 — 예전 SmokeDrift
            // 버전들이 Transform을 직접 움직이던 시절의 값이 씬 파일에 그대로
            // 남아 있을 수 있고(재실행해도 "생성 때만 세팅"이면 그 잘못된 값이
            // 영영 안 고쳐진다), 지금 버전은 Transform을 절대 안 옮기므로 여기서
            // 한 번 확실히 Things와 같은 자리로 되돌려 둔다.
            if (go.transform.parent != space)
                go.transform.SetParent(space, worldPositionStays: false);

            Undo.RecordObject(go.transform, UndoGroupName);
            go.transform.localPosition = things.localPosition;

            // get-or-add는 반드시 명시적으로 — `??`는 C# 참조 비교라 Unity의 오버로드된
            // null(빈 컴포넌트)을 못 걸러 AddComponent를 건너뛴다. 새 GameObject엔
            // Transform뿐이라 SpriteRenderer를 여기서 붙여야 한다.
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null)
                sr = Undo.AddComponent<SpriteRenderer>(go);
            if (sr == null)
            {
                report.Problem("Smoke GameObject에 SpriteRenderer를 붙이지 못했습니다.");
                return;
            }

            Undo.RecordObject(sr, UndoGroupName);
            sr.sprite = smokeSprite;
            sr.sortingOrder = -39; // Things(-40) 바로 앞
            if (unlit != null)
                sr.sharedMaterial = unlit; // 옅은 자체발광 김 — Lit면 어두운 방에서 묻힌다
            EditorUtility.SetDirty(sr);

            var drift = go.GetComponent<SmokeDrift>();
            if (drift == null)
                drift = Undo.AddComponent<SmokeDrift>(go);

            Undo.RecordObject(drift, UndoGroupName);
            var so = new SerializedObject(drift);
            SetBool(so, "_enabled", true);
            SetFloat(so, "_riseRowsPerSecond", 7f);
            SetInt(so, "_emitSourceRows", 8);  // 밑동만 잘라 내보낸다(덩어리 방지)
            SetInt(so, "_emitCyclePx", 2);     // 아주 촘촘히 = 연속적인 줄기
            SetInt(so, "_lifetimePx", 34);     // 완전히 사라질 때까지의 거리
            SetInt(so, "_fadeInPx", 2);
            SetInt(so, "_swayAmpPx", 5);
            SetFloat(so, "_swayFrequency", 0.07f); // 조각 경로가 꿈틀거리는 빈도
            SetFloat(so, "_spreadAmount", 1.8f);   // 올라갈수록 가로로 퍼지는 배율
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(drift);

            report.Linked(created
                ? "Space/Smoke 생성 (Smoke.png + SmokeDrift, order -39)"
                : "Smoke/SmokeDrift 값 재적용");
        }

        // ── E. Shadow Caster 2D ──────────────────────────────────────────
        private static void ApplyShadowCaster(Transform screenRoot, string spriteName, SceneSetupReport report)
        {
            var target = FindChild(screenRoot, spriteName);
            if (target == null)
            {
                report.Problem($"{spriteName} 스프라이트를 찾지 못해 그림자 캐스터를 붙이지 못했습니다.");
                return;
            }

            var caster = target.GetComponent<ShadowCaster2D>();
            var created = caster == null;
            if (created)
            {
                // 에디터에서 AddComponent로 붙어야 Awake에서 SpriteRenderer 실루엣
                // 프로바이더가 자동 선택된다(스프라이트에 fallback physics shape가 있음).
                caster = Undo.AddComponent<ShadowCaster2D>(target.gameObject);
            }

            Undo.RecordObject(caster, UndoGroupName);
            caster.castingOption = ShadowCaster2D.ShadowCastingOptions.CastShadow; // 캐스트만(자기그림자 off)

            // ── 그림자가 안 보이던 진짜 원인: trimEdge ──────────────────────
            // SpriteRenderer 프로바이더는 트림 폭을 "스프라이트 전체 bounds"(3.2×1.8)의
            // 5%(≈0.09 유닛)로 잡아 실루엣을 안쪽으로 깎는다. 그런데 실제 인물은 그
            // 프레임 안의 작은 형상(팔다리 폭 ≈0.1 유닛)이라, 0.09를 깎으면 팔다리가
            // 통째로 사라져 그림자 메쉬가 비어 버린다. 트림을 0으로 되돌린다 —
            // 픽셀아트 그림자는 어차피 DotTexture가 포스터라이즈하므로 거친 윤곽이면 족하다.
            // (m_ShadowMesh는 Awake에서 만들어지지만, 혹시 아직이면 Update()가 만든다.)
            var vtx = 0;
            var boundR = 0f;
            try
            {
                caster.trimEdge = 0f;
                // 셰이프를 처음부터 다시 만들게 강제: 껐다 켜면 프로바이더가 재초기화되고,
                // Update()가 트림 0으로 메쉬를 다시 생성한다(진단값도 신선해진다).
                caster.enabled = false;
                caster.enabled = true;
                caster.Update();
                vtx = caster.mesh != null ? caster.mesh.vertexCount : 0;
                boundR = caster.boundingSphere.radius;
            }
            catch (System.NullReferenceException) { /* 메쉬 미생성 — 다음 프레임 Update가 기본 트림으로 만든다 */ }

            // 정렬 레이어를 명시(씬엔 Default 하나뿐). 비어 있으면 라이트와 매칭 안 돼
            // 그림자 계산에서 빠진다.
            var serialized = new SerializedObject(caster);
            var layers = serialized.FindProperty("m_ApplyToSortingLayers");
            if (layers != null && layers.isArray)
            {
                layers.arraySize = 1;
                layers.GetArrayElementAtIndex(0).intValue = 0; // Default
                serialized.ApplyModifiedProperties();
            }

            EditorUtility.SetDirty(caster);

            // 진단: 셰이프 소스(2=SpriteRenderer 프로바이더 정상), 그림자 메쉬 정점 수
            // (0이면 셰이프가 비어 그림자가 안 나온다), 캐스터 bounds 반경.
            var source = serialized.FindProperty("m_ShadowCastingSource");
            var diag = $" [source={source?.intValue}, meshVtx={vtx}, boundR={boundR:0.00}]";
            report.Linked(created
                ? $"{spriteName}에 ShadowCaster2D{diag}"
                : $"{spriteName} ShadowCaster2D 재적용 (trimEdge=0){diag}");
        }

        // ── F. 광원 스프라이트 → Unlit ──────────────────────────────────
        private static void SwapToUnlit(Transform screenRoot, string spriteName, Material unlit, SceneSetupReport report)
        {
            var target = FindChild(screenRoot, spriteName);
            var renderer = target != null ? target.GetComponent<SpriteRenderer>() : null;
            if (renderer == null)
            {
                report.Problem($"{spriteName}의 SpriteRenderer를 찾지 못해 Unlit 교체를 건너뜁니다.");
                return;
            }

            if (renderer.sharedMaterial == unlit)
                return;

            Undo.RecordObject(renderer, UndoGroupName);
            renderer.sharedMaterial = unlit;
            EditorUtility.SetDirty(renderer);
            report.Linked($"{spriteName} 머티리얼 → Sprite-Unlit-Default");
        }

        // ── G. Shadow(+50) 비네트 끄기 ──────────────────────────────────
        private static void DisableShadowVignette(Transform screenRoot, Material unlit, SceneSetupReport report)
        {
            var shadow = FindChild(screenRoot, "Shadow");
            var renderer = shadow != null ? shadow.GetComponent<SpriteRenderer>() : null;
            if (renderer == null)
                return;

            var needsDisable = renderer.enabled;
            var needsUnlit = unlit != null && renderer.sharedMaterial != unlit;
            if (!needsDisable && !needsUnlit)
                return;

            Undo.RecordObject(renderer, UndoGroupName);
            if (needsDisable)
                renderer.enabled = false;
            if (needsUnlit)
                renderer.sharedMaterial = unlit;

            EditorUtility.SetDirty(renderer);
            report.Linked("Shadow(+50) 비네트 비활성 (라이팅으로 대체 — 부족하면 되켜고 알파 하향)");
        }

        // ── 공통 ────────────────────────────────────────────────────────

        // 이름의 자식 라이트를 찾아 돌려주고, 없으면 만든다. 위치는 만들 때만 잡는다.
        private static Light2D EnsureLight(
            string name, Transform parent, Vector3 localPosition, out bool created)
        {
            var existing = FindChild(parent, name);
            if (existing != null)
            {
                created = false;
                var light2D = existing.GetComponent<Light2D>();
                return light2D != null ? light2D : Undo.AddComponent<Light2D>(existing.gameObject);
            }

            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, UndoGroupName);
            go.transform.SetParent(parent, worldPositionStays: false);
            go.transform.localPosition = localPosition;

            created = true;
            return Undo.AddComponent<Light2D>(go);
        }

        // Freeform 셰이프는 shapePath가 바뀌면 메쉬를 다시 만들어야 보인다.
        // 컴포넌트를 껐다 켜면 OnEnable에서 UpdateMesh가 강제로 다시 돈다(공개 경로).
        private static void ForceShapeMeshRebuild(Light2D light)
        {
            light.enabled = false;
            light.enabled = true;
        }

        private static Material LoadUnlitMaterial(SceneSetupReport report)
        {
            var path = AssetDatabase.GUIDToAssetPath(UnlitMaterialGuid);
            var material = string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
                report.Problem("Sprite-Unlit-Default 머티리얼을 찾지 못했습니다. 램프·시계·Shadow 머티리얼 교체를 건너뜁니다.");

            return material;
        }

        // 이름으로 자손 Transform을 찾는다(비활성 포함). 스프라이트는 Space 또는
        // MemoryRoomScreen 바로 아래에 있지만, 깊이에 의존하지 않도록 재귀로 훑는다.
        private static Transform FindChild(Transform root, string name)
        {
            if (root.name == name)
                return root;

            return root
                .GetComponentsInChildren<Transform>(includeInactive: true)
                .FirstOrDefault(t => t.name == name);
        }

        private static void Present(string sceneName, SceneSetupReport report)
        {
            var summary = report.Summarize();
            Debug.Log($"[{UndoGroupName}] 씬: {sceneName}\n{summary}");

            var title = report.HasProblems ? "기억 방 조명 구성 — 확인 필요" : "기억 방 조명 구성 완료";
            EditorUtility.DisplayDialog(title, summary, "확인");
        }
    }
}
