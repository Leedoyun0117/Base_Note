using System;
using GameName.Core.Commissions;
using GameName.Core.Events;
using GameName.Core.Mentality;

namespace GameName.UI.Flow
{
    // 복귀 확인 패널의 입력 처리를 담당한다. "계단에 서 있는가"는 판단하지
    // 않는다 — 이 패널 자체가 FlowOverlayController에 의해 그 조건일 때만
    // 보이도록 이미 걸러져 있고, 실제 전환 가부(단계가 InMemory인가, 정말
    // 계단 위인가)는 CommissionSession.TryReturnToReality가 다시 확인한다.
    //
    // 정신력이 0이면 복귀를 권하는 안내만 보여준다 — 강제로 내보내지 않는다
    // (소지한 앰플로는 계속 시향할 수 있어야 하므로, 이 패널이 할 일은 안내뿐이다).
    public sealed class ReturnToRealityPanelController : IDisposable
    {
        private readonly ReturnToRealityPanelView _view;
        private readonly CommissionSession _commissionSession;
        private readonly IMentalityGauge _mentalityGauge;
        private readonly IDisposable _mentalitySubscription;

        public ReturnToRealityPanelController(
            ReturnToRealityPanelView view,
            CommissionSession commissionSession,
            IMentalityGauge mentalityGauge,
            IEventBus eventBus)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _commissionSession = commissionSession ?? throw new ArgumentNullException(nameof(commissionSession));
            _mentalityGauge = mentalityGauge ?? throw new ArgumentNullException(nameof(mentalityGauge));
            if (eventBus == null) throw new ArgumentNullException(nameof(eventBus));

            _view.ExitRequested += OnExitRequested;
            _view.ExitConfirmed += OnExitConfirmed;
            _view.ExitCancelled += OnExitCancelled;
            _mentalitySubscription = eventBus.Subscribe<MentalityChangedEvent>(_ => RefreshMentalityNotice());

            RefreshMentalityNotice();
        }

        private void OnExitRequested() => _view.SetConfirmVisible(true);

        private void OnExitCancelled() => _view.SetConfirmVisible(false);

        private void OnExitConfirmed()
        {
            _view.SetConfirmVisible(false);
            _commissionSession.TryReturnToReality();
            // 성공하면 FlowOverlayController가 단계 변경 이벤트로 이 패널
            // 자체를 숨긴다.
        }

        private void RefreshMentalityNotice()
        {
            _view.SetMentalityNotice(_mentalityGauge.CanAct
                ? null
                : "정신력이 없습니다. 소지한 앰플로는 계속 시향할 수 있지만, 더 할 수 있는 일이 많지 않습니다. 복귀를 권장합니다.");
        }

        public void Dispose()
        {
            _view.ExitRequested -= OnExitRequested;
            _view.ExitConfirmed -= OnExitConfirmed;
            _view.ExitCancelled -= OnExitCancelled;
            _mentalitySubscription.Dispose();
        }
    }
}
