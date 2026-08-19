using System;
using System.Collections.Generic;

namespace GameName.UI.Overlays
{
    // 오버레이 화면들의 가시성을 관리하는 단 하나의 자리.
    //
    // 각 화면 컨트롤러가 스스로 "나는 지금 보인다/안 보인다"를 들고 있으면,
    // 화면이 늘어날 때마다 "동시에 두 개가 떠 있으면 안 된다" 같은 규칙을
    // 강제할 곳이 사라진다. 그래서 가시성 상태는 전부 여기 한 곳에만 있고,
    // 각 화면은 보이라는 지시를 받기만 한다(IOverlayPanelContent).
    //
    // 한 번에 하나만 보이게 하는 이유는 미래 계획 때문만이 아니다 — 오버레이는
    // 화면 전체를 덮으므로 둘이 동시에 떠 있으면 아래 것은 어차피 조작할 수
    // 없다. 나중에 Tab 하나로 기록지/인벤토리를 오가는 탭 패널로 합칠 때는 이
    // 규칙이 그대로 탭 전환이 된다.
    //
    // UnityEngine에 의존하지 않는 순수 C#이라, 입력이나 씬 없이도 규칙만 따로
    // 검증할 수 있다.
    public sealed class OverlayPanelRouter
    {
        private readonly Dictionary<OverlayPanel, IOverlayPanelContent> _contentsByPanel =
            new Dictionary<OverlayPanel, IOverlayPanelContent>();

        // 지금 떠 있는 오버레이. 아무것도 떠 있지 않으면 null이다.
        public OverlayPanel? Visible { get; private set; }

        public bool IsAnyVisible => Visible.HasValue;

        // 오버레이가 뜨거나 사라졌다. 씬 조작을 막아야 할지 판단하는 쪽이
        // 매 프레임 묻지 않고 이 알림만 듣게 하기 위한 것이다.
        public event Action<OverlayPanel?> VisibleChanged;

        // 아직 등록되지 않은 오버레이를 Show/Toggle 하려는 시도는 조용히
        // 무시된다(예외가 아니다) — 각 화면의 Bootstrap이 자기 오버레이를
        // 등록하는데, 화면 전환 때문에 아직 등록 전인 순간에 키를 누를 수 있기
        // 때문이다. 그건 버그가 아니라 정상적인 타이밍이다.
        public void Register(OverlayPanel panel, IOverlayPanelContent content)
        {
            _contentsByPanel[panel] = content ?? throw new ArgumentNullException(nameof(content));

            // 새로 등록된 화면도 지금의 가시성 상태를 그대로 따라야 한다 —
            // 그러지 않으면 UXML 기본값(보이는 상태)에 노출된 채로 남는다.
            content.SetVisible(Visible.HasValue && Visible.Value == panel);
        }

        public void Toggle(OverlayPanel panel)
        {
            if (Visible.HasValue && Visible.Value == panel)
                HideAll();
            else
                Show(panel);
        }

        public void Show(OverlayPanel panel)
        {
            if (!_contentsByPanel.ContainsKey(panel))
                return;

            foreach (var pair in _contentsByPanel)
                pair.Value.SetVisible(pair.Key == panel);

            SetVisible(panel);
        }

        public void HideAll()
        {
            foreach (var pair in _contentsByPanel)
                pair.Value.SetVisible(false);

            SetVisible(null);
        }

        // 값이 실제로 달라졌을 때만 알린다 — 화면이 전환될 때마다 HideAll이
        // 다시 불리는데, 그때마다 같은 상태를 통보하면 듣는 쪽이 불필요하게
        // 다시 그린다.
        private void SetVisible(OverlayPanel? panel)
        {
            if (Nullable.Equals(Visible, panel))
                return;

            Visible = panel;
            VisibleChanged?.Invoke(panel);
        }
    }
}
