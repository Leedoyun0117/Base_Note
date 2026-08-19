using System;
using GameName.Core.Clues;
using GameName.UI.MemoryRoom.Space;

namespace GameName.UI.Diagnostics
{
    // 단서를 누른 뒤 확대 화면이 뜨기까지의 구간을 본다.
    //
    // 지금까지 로그는 이 구간의 양 끝만 찍고 있었다: "무엇이 선택되었는가"(마우스
    // 판정)와 "오버레이가 떠 있는가"(결과). 그런데 마지막 캡처에서 그 둘이
    // 어긋났다 — 단서는 분명히 선택되었는데 확대 화면은 뜨지 않았다. 그러면
    // 원인은 판정도 오버레이도 아니고 그 사이 어딘가인데, 그 사이가 통째로
    // 로그에 없었다.
    //
    // 사이에는 네 단계가 있다:
    //   씬 오브젝트 → 뷰(ClueActivated) → 방 컨트롤러(식별자로 단서 정보를 되찾음)
    //   → 화면 컨트롤러(확대 화면 열기 + 오버레이 띄우기)
    //
    // 뷰의 알림은 공개 이벤트라 밖에서 그대로 들을 수 있다. 그래서 "뷰까지는
    // 왔는가"를 게임 코드에 손대지 않고 확인할 수 있고, 뷰까지 왔는데 오버레이가
    // 안 떴다면 남은 후보는 둘로 좁혀진다.
    //   · 방 컨트롤러가 그 식별자를 모른다 → HUD에 "이 단서는 더 이상 이 방에
    //     없습니다."가 뜬다(씬 오브젝트와 컨트롤러의 목록이 어긋난 것이다).
    //   · 화면 컨트롤러 쪽에서 예외가 났다 → 콘솔에 빨간 줄이 남는다.
    // 그 둘을 가려 주는 것이 이 보고의 목적이다.
    internal sealed class ClueActivationReporter : IDisposable
    {
        private readonly ClueDebugContext _context;

        private MemoryRoomSpaceView _subscribedView;
        private bool _hasPending;
        private ClueId _pendingClueId;

        public ClueActivationReporter(ClueDebugContext context)
        {
            _context = context;
        }

        // 뷰는 화면이 껐다 켜질 때 그대로 살아 있지만, 씬이 다시 로드되면 다른
        // 인스턴스가 된다. 구독해 둔 대상이 지금의 뷰와 다르면 옮겨 붙는다.
        public void EnsureSubscribed()
        {
            var view = _context.View;
            if (view == null || ReferenceEquals(view, _subscribedView))
                return;

            Unsubscribe();

            view.ClueActivated += OnClueActivated;
            _subscribedView = view;
        }

        private void OnClueActivated(ClueId clueId)
        {
            _hasPending = true;
            _pendingClueId = clueId;
        }

        // 프레임 끝에서 본다 — 확대 화면을 여는 일은 이 알림에 이어 붙어 일어나므로,
        // 알림이 온 그 자리에서 보면 아직 아무 일도 일어나지 않은 상태를 찍는다.
        public void Tick()
        {
            if (!_hasPending)
                return;

            _hasPending = false;

            var overlay = _context.OverlayStateText();
            var hudMessage = _context.HudMessageText();
            var opened = overlay == "ClueZoom";

            ClueDebugLog.Write(
                $"단서누름 | {_pendingClueId.Value} | 뷰까지 도달=예" +
                $" | 확대화면={(opened ? "열림" : "안 열림")}" +
                $" | 오버레이={overlay}" +
                $" | HUD안내=\"{hudMessage}\"");

            if (opened)
                return;

            if (!string.IsNullOrEmpty(hudMessage))
            {
                ClueDebugLog.Suspect(
                    $"눌린 단서({_pendingClueId.Value})를 방 컨트롤러가 알아보지 못했다 — " +
                    $"씬에 있는 오브젝트와 컨트롤러가 아는 목록이 어긋났다. HUD 안내: \"{hudMessage}\"");
                return;
            }

            ClueDebugLog.Suspect(
                $"눌린 단서({_pendingClueId.Value})가 뷰까지는 왔는데 확대 화면이 뜨지 않았고 안내도 없다 — " +
                "화면 컨트롤러 쪽에서 흐름이 끊겼다. 콘솔의 빨간 예외 줄을 함께 확인할 것.");
        }

        private void Unsubscribe()
        {
            if (_subscribedView == null)
                return;

            _subscribedView.ClueActivated -= OnClueActivated;
            _subscribedView = null;
        }

        public void Dispose() => Unsubscribe();
    }
}
