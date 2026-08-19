using System;
using System.Collections.Generic;
using System.Reflection;
using GameName.UI.MemoryRoom;
using GameName.UI.Overlays;

namespace GameName.UI.Diagnostics
{
    // 확대 화면이 열리기까지의 배선이 지금 실제로 이어져 있는지 읽는다.
    //
    // 앞선 보고로 범위가 여기까지 좁혀졌다: 눌린 단서는 뷰까지 왔고, 방
    // 컨트롤러도 그 단서를 알아봤다(못 알아봤다면 HUD에 안내가 떴을 것이다).
    // 그런데 확대 화면이 열리지 않았다. 그렇다면 남은 곳은 "방 컨트롤러의 알림을
    // 듣는 쪽"뿐이다.
    //
    // 그 지점을 또 코드만 읽고 추측하지 않는다. 이번 작업에서 추측은 이미 두 번
    // 빗나갔고, 두 번 다 "코드상 그럴 리 없다"는 판단이었다. 그래서 실행 중의
    // 값을 그대로 읽는다.
    //
    //   · MemoryRoomBootstrap._screenController가 null인가
    //     → null이면 OnEnable이 중간에 끊긴 것이다. 방 컨트롤러는 그보다 먼저
    //       만들어지므로 방은 정상적으로 그려지고 마우스 판정도 살아 있다.
    //       "눌리는데 아무 일도 안 일어난다"가 정확히 이 모습이다.
    //   · 방 컨트롤러의 ClueActivated에 듣는 사람이 몇 명인가
    //     → 0이면 알림이 허공으로 간다. C#의 ?.Invoke는 구독자가 없어도 조용히
    //       지나가므로 이 상태는 아무 흔적도 남기지 않는다.
    //   · 확대 화면이 오버레이 목록에 등록되어 있는가
    //     → 없으면 Show()가 조용히 무시된다(라우터가 그렇게 만들어져 있다).
    //
    // 셋 다 "조용히 실패하는" 상태라서 화면에도 콘솔에도 아무것도 남기지 않는다.
    // 그래서 눈으로는 영영 찾을 수 없고, 이렇게 직접 읽는 수밖에 없다.
    internal sealed class ScreenWiringReporter
    {
        private const BindingFlags Instance = BindingFlags.NonPublic | BindingFlags.Instance;

        private readonly ClueDebugContext _context;
        private string _lastReport = "";

        public ScreenWiringReporter(ClueDebugContext context)
        {
            _context = context;
        }

        // 값이 달라졌을 때만 찍는다 — 매 프레임 같은 줄을 쌓으면 정작 달라지는
        // 순간을 놓친다.
        public void Tick()
        {
            var report = Describe();
            if (report == _lastReport)
                return;

            _lastReport = report;
            ClueDebugLog.Write($"화면배선 | {report}");

            if (report.Contains("끊김") || report.Contains("듣는사람=0") || report.Contains("등록안됨"))
                ClueDebugLog.Suspect($"확대 화면으로 가는 배선이 끊겨 있다: {report}");
        }

        private string Describe()
        {
            var bootstrap = _context.RoomBootstrap;
            if (bootstrap == null)
                return "기억 방 화면을 찾지 못함";

            if (!bootstrap.isActiveAndEnabled)
                return "기억 방 화면이 꺼져 있음";

            var screenController = ReadField(bootstrap, "_screenController");
            if (screenController == null)
                return "화면컨트롤러=없음(OnEnable이 끝까지 돌지 않고 중간에 끊김)";

            var parts = new List<string> { "화면컨트롤러=있음" };

            var space = ReadField(screenController, "_space");
            parts.Add(space == null
                ? "방컨트롤러=없음"
                : $"단서알림 듣는사람={SubscriberCountOf(space, "ClueActivated")}");

            parts.Add($"오버레이등록={DescribeRegistered()}");

            return string.Join(", ", parts);
        }

        // 확대 화면이 라우터에 등록되어 있지 않으면 Show()는 아무 일도 하지 않는다.
        private string DescribeRegistered()
        {
            if (_context.OverlayPanels == null)
                return "(호스트 없음)";

            var contents = ReadField(_context.OverlayPanels, "_contents") as System.Collections.IDictionary;
            if (contents == null)
                return "(목록을 읽지 못함)";

            var registered = new List<string>();
            foreach (var key in contents.Keys)
                registered.Add(key.ToString());

            if (!registered.Contains(OverlayPanel.ClueZoom.ToString()))
                return $"확대화면 등록안됨 [{string.Join("/", registered)}]";

            return string.Join("/", registered);
        }

        private static int SubscriberCountOf(object target, string eventFieldName)
        {
            var handler = ReadField(target, eventFieldName) as Delegate;
            return handler == null ? 0 : handler.GetInvocationList().Length;
        }

        private static object ReadField(object target, string fieldName)
        {
            if (target == null)
                return null;

            var type = target.GetType();
            while (type != null)
            {
                var field = type.GetField(fieldName, Instance);
                if (field != null)
                    return field.GetValue(target);

                type = type.BaseType;
            }

            return null;
        }
    }
}
