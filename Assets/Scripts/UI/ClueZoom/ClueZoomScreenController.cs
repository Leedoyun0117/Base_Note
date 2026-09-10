using System;
using GameName.Core.Clues;
using GameName.Core.Complexes;
using GameName.Core.Events;

namespace GameName.UI.ClueZoom
{
    // 스토리 패널의 Core 연동.
    //
    // 방에서 단서를 누르면 Read()가 불려 ClueUseProcessor.Use()를 거친다 —
    // 인벤토리에 직접 담거나 상태를 직접 바꾸지 않는다. 성공하면 Core가 낸
    // 사건(ClueUsedEvent = 서사, ClueInterpretedEvent = 태그 체인)을 듣고
    // 패널을 채운다. 실패하면 이유만 안내한다.
    public sealed class ClueZoomScreenController : IDisposable
    {
        private readonly ClueZoomScreenView _view;
        private readonly ClueUseProcessor _clueUse;
        private readonly IDisposable[] _subscriptions;

        // 지금 이 컨트롤러가 연 읽기인지 — 다른 경로로 난 사건에 반응하지 않게 한다.
        private ClueId? _pending;

        // 단서를 실제로 읽었다 — 방 화면이 그 단서를 지워야 한다.
        public event Action ClueRead;

        // 이 화면을 닫아 달라는 요청.
        public event Action CloseRequested;

        public ClueZoomScreenController(
            ClueZoomScreenView view, ClueUseProcessor clueUse, IEventBus eventBus)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _clueUse = clueUse ?? throw new ArgumentNullException(nameof(clueUse));
            if (eventBus == null) throw new ArgumentNullException(nameof(eventBus));

            _view.ExitRequested += OnExitRequested;

            _subscriptions = new[]
            {
                eventBus.Subscribe<ClueUsedEvent>(OnClueUsed),
                eventBus.Subscribe<ClueInterpretedEvent>(OnClueInterpreted),
            };
        }

        // [수집]을 눌렀다. 화면 없이 규칙을 확인할 수 있도록 공개 메서드로도 열어 둔다.
        public void Read(ClueInfo clue)
        {
            if (clue == null) throw new ArgumentNullException(nameof(clue));

            _pending = clue.Id;
            _view.BeginRead(clue.DisplayName);

            var result = _clueUse.Use(clue.Id);
            if (!result.Succeeded)
            {
                _pending = null;
                _view.SetMessage(DescribeFailure(result.FailureReason));
            }
        }

        public void OnHidden()
        {
            _pending = null;
            _view.SetMessage(null);
        }

        private void OnClueUsed(ClueUsedEvent e)
        {
            if (_pending == null || !_pending.Value.Equals(e.ClueId))
                return;

            _view.SetStory(e.Story);
            ClueRead?.Invoke();
        }

        private void OnClueInterpreted(ClueInterpretedEvent e)
        {
            // ClueUseProcessor는 해석 → 사용 순서로 발행한다. _pending은 여기서
            // 지우지 않는다 — 뒤이어 올 ClueUsedEvent가 서사를 채워야 하므로.
            if (_pending == null)
                return;

            _view.SetInterpretation(e.SourceTags, e.Steps, e.FinalTags);
        }

        private void OnExitRequested()
        {
            _pending = null;
            CloseRequested?.Invoke();
        }

        private static string DescribeFailure(ClueUseFailureReason? reason)
        {
            switch (reason)
            {
                case ClueUseFailureReason.NotAvailable:
                    return "이미 읽었거나 이 라운드의 단서가 아닙니다.";
                default:
                    return "이 단서는 지금 읽을 수 없습니다.";
            }
        }

        public void Dispose()
        {
            _view.ExitRequested -= OnExitRequested;
            foreach (var subscription in _subscriptions)
                subscription.Dispose();
        }
    }
}
