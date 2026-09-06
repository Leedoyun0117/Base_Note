using System;

namespace GameName.UI.Inventory
{
    // 가방 단서 패널 화면 표면. 컨트롤러는 이 경계만 보고, 실제 UI Toolkit
    // 요소를 다루는 것은 ClueUsePanelView다.
    //
    // 경계를 둔 이유는 다른 스크린 컨트롤러 테스트와 같다 — "ClueState/자원에
    // 따라 추출 버튼이 열리는가"는 UIDocument 없이 검증되어야 한다.
    public interface IClueUsePanelView
    {
        event Action Extract;
        event Action Discard;
        event Action Closed;

        void Open(string title);
        void Close();
        void SetActions(bool extractEnabled, string extractReason);
        void SetDiscardAction(bool discardEnabled, string discardReason);
        void SetResult(string message);
    }
}
