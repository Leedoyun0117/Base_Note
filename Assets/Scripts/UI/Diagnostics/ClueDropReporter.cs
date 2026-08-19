using System;
using System.Text;
using GameName.Core.Clues;
using GameName.Core.Events;
using GameName.UI.MemoryRoom.Space;

namespace GameName.UI.Diagnostics
{
    // 3번 관찰: 단서를 버렸을 때 Core·계산·씬이 서로 같은 말을 하는가.
    //
    // 보고를 이벤트가 오는 그 자리에서 하지 않고 그 프레임의 끝으로 미룬다.
    // 버린 자리를 기록하고 방을 다시 그리는 것은 같은 이벤트의 다른 구독자
    // (MemoryRoomSpaceController)가 하는 일이라, 구독 순서에 따라 아직 아무것도
    // 일어나지 않은 상태를 볼 수 있기 때문이다. 프레임 끝에서 보면 그 프레임에
    // 일어날 일은 전부 끝나 있다.
    internal sealed class ClueDropReporter : IDisposable
    {
        private readonly ClueDebugContext _context;
        private IDisposable _subscription;
        private bool _hasPending;
        private ClueDroppedEvent _pending;

        public ClueDropReporter(ClueDebugContext context)
        {
            _context = context;
        }

        // 세션은 씬이 뜬 뒤에야 만들어지므로 구독도 늦게, 그리고 끊겼으면 다시
        // 건다 — 진단이 조용히 멈추는 것을 막기 위한 같은 이유다.
        public void EnsureSubscribed()
        {
            if (_subscription != null)
                return;

            var session = _context.Session;
            if (session == null)
                return;

            _subscription = session.EventBus.Subscribe<ClueDroppedEvent>(OnClueDropped);
        }

        private void OnClueDropped(ClueDroppedEvent dropped)
        {
            _hasPending = true;
            _pending = dropped;
        }

        public void Tick()
        {
            if (!_hasPending)
                return;

            _hasPending = false;
            Report(_pending);
            ClueDropDebugHook.Clear();
        }

        private void Report(ClueDroppedEvent dropped)
        {
            var line = new StringBuilder();
            line.Append($"버리기 | {dropped.ClueId.Value} → 방 {dropped.RoomId.Value}");
            line.Append($" | Core 결과={DescribeCoreResult(dropped)}");
            line.Append($" | {DescribePlacement(dropped)}");
            line.Append($" | {DescribeSceneObject(dropped)}");

            ClueDebugLog.Write(line.ToString());
        }

        // 화면을 거치지 않고 버린 경우(테스트 등)에는 후크에 기록이 없다.
        // 그 사실을 감추지 않고 그대로 적는다.
        private static string DescribeCoreResult(ClueDroppedEvent dropped)
        {
            if (!ClueDropDebugHook.HasResult || !ClueDropDebugHook.LastClueId.Equals(dropped.ClueId))
                return "성공(이벤트로 확인, 화면을 거치지 않음)";

            return ClueDropDebugHook.Succeeded
                ? "성공"
                : $"실패({ClueDropDebugHook.FailureReason})";
        }

        // 계산된 배치 비율과 그 비율이 실제로 어느 좌표를 가리키는지.
        private string DescribePlacement(ClueDroppedEvent dropped)
        {
            var session = _context.Session;
            var layout = _context.Layout;

            if (session == null || layout == null)
                return "배치=세션이나 방 치수를 읽지 못함";

            if (!session.DroppedCluePositions.TryGet(dropped.ClueId, out var ratio))
                return "배치=버린 자리가 기록되지 않음";

            var kinds = _context.CurrentRoomClues();
            if (!kinds.TryGet(dropped.ClueId, out var info))
                return $"배치=비율 {ratio} (종류를 몰라 좌표를 계산하지 못함)";

            var position = CluePlacementLayout.PositionAt(layout, info.Kind, ratio);
            var playerX = _context.View == null ? 0f : _context.View.PlayerX;

            return $"배치=비율 {ratio} → 좌표({position.x:0.###},{position.y:0.###}), 플레이어x={playerX:0.###}";
        }

        // 계산이 맞아도 오브젝트가 안 생겼을 수 있고, 생겼어도 눈에 안 보일 수
        // 있다. 그래서 존재 여부와 좌표·콜라이더를 함께 적는다.
        private string DescribeSceneObject(ClueDroppedEvent dropped)
        {
            var kinds = _context.CurrentRoomClues();

            foreach (var state in ClueSceneSnapshot.Capture(_context.View))
            {
                if (!state.ClueIdRead || !state.ClueId.Equals(dropped.ClueId))
                    continue;

                return $"씬오브젝트=생성됨 {state.Describe(kinds.KindTextOf(state))}";
            }

            return "씬오브젝트=생성되지 않음";
        }

        public void Dispose()
        {
            _subscription?.Dispose();
            _subscription = null;
        }
    }
}
