using System;
using GameName.Core.Events;

namespace GameName.UI.Diagnostics
{
    // 4번 관찰: 방을 옮긴 직후 씬에 무엇이 남아 있는가.
    //
    // 이동 이벤트가 온 그 자리에서 한 번, 그리고 그 프레임의 끝에서 한 번 —
    // 두 번 찍는다. 두 값이 다르면 그 차이 자체가 답이다. 이동 직후에는 이전
    // 방의 오브젝트가 남아 있다가 프레임 끝에 사라진다면 "그 프레임 동안에는
    // 마우스가 이전 방 단서를 집을 수 있었다"는 뜻이고, 반대로 프레임이 끝나도
    // 그대로면 파괴 자체가 일어나지 않은 것이다.
    //
    // 이 구분은 프레임을 넘긴 뒤에만 확인하는 테스트로는 절대 볼 수 없는 것이라,
    // 눈으로 직접 봐야 한다.
    internal sealed class RoomChangeReporter : IDisposable
    {
        private readonly ClueDebugContext _context;
        private IDisposable _subscription;
        private bool _hasPending;

        public RoomChangeReporter(ClueDebugContext context)
        {
            _context = context;
        }

        public void EnsureSubscribed()
        {
            if (_subscription != null)
                return;

            var session = _context.Session;
            if (session == null)
                return;

            _subscription = session.EventBus.Subscribe<MemoryRoomMoveCompletedEvent>(OnMoveCompleted);
        }

        private void OnMoveCompleted(MemoryRoomMoveCompletedEvent moved)
        {
            var kinds = _context.CurrentRoomClues();
            var states = ClueSceneSnapshot.Capture(_context.View);

            ClueDebugLog.Write(
                $"방이동 | {moved.PreviousPosition.Value} → {moved.NewPosition.Value}" +
                $" | 이동 직후 씬 단서 {states.Count}개: {ClueSceneSnapshot.Describe(states, kinds)}");

            _hasPending = true;
        }

        public void Tick()
        {
            if (!_hasPending)
                return;

            _hasPending = false;

            var kinds = _context.CurrentRoomClues();
            var states = ClueSceneSnapshot.Capture(_context.View);

            ClueDebugLog.Write(
                $"방이동(프레임 끝) | 위치={_context.CurrentPlaceText()}" +
                $" | 씬 단서 {states.Count}개: {ClueSceneSnapshot.Describe(states, kinds)}");
        }

        public void Dispose()
        {
            _subscription?.Dispose();
            _subscription = null;
        }
    }
}
