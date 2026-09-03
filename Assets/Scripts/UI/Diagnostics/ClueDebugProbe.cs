using UnityEngine;

namespace GameName.UI.Diagnostics
{
    // 단서 상호작용 진단의 유일한 켜고 끄는 지점.
    //
    // 이 컴포넌트가 씬에 없으면 이 폴더의 코드는 한 줄도 돌지 않는다. 그래서
    // 진단을 끝내는 절차가 "오브젝트 하나 지우기"만큼 단순하다. 인스펙터에서
    // 항목별로 따로 끌 수도 있다 — 예를 들어 방 이동만 쫓을 때 클릭 로그가
    // 섞이면 읽기 어렵다.
    //
    // 실행 순서를 앞으로 크게 당겨 둔다. 판정의 "입력"(씬 조작 허용 여부, UI
    // 가림, 그 지점의 콜라이더)은 ScenePointerInput이 돌기 전에 재야 하기
    // 때문이다.
    //
    // 처음에는 반대로 뒤로 밀어 두었다가 한 번 크게 헤맸다. 클릭이 성공해서
    // 확대 화면이 열린 프레임에서, 그 결과(조작 꺼짐, 오버레이가 화면을 덮음)를
    // 원인인 것처럼 찍었기 때문이다. 판정 결과(무엇이 골라졌는가)는 LateUpdate에서
    // 읽으므로 여전히 같은 줄에 함께 나온다 — 모든 Update가 끝난 뒤에 LateUpdate가
    // 돌기 때문이다.
    [DefaultExecutionOrder(-1000)]
    public sealed class ClueDebugProbe : MonoBehaviour
    {
        [Header("전체 스위치")]
        [Tooltip("끄면 이 오브젝트는 아무 일도 하지 않는다. 콘솔에서 [ClueDebug]로 걸러 볼 수 있다.")]
        [SerializeField] private bool _logEnabled = true;

        [Header("항목별")]
        [Tooltip("1. 방을 그릴 때마다 생성·파괴된 단서 오브젝트")]
        [SerializeField] private bool _logSceneObjects = true;

        [Tooltip("2. 마우스 버튼을 눌렀을 때의 판정 경로")]
        [SerializeField] private bool _logPointerPicks = true;

        [Tooltip("5. 단서를 누른 뒤 확대 화면이 뜨기까지의 구간")]
        [SerializeField] private bool _logClueActivations = true;

        [Tooltip("6. 확대 화면으로 가는 배선이 이어져 있는지(값이 달라질 때만 찍는다)")]
        [SerializeField] private bool _logScreenWiring = true;

        [Tooltip("7. 오버레이가 뜨고 사라진 순간(프레임 번호와 함께)")]
        [SerializeField] private bool _logOverlayTransitions = true;

        private readonly ClueDebugContext _context = new ClueDebugContext();

        private ClueSceneChangeReporter _sceneChanges;
        private PointerPickReporter _pointerPicks;
        private ClueActivationReporter _clueActivations;
        private ScreenWiringReporter _screenWiring;
        private OverlayTransitionReporter _overlayTransitions;

        private void Awake()
        {
            _sceneChanges = new ClueSceneChangeReporter(_context);
            _pointerPicks = new PointerPickReporter(_context);
            _clueActivations = new ClueActivationReporter(_context);
            _screenWiring = new ScreenWiringReporter(_context);
            _overlayTransitions = new OverlayTransitionReporter(_context);
        }

        private void OnEnable()
        {
            ClueDebugLog.Enabled = _logEnabled;
            ClueDebugLog.Write("진단 시작 — 콘솔 검색창에 [ClueDebug]를 넣으면 이 출력만 걸러진다.");
        }

        private void OnDisable()
        {
            ClueDebugLog.Enabled = false;
        }

        // 인스펙터에서 체크를 바꾸면 플레이 중에도 바로 반영된다.
        private void OnValidate()
        {
            ClueDebugLog.Enabled = _logEnabled;
        }

        private void Update()
        {
            ClueDebugLog.Enabled = _logEnabled;
            if (!_logEnabled)
                return;

            // 세션은 씬이 뜬 뒤에 만들어지므로 구독은 매 프레임 확인해서 건다.
            // 이미 걸려 있으면 아무 일도 하지 않는다.
            if (_logClueActivations)
                _clueActivations.EnsureSubscribed();

            if (_logOverlayTransitions)
                _overlayTransitions.EnsureSubscribed();

            // 판정이 돌기 전의 상태만 담아 둔다. 실제 출력은 LateUpdate다.
            if (_logPointerPicks)
                _pointerPicks.CaptureBeforePick();
        }

        // 프레임에 일어날 일이 전부 끝난 뒤에 보는 보고들. 이동과 버리기는
        // 이벤트 구독자 여럿이 이어 달리는 구조라, 그 줄이 다 끝난 자리에서
        // 봐야 최종 상태가 보인다.
        private void LateUpdate()
        {
            if (!_logEnabled)
                return;

            // 모든 Update가 끝난 자리 — 이 프레임의 판정 결과가 확정되어 있다.
            if (_logPointerPicks)
                _pointerPicks.ReportAfterPick();

            if (_logSceneObjects)
                _sceneChanges.Tick();

            if (_logClueActivations)
                _clueActivations.Tick();

            if (_logScreenWiring)
                _screenWiring.Tick();
        }

        private void OnDestroy()
        {
            _clueActivations?.Dispose();
            _overlayTransitions?.Dispose();
            ClueDebugLog.Enabled = false;
        }
    }
}
