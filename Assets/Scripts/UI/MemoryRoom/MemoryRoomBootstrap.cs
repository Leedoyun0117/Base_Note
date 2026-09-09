using GameName.UI.Authoring;
using GameName.UI.ClueZoom;
using GameName.UI.Inventory;
using GameName.UI.MemoryRoom.Dialogue;
using GameName.UI.MemoryRoom.Space;
using GameName.UI.Overlays;
using GameName.UI.Restoration;
using GameName.UI.Session;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameName.UI.MemoryRoom
{
    // 기억 방 화면의 구성 루트. GameSessionBootstrap이 이미 조립해 둔
    // GameSession을 참조로 받아 그 위에 이 화면의 View/Controller만 얹는다 —
    // Core 객체를 여기서 새로 만들지 않는다.
    //
    // 이 화면은 두 가지 표시 수단을 함께 쓴다: 방 자체와 마스크는 2D 씬
    // 오브젝트로, 상단 바·대화 패널·오버레이는 UI Toolkit으로 그린다. 그 둘을
    // 잇는 배선도 전부 여기 한 곳에서 이뤄지고, 씬 오브젝트는 어디서도 Core
    // 처리기를 직접 참조하지 않는다.
    //
    // 방 전환 계기는 이제 실제 대화다 — 대사가 소진되면 DialogueProgressor가
    // DialogueEndedEvent를, 신뢰가 0이면 TrustGauge가 그 사실을 발행하고,
    // RoomCompletionArbiter → RunProgressor가 다음 방으로 넘긴다. 예전의 임시
    // 진행 키(N)는 없어졌다.
    [RequireComponent(typeof(UIDocument))]
    public sealed class MemoryRoomBootstrap : MonoBehaviour
    {
        [SerializeField] private GameSessionBootstrap _gameSession;

        // 씬에 그려지는 방. HUD와 같은 GameObject에 둘 이유가 없어 따로 받는다.
        [SerializeField] private MemoryRoomSpaceView _spaceView;

        // 신뢰가 깎일 때 방을 흔드는 카메라 컴포넌트. 씬 메인 카메라에 붙는다.
        [SerializeField] private CameraShake _cameraShake;

        // 신뢰가 이 값 이하로 내려가 있는 동안에는 화면이 계속 떨린다(임펄스가
        // 아니라 지속). 0으로 두면 마지막 한 칸에서도 지속 떨림 없이 임펄스만.
        [SerializeField] private int _continuousShakeAtOrBelowTrust = 1;

        // 방 치수는 씬이 아니라 에셋에서 온다 — 코드에도 씬에도 숫자를 박지 않기 위함이다.
        [SerializeField] private MemoryRoomLayoutAsset _layoutAsset;

        // 가려진 대사 구간에 대신 그릴 문구("[파랑]" 등). 색 이름이 코드에 박히지
        // 않도록 저작 에셋으로 받는다.
        [SerializeField] private MemoryColorLabelAsset _memoryColorLabels;

        // 인벤토리·설명 창의 가시성을 한 곳에서 관리하는 컴포넌트.
        [SerializeField] private OverlayPanelHost _overlayPanels;

        // 도트 열화 셰이더에게 "신뢰 마스크가 지금 얼마나 덮고 있는지"를 알리는
        // 전역 값. 0 = 안 덮음(효과 없음), 1 = 완전히 닫힘. 셰이더는 FullScreenPass라
        // 프레임별 파라미터를 못 받으므로 공유 머티리얼 대신 전역으로 넘긴다.
        // 매 LateUpdate마다 마스크 판의 실제(=USS transition으로 애니메이션 중인)
        // 폭을 읽어 흘리므로, 셰이더 경계도 UI와 같은 속도로 열리고 닫힌다.
        private static readonly int MaskCoverId = Shader.PropertyToID("_MaskCover");

        private MemoryRoomScreenController _screenController;
        private MemoryRoomMaskView _maskView;

        private void OnEnable()
        {
            var session = _gameSession.Session;
            var root = GetComponent<UIDocument>().rootVisualElement;
            var layout = _layoutAsset.ToLayout();

            // ── HUD(상단 상태 표시줄) ───────────────────────────────────
            var hudView = new MemoryRoomHudView(root);
            hudView.SetKeyHints(OverlayKeyHint.Describe(_overlayPanels.KeyNameOf(OverlayPanel.Inventory)));
            var hudController = new MemoryRoomHudController(
                hudView, session.Trust, session.Hiromi, session.MoveHiromiCost, session.Chance,
                session.Memories, session.EventBus);

            // ── 2D 씬 + 마스크 ─────────────────────────────────────────
            var spaceController = new MemoryRoomSpaceController(
                _spaceView, layout, session.ClueTracker, session.ClueState, session.ClueAccess,
                session.CurrentRoomId, session.EventBus);

            // 마스크는 UI Toolkit 요소(같은 root 안의 mask-left/right)로, 이제
            // 폭만 애니메이션하고 시각 처리는 셰이더가 한다.
            _maskView = new MemoryRoomMaskView(root);
            var maskController = new MemoryRoomMaskController(
                _maskView, session.Visibility, session.Trust, session.EventBus);

            // 레이아웃이 잡히기 전 첫 프레임용 초기값. 이후로는 LateUpdate가
            // 애니메이션 중인 실제 폭을 흘린다.
            Shader.SetGlobalFloat(MaskCoverId, _maskView.CoverFraction);

            // 신뢰가 깎일 때 2D 씬만 짧게 흔든다(UI 레이어는 그대로). 정식
            // 배선은 씬 구성 도구가 메인 카메라에 붙여 이 필드에 연결한다 —
            // 아직 안 돌린 씬에서도 죽지 않도록 카메라에서 직접 찾아 붙인다.
            var cameraShake = _cameraShake != null ? _cameraShake : ResolveCameraShake();
            var cameraShakeController = new MemoryRoomCameraShakeController(
                cameraShake, session.Trust, session.EventBus, _continuousShakeAtOrBelowTrust);

            // ── 하단 대화 패널 ──────────────────────────────────────────
            var dialogueView = new DialoguePanelView(
                root, session.CensoredTextParser, session.CensorResolver, _memoryColorLabels);
            var dialogueController = new DialoguePanelController(
                dialogueView, session.Dialogue, session.CensorUnlock, session.Memories, session.ClueTracker,
                session.CensorKeyColors, _memoryColorLabels.DisplayName, session.CurrentRoomId, session.EventBus);

            // ── 오버레이: 단서 설명 창 ──────────────────────────────────
            var zoomView = new ClueZoomScreenView(_overlayPanels.RootOf(OverlayPanel.ClueZoom));
            var zoomController = new ClueZoomScreenController(zoomView, session.ClueCollectionProcessor);

            // ── 오버레이: 가방 ─────────────────────────────────────────
            var inventoryRoot = _overlayPanels.RootOf(OverlayPanel.Inventory);
            var inventoryView = new InventoryScreenView(inventoryRoot);
            var inventoryController = new InventoryScreenController(inventoryView, session.Inventory);

            var clueUseView = new ClueUsePanelView(inventoryRoot);
            var clueUseController = new ClueUsePanelController(
                clueUseView, session.ExtractionProcessor, session.ClueDiscardProcessor, session.ClueState,
                session.Hiromi, session.EventBus);

            // "다음 기억으로" 레버 — 대화를 끝까지 보지 않고도 히로민을 써서
            // 스스로 방을 떠나는 유일한 조작. 확인 팝업(히로민 부족 시 강제
            // 이동 여부)도 여기서 뜬다.
            var memoryMoveView = new MemoryMoveLeverView(inventoryRoot);
            var memoryMoveLeverController = new MemoryMoveLeverController(
                memoryMoveView, session.MemoryMove, session.Hiromi, session.Chance,
                session.MoveHiromiCost, session.EventBus);

            // ── 오버레이: 복원도(같은 가방 문서 안의 두 번째 탭) ────────
            var restorationView = new RestorationCanvasView(
                inventoryRoot, _memoryColorLabels.DisplayName);
            var restorationController = new RestorationScreenController(
                restorationView, session.RestorationBoard, session.RestorationBoardEditor,
                _memoryColorLabels.DisplayName, session.EventBus);

            var overlayTabs = new OverlayTabController(
                inventoryRoot,
                new[] { ("overlay-tab-inventory", "inventory-panel"), ("overlay-tab-restoration", "restoration-panel") });

            _screenController = new MemoryRoomScreenController(
                hudView, hudController, spaceController, maskController, cameraShakeController,
                dialogueController, zoomController, inventoryController, clueUseController,
                memoryMoveLeverController, restorationController, overlayTabs, _overlayPanels);

            _overlayPanels.Bind(OverlayPanel.Inventory, () =>
            {
                inventoryController.Refresh();
                restorationController.Refresh();
            });
            _overlayPanels.Bind(OverlayPanel.ClueZoom, onShown: null, onHidden: _screenController.OnClueZoomHidden);
        }

        // 마스크 판의 애니메이션 중인 실제 폭을 매 프레임 셰이더로 흘린다 —
        // 이래야 셰이더의 찢긴 경계도 UI transition과 같은 속도로 열리고 닫힌다.
        private void LateUpdate()
        {
            if (_maskView != null)
                Shader.SetGlobalFloat(MaskCoverId, _maskView.CoverFraction);
        }

        // 씬 구성 도구가 아직 _cameraShake를 연결하지 않았을 때의 대비책.
        // 메인 카메라에 컴포넌트가 없으면 그 자리에서 붙인다.
        private static CameraShake ResolveCameraShake()
        {
            var camera = Camera.main;
            if (camera == null)
                return null;

            return camera.GetComponent<CameraShake>() ?? camera.gameObject.AddComponent<CameraShake>();
        }

        private void OnDisable()
        {
            // 오버레이 쪽에 걸어 둔 대리자를 먼저 끊는다 — 이 화면이 꺼진 뒤에도
            // 남아 있으면 이미 정리된 컨트롤러를 가리키게 된다.
            if (_overlayPanels != null)
            {
                _overlayPanels.Bind(OverlayPanel.Inventory, null);
                _overlayPanels.Bind(OverlayPanel.ClueZoom, null);
            }

            _screenController?.Dispose();
            _screenController = null;
            _maskView = null;

            // 이 화면을 벗어나면 좌우 열화도 함께 걷힌다.
            Shader.SetGlobalFloat(MaskCoverId, 0f);
        }
    }
}
