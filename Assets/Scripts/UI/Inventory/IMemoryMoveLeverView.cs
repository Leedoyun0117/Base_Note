using System;

namespace GameName.UI.Inventory
{
    // 가방 화면의 "다음 기억으로" 레버 화면 표면. 컨트롤러는 이 경계만 보고,
    // 실제 UI Toolkit 요소를 다루는 것은 MemoryMoveLeverView다.
    public interface IMemoryMoveLeverView
    {
        // 레버를 당겼다 — 히로민이 충분한지는 컨트롤러가 판단한다.
        event Action MoveClicked;

        // 확인 팝업에서 "기회를 써서 강제로 이동"을 골랐다.
        event Action ForceConfirmed;

        // 확인 팝업에서 "더 대화하고 히로민을 모은다(취소)"를 골랐다.
        event Action ForceCancelled;

        void SetMoveEnabled(bool enabled);

        // 히로민이 문턱보다 모자랄 때만 뜨는 확인 팝업. message에 남은 기회
        // 수까지 실어 보여 준다.
        void ShowForceConfirm(string message);
        void HideForceConfirm();
    }
}
