using System;
using GameName.Core.Events;
using GameName.Core.Hiromi;

namespace GameName.UI.Inventory
{
    // 가방 화면의 "다음 기억으로" 레버 Core 연동.
    //
    // 히로민이 이동 문턱 이상이면 곧장 MemoryMoveProcessor.Move()를 부른다 —
    // 확인할 것이 없다. 문턱보다 모자라면 그 자리에서 강제로 밀어붙이지 않고
    // 먼저 묻는다: 기회를 하나 써서 지금 갈 것인가, 아니면 방으로 돌아가 더
    // 대화해 히로민을 모을 것인가. 취소를 고르면 Move()를 아예 부르지 않으므로
    // 아무것도 소모되지 않는다 — 확인은 순전히 화면 쪽 판단이고, Move() 자체는
    // 언제나 무조건 실행되는 행동이기 때문이다(그 안에서 강제/일반을 가르는
    // 것은 MemoryMoveProcessor의 몫).
    public sealed class MemoryMoveLeverController : IDisposable
    {
        private readonly IMemoryMoveLeverView _view;
        private readonly MemoryMoveProcessor _memoryMove;
        private readonly IHiromiReader _hiromi;
        private readonly IChanceReader _chance;
        private readonly int _hiromiMoveThreshold;
        private readonly IDisposable _runEndedSubscription;

        public MemoryMoveLeverController(
            IMemoryMoveLeverView view,
            MemoryMoveProcessor memoryMove,
            IHiromiReader hiromi,
            IChanceReader chance,
            int hiromiMoveThreshold,
            IEventBus eventBus)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _memoryMove = memoryMove ?? throw new ArgumentNullException(nameof(memoryMove));
            _hiromi = hiromi ?? throw new ArgumentNullException(nameof(hiromi));
            _chance = chance ?? throw new ArgumentNullException(nameof(chance));
            _hiromiMoveThreshold = hiromiMoveThreshold;
            if (eventBus == null) throw new ArgumentNullException(nameof(eventBus));

            _view.MoveClicked += OnMoveClicked;
            _view.ForceConfirmed += OnForceConfirmed;
            _view.ForceCancelled += OnForceCancelled;

            // 런이 끝나면 더 이동할 곳이 없다.
            _runEndedSubscription = eventBus.Subscribe<RunCompletedEvent>(_ => _view.SetMoveEnabled(false));
        }

        private void OnMoveClicked()
        {
            if (_hiromi.Remaining >= _hiromiMoveThreshold)
            {
                _memoryMove.Move();
                return;
            }

            _view.ShowForceConfirm(
                $"히로민이 모자랍니다({_hiromi.Remaining}/{_hiromiMoveThreshold}). " +
                $"기회를 하나 써서 강제로 이동하시겠습니까? (남은 기회 {_chance.Remaining})");
        }

        private void OnForceConfirmed()
        {
            _view.HideForceConfirm();
            _memoryMove.Move();
        }

        private void OnForceCancelled()
        {
            _view.HideForceConfirm();
        }

        public void Dispose()
        {
            _view.MoveClicked -= OnMoveClicked;
            _view.ForceConfirmed -= OnForceConfirmed;
            _view.ForceCancelled -= OnForceCancelled;
            _runEndedSubscription.Dispose();
        }
    }
}
