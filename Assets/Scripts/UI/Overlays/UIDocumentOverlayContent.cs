using System;
using UnityEngine.UIElements;

namespace GameName.UI.Overlays
{
    // UIDocument 하나를 통째로 보이고 숨기는 IOverlayPanelContent 구현.
    //
    // 열릴 때 한 번 다시 그리는 것까지 여기서 맡는다 — 오버레이는 떠 있는
    // 동안 아래 화면을 조작할 수 없어 내용이 바뀔 일이 없으므로, 상시 구독
    // 대신 열 때 한 번만 그려도 항상 최신이다(기록지 화면이 원래 쓰던 방식이다).
    //
    // 다시 그리는 방법은 대리자로 받는다 — 기록지 컨트롤러인지 인벤토리
    // 컨트롤러인지 이 타입이 알 필요가 없기 때문이다. 아직 아무도 붙지 않은
    // 동안에도 열고 닫는 것 자체는 동작해야 하므로 null을 허용한다.
    public sealed class UIDocumentOverlayContent : IOverlayPanelContent
    {
        private readonly VisualElement _root;
        private Action _onShown;
        private Action _onHidden;

        public UIDocumentOverlayContent(VisualElement root)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
        }

        // 숨겨질 때의 알림도 받는다 — 확대 화면처럼 열려 있는 동안 임시 상태
        // (드래그 중인 단서 등)를 들고 있는 화면은, 자기가 아닌 다른 이유로
        // 닫혔을 때도 그 상태를 정리할 기회가 있어야 한다.
        public void BindCallbacks(Action onShown, Action onHidden)
        {
            _onShown = onShown;
            _onHidden = onHidden;
        }

        public void SetVisible(bool visible)
        {
            _root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

            if (visible)
                _onShown?.Invoke();
            else
                _onHidden?.Invoke();
        }
    }
}
