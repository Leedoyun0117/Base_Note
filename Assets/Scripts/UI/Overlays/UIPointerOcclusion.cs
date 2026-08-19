using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameName.UI.Overlays
{
    // 지금 마우스 아래에 "실제로 집을 수 있는" UI 요소가 있는지 판정한다.
    //
    // 2D 씬 클릭은 Physics2D로 직접 찍기 때문에 UI가 저절로 막아주지 않는다.
    // 그렇다고 UI가 떠 있는 동안 씬을 통째로 막으면, 화면 가장자리의 얇은
    // HUD 하나 때문에 방 전체가 조작 불가가 된다. 그래서 "요소 단위"로 묻는다.
    //
    // 핵심은 picking-mode다. 배경 컨테이너를 Ignore로 꺼 두면 Pick이 그
    // 요소들을 건너뛰므로, 버튼처럼 진짜 조작 대상 위에서만 true가 된다.
    // 즉 이 판정의 정확도는 UXML의 picking-mode 설정이 그대로 결정한다.
    public static class UIPointerOcclusion
    {
        public static bool IsPointerOverPickableElement(
            IReadOnlyList<UIDocument> documents, Vector2 screenPosition)
        {
            if (documents == null)
                return false;

            foreach (var document in documents)
            {
                if (!IsUsable(document))
                    continue;

                var panel = document.rootVisualElement.panel;
                if (panel == null)
                    continue;

                // 화면 좌표(왼쪽 아래 원점)를 패널 좌표(왼쪽 위 원점)로 옮긴다.
                var panelPosition = RuntimePanelUtils.ScreenToPanel(panel, screenPosition);
                if (Occludes(document, panel.Pick(panelPosition)))
                    return true;
            }

            return false;
        }

        // 문서를 담는 껍데기(패널 루트와 UXML을 감싸는 TemplateContainer)는
        // 화면 전체를 덮으면서도 picking-mode를 우리가 정할 수 없는 요소다.
        // 그것들까지 "포인터를 받는 UI"로 치면 HUD가 한 줄만 있어도 화면 전체가
        // 조작 불가가 된다 — 눈에는 방이 멀쩡히 보이는데 아무것도 눌리지 않는,
        // 원인을 짐작하기 가장 어려운 종류의 증상이다. 그래서 실제 내용 요소에
        // 닿았을 때만 가려진 것으로 본다.
        private static bool Occludes(UIDocument document, VisualElement picked)
        {
            if (picked == null)
                return false;

            if (picked == document.rootVisualElement)
                return false;

            return !(picked is TemplateContainer);
        }

        // 꺼져 있는 문서는 화면에 없는 것과 같다 — 오버레이가 닫혀 있을 때
        // 그 문서가 클릭을 막으면 안 된다.
        private static bool IsUsable(UIDocument document) =>
            document != null &&
            document.isActiveAndEnabled &&
            document.rootVisualElement != null &&
            document.rootVisualElement.resolvedStyle.display != DisplayStyle.None;
    }
}
