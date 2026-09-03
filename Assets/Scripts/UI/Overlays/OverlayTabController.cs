using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace GameName.UI.Overlays
{
    // 한 오버레이 문서 안에서 여러 패널을 탭으로 전환한다.
    //
    // OverlayPanelRouter의 "오버레이는 하나만" 원칙은 그대로다 — 이 탭들은 같은
    // 오버레이 슬롯 안의 하위 화면이지 별도 오버레이가 아니다. 그래서 라우터를
    // 거치지 않고 여기서 display만 바꾼다.
    //
    // 탭 상태는 오버레이가 닫혀도 그대로 남는다(다시 열면 마지막 탭). 구현이
    // 간단하고, 스펙상 유지/초기화 어느 쪽이든 무방하다.
    public sealed class OverlayTabController : IDisposable
    {
        private readonly List<(Button Button, VisualElement Panel, EventCallback<ClickEvent> Handler)> _tabs =
            new List<(Button, VisualElement, EventCallback<ClickEvent>)>();

        // 탭이 바뀌었다(index). 갓 보이게 된 패널이 레이아웃 후 다시 그려야 할
        // 때(복원도 캔버스 등) 쓴다.
        public event Action<int> TabSelected;

        public int SelectedIndex { get; private set; }

        // tabs: (버튼 이름, 패널 이름) 쌍. root 아래에서 찾는다.
        public OverlayTabController(VisualElement root, IReadOnlyList<(string ButtonName, string PanelName)> tabs, int initialIndex = 0)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            if (tabs == null || tabs.Count == 0) throw new ArgumentException("탭이 하나도 없다.", nameof(tabs));

            for (var i = 0; i < tabs.Count; i++)
            {
                var button = root.Q<Button>(tabs[i].ButtonName);
                var panel = root.Q<VisualElement>(tabs[i].PanelName);
                if (button == null || panel == null)
                    throw new InvalidOperationException(
                        $"탭 요소를 찾지 못했다: {tabs[i].ButtonName} / {tabs[i].PanelName}");

                var index = i;
                EventCallback<ClickEvent> handler = _ => Select(index);
                button.RegisterCallback(handler);
                _tabs.Add((button, panel, handler));
            }

            Select(initialIndex, notify: false);
        }

        public void Select(int index) => Select(index, notify: true);

        private void Select(int index, bool notify)
        {
            if (index < 0 || index >= _tabs.Count)
                return;

            SelectedIndex = index;

            for (var i = 0; i < _tabs.Count; i++)
            {
                var active = i == index;
                _tabs[i].Panel.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
                _tabs[i].Button.EnableInClassList("overlay-tab--active", active);
            }

            if (notify)
                TabSelected?.Invoke(index);
        }

        public void Dispose()
        {
            foreach (var (button, _, handler) in _tabs)
                button.UnregisterCallback(handler);

            _tabs.Clear();
            TabSelected = null;
        }
    }
}
