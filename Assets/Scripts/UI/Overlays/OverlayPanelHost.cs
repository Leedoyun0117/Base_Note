using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace GameName.UI.Overlays
{
    // 오버레이 화면들을 한데 모아 두는 씬 컴포넌트. 씬에 하나만 둔다.
    //
    // 예전에는 기록지만 자기 전용 컴포넌트(JournalVisibilityController)로
    // Tab 키와 가시성을 함께 들고 있었다. 인벤토리 화면이 생기면서 같은 구조를
    // 하나 더 만들면 동시에 둘이 뜨는 상태를 막을 곳이 없어지고, 나중에 둘을
    // 탭으로 합칠 때 두 컴포넌트를 하나로 되돌리는 작업이 남는다. 그래서 지금
    // 미리 한 자리로 모은다 — 합치는 것 자체는 이번에 하지 않는다.
    //
    // 키를 인스펙터에서 바꿀 수 있게 직렬화 필드로 둔다. 지금 인벤토리가 I인
    // 것은 Tab이 이미 기록지에 쓰이고 있어 충돌을 피하려는 임시 조치이므로,
    // 이 값이 코드에 상수로 박혀 있으면 안 된다 — 통합 시점에는 둘 다 Tab이
    // 되고 화면 안에서 탭으로 갈린다.
    //
    // 레거시 UnityEngine.Input이 아니라 새 Input System(Keyboard.current)을
    // 쓴다 — 이 프로젝트의 Active Input Handling이 Input System Package 단독이라
    // 레거시 Input 클래스를 호출하면 즉시 예외가 난다. UI Toolkit의 포커스 기반
    // 키 이벤트가 아니라 매 프레임 폴링하는 이유도 예전과 같다: 어떤 요소가
    // 포커스를 들고 있든 항상 똑같이 동작해야 하기 때문이다.
    // 각 화면의 Bootstrap이 OnEnable에서 이 컴포넌트에 자기 오버레이를 붙이므로,
    // 그 전에 Awake가 끝나 있어야 한다 — Unity는 서로 다른 오브젝트의 Awake
    // 순서를 기본적으로 보장하지 않는다(GameSessionBootstrap이 -100을 쓰는 것과
    // 같은 이유이며, 그보다는 뒤에 와야 한다).
    [DefaultExecutionOrder(-90)]
    public sealed class OverlayPanelHost : MonoBehaviour
    {
        [SerializeField] private UIDocument _journalDocument;
        [SerializeField] private UIDocument _inventoryDocument;
        [SerializeField] private UIDocument _clueZoomDocument;

        [SerializeField] private Key _journalKey = Key.Tab;
        [SerializeField] private Key _inventoryKey = Key.I;

        private readonly OverlayPanelRouter _router = new OverlayPanelRouter();
        private readonly Dictionary<OverlayPanel, UIDocumentOverlayContent> _contents =
            new Dictionary<OverlayPanel, UIDocumentOverlayContent>();

        // 각 화면의 Bootstrap이 이 UIDocument의 rootVisualElement로 자기 View를
        // 만들어야 하므로 노출한다 — 같은 UIDocument 참조를 인스펙터에 두 번
        // 따로 연결하지 않아도 되게 하기 위함이다.
        // 전체 화면 오버레이가 하나라도 떠 있는가. 씬 조작을 막을지 정하는
        // 쪽이 이 하나만 보면 되게 한다.
        public bool IsAnyVisible => _router.IsAnyVisible;

        public event Action<OverlayPanel?> VisibleChanged
        {
            add => _router.VisibleChanged += value;
            remove => _router.VisibleChanged -= value;
        }

        public VisualElement RootOf(OverlayPanel panel)
        {
            var document = DocumentOf(panel);
            return document == null ? null : document.rootVisualElement;
        }

        // 실제로 설정된 키 이름. 화면의 조작 안내가 이 값에서 만들어지므로
        // 인스펙터에서 키를 바꾸면 안내도 함께 바뀐다.
        public string KeyNameOf(OverlayPanel panel)
        {
            switch (panel)
            {
                case OverlayPanel.Journal: return _journalKey.ToString();
                case OverlayPanel.Inventory: return _inventoryKey.ToString();
                default: return string.Empty;
            }
        }

        public UIDocument DocumentOf(OverlayPanel panel)
        {
            switch (panel)
            {
                case OverlayPanel.Journal: return _journalDocument;
                case OverlayPanel.Inventory: return _inventoryDocument;
                case OverlayPanel.ClueZoom: return _clueZoomDocument;
                default: return null;
            }
        }

        // 초기 숨김은 반드시 여기(Awake)에서 정한다 — Bind()가 언제 호출되는지에
        // 기대지 않기 위함이다. Bind는 각 화면의 Bootstrap이 자기 OnEnable에서
        // 부르는데, 화면들은 의뢰 단계가 InMemory가 되기 전까지 전부 비활성이라
        // 그때까지 아예 호출되지 않는다. 그 사이 오버레이가 UXML 기본값(보이는
        // 상태)에 노출되어 있으면 Play 진입 즉시 화면을 덮어 버린다.
        private void Awake()
        {
            RegisterIfPresent(OverlayPanel.Journal, _journalDocument);
            RegisterIfPresent(OverlayPanel.Inventory, _inventoryDocument);
            RegisterIfPresent(OverlayPanel.ClueZoom, _clueZoomDocument);
            _router.HideAll();
        }

        // 화면이 전환될 때마다 각 Bootstrap이 다시 부른다. 가시성은 여기서
        // 건드리지 않는다 — 매번 강제로 닫으면 사용자가 열어 둔 오버레이가
        // 화면을 옮길 때마다 제멋대로 닫힌다.
        public void Bind(OverlayPanel panel, Action onShown, Action onHidden = null)
        {
            if (_contents.TryGetValue(panel, out var content))
                content.BindCallbacks(onShown, onHidden);
        }

        // 키가 아니라 게임 안의 행동으로 열리고 닫히는 오버레이(확대 화면)를
        // 위한 통로. 여는 방법이 늘어나도 가시성 상태는 여전히 라우터 한 곳에만
        // 있다는 것이 요점이다.
        public void Show(OverlayPanel panel) => _router.Show(panel);

        public void Hide(OverlayPanel panel)
        {
            if (_router.Visible.HasValue && _router.Visible.Value == panel)
                _router.HideAll();
        }

        private void RegisterIfPresent(OverlayPanel panel, UIDocument document)
        {
            if (document == null)
                return;

            var content = new UIDocumentOverlayContent(document.rootVisualElement);
            _contents.Add(panel, content);
            _router.Register(panel, content);
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard[_journalKey].wasPressedThisFrame)
                _router.Toggle(OverlayPanel.Journal);
            else if (keyboard[_inventoryKey].wasPressedThisFrame)
                _router.Toggle(OverlayPanel.Inventory);
        }
    }
}
