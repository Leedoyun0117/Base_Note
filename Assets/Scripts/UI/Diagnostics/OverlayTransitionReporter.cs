using System;
using GameName.UI.Overlays;
using UnityEngine;

namespace GameName.UI.Diagnostics
{
    // 오버레이가 뜨고 사라진 순간을 프레임 번호와 함께 남긴다.
    //
    // "확대 화면이 안 뜬다"와 "떴다가 같은 프레임에 닫힌다"는 밖에서 보면 완전히
    // 같아 보인다. 화면이 깜빡이지도 않고, 프레임 끝에 상태를 읽으면 둘 다
    // "안 떠 있음"이다. 지금까지 이 둘을 구분하지 못해서 원인을 화면 컨트롤러
    // 쪽에서만 찾았다.
    //
    // 프레임 번호를 함께 찍으면 그 구분이 한눈에 된다. 같은 번호에 열림과 닫힘이
    // 나란히 찍히면 열리자마자 닫힌 것이고, 열림만 찍히고 닫힘이 없으면 정상이다.
    internal sealed class OverlayTransitionReporter : IDisposable
    {
        private readonly ClueDebugContext _context;

        private OverlayPanelHost _subscribedHost;
        private int _lastFrame = -1;

        public OverlayTransitionReporter(ClueDebugContext context)
        {
            _context = context;
        }

        public void EnsureSubscribed()
        {
            var host = _context.OverlayPanels;
            if (host == null || ReferenceEquals(host, _subscribedHost))
                return;

            Unsubscribe();

            host.VisibleChanged += OnVisibleChanged;
            _subscribedHost = host;
        }

        private void OnVisibleChanged(OverlayPanel? visible)
        {
            var frame = Time.frameCount;
            var sameFrameAsPrevious = frame == _lastFrame;
            _lastFrame = frame;

            var what = visible.HasValue ? visible.Value.ToString() : "없음(닫힘)";
            ClueDebugLog.Write($"오버레이변화 | 프레임 {frame} | → {what}");

            // 한 프레임 안에서 두 번 바뀌었다는 것은 떴다가 곧바로 닫혔다는 뜻이다.
            if (sameFrameAsPrevious && !visible.HasValue)
            {
                ClueDebugLog.Suspect(
                    $"프레임 {frame}에 오버레이가 열리자마자 닫혔다 — 화면을 연 그 누름이 " +
                    "닫기로도 쓰인 것이다. 밖에서는 \"눌러도 안 열린다\"로만 보인다.");
            }
        }

        private void Unsubscribe()
        {
            if (_subscribedHost == null)
                return;

            _subscribedHost.VisibleChanged -= OnVisibleChanged;
            _subscribedHost = null;
        }

        public void Dispose() => Unsubscribe();
    }
}
