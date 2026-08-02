using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace GameName.UI.Journal
{
    // Tab 키로 기록지를 열고 닫는다. UI Toolkit의 포커스 기반 키 이벤트가 아니라
    // 매 프레임 키보드 상태를 직접 폴링한다 — 조향실 화면 쪽에 어떤 버튼이
    // 포커스를 들고 있든, 혹은 아무 것도 포커스가 없든 항상 똑같이 동작해야
    // 하기 때문이다(포커스에 의존하면 "이 버튼을 누른 직후에는 Tab이 안 먹는다"
    // 같은 조건부 버그가 생긴다).
    //
    // 레거시 UnityEngine.Input이 아니라 새 Input System(Keyboard.current)을
    // 쓴다 — 이 프로젝트는 Player Settings의 Active Input Handling이 "Input
    // System Package" 단독으로 설정되어 있어(Both가 아님) 레거시 Input 클래스를
    // 호출하면 즉시 InvalidOperationException이 발생한다.
    //
    // 이 화면은 조향실과 별도의 UIDocument(별도 GameObject)로 띄운다. 기록지가
    // 화면 전체를 덮는 오버레이로 표시되므로, 열려 있는 동안에는 아래 깔린
    // 조향실 화면의 클릭이 자연히 막힌다(UI Toolkit의 피킹 규칙) — 별도의
    // "입력 잠금" 로직을 따로 두지 않아도 두 화면의 입력이 서로 충돌하지 않는다.
    public sealed class JournalVisibilityController : MonoBehaviour
    {
        [SerializeField] private UIDocument _journalDocument;

        private JournalScreenController _screenController;
        private bool _isVisible;

        // 구성 루트(PerfumeryBootstrap 등)가 이 UIDocument의 rootVisualElement로
        // JournalScreenView를 만들어야 하므로 노출한다 — 같은 UIDocument 참조를
        // 인스펙터에 두 번 따로 연결하지 않아도 되게 하기 위함이다.
        public UIDocument Document => _journalDocument;

        // 초기 숨김은 반드시 여기(Awake)에서 정한다 — Initialize()가 언제
        // 호출되는지에 기대지 않기 위함이다. Initialize는 각 화면의 Bootstrap이
        // 자기 OnEnable에서 부르는데, 이제 조향실/기억 방/분석실 화면은 의뢰
        // 단계가 InMemory가 되기 전까지 전부 비활성 상태라 그 시점까지
        // Initialize가 아예 호출되지 않는다. 그 사이 기록지는 UXML/USS의
        // 기본값(별도로 숨겨 두지 않으면 보이는 상태)에 그대로 노출되어 있었다
        // — 이게 "Play 진입 시 기록지가 이미 떠 있던" 원인이었다. Awake는 이
        // GameObject 자신이 활성화되는 즉시(다른 화면과 무관하게) 실행되므로
        // 이 문제가 생기지 않는다.
        private void Awake()
        {
            SetVisible(false);
        }

        public void Initialize(JournalScreenController screenController)
        {
            _screenController = screenController;

            // 가시성은 여기서 다시 건드리지 않는다 — 화면이 전환될 때마다(조향실
            // ↔ 기억 방 ↔ 분석실) 각 Bootstrap이 이 메서드를 다시 호출하는데,
            // 매번 강제로 닫아 버리면 사용자가 Tab으로 열어 둔 기록지가 화면을
            // 옮길 때마다 제멋대로 닫힌다. 최초 숨김은 Awake()가 이미 보장했다.
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.tabKey.wasPressedThisFrame)
                SetVisible(!_isVisible);
        }

        private void SetVisible(bool visible)
        {
            _isVisible = visible;
            _journalDocument.rootVisualElement.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

            if (visible)
                _screenController?.Refresh();
        }
    }
}
