using System;
using System.Collections.Generic;
using GameName.Core.Clues;
using GameName.Core.Commissions;
using GameName.Core.Events;
using GameName.Core.Inventory;
using GameName.Core.MemoryRooms;

namespace GameName.UI.Flow
{
    // 복귀 확인 패널의 입력 처리를 담당한다. "계단에 서 있는가"는 판단하지
    // 않는다 — 이 패널 자체가 FlowOverlayController에 의해 그 조건일 때만
    // 보이도록 이미 걸러져 있고, 실제 전환 가부(단계가 InMemory인가, 정말
    // 계단 위인가)는 CommissionSession.TryReturnToReality가 다시 확인한다.
    //
    // "지금 나가면 무엇을 잃는지"를 정신력 상태가 아니라 실제로 남은 일
    // (미분석 단서/미복원 방 개수)로 보여준다 — 이 게임은 자원을 끝까지
    // 소진해야 나가는 게임이 아니라, 충분하다고 판단하면 언제든 나오는
    // 추리 게임이다. 그래서 정신력이 남아 있어도 이탈은 항상 자연스러운
    // 선택이며, 이 패널은 정신력 잔량을 조건으로 안내 문구를 바꾸지 않는다.
    // 정답에 해당하는 정보(어떤 단서인지, 어떤 방의 진실인지)는 개수 계산
    // 과정에서도 절대 이 타입에 들어오지 않는다 — MemoryExitSummaryCalculator가
    // 넘겨주는 것은 숫자 두 개뿐이다.
    public sealed class ReturnToRealityPanelController : IDisposable
    {
        private readonly ReturnToRealityPanelView _view;
        private readonly CommissionSession _commissionSession;
        private readonly IPlayerInventory _inventory;
        private readonly IClueStorage _clueStorage;
        private readonly IClueAnalysisProgress _analysisProgress;
        private readonly IReadOnlyList<MemoryRoomId> _roomIds;
        private readonly IMemoryRoomRestorationTracker _restorationTracker;

        private readonly IDisposable _clueAnalyzedSubscription;
        private readonly IDisposable _roomRestoredSubscription;

        public ReturnToRealityPanelController(
            ReturnToRealityPanelView view,
            CommissionSession commissionSession,
            IPlayerInventory inventory,
            IClueStorage clueStorage,
            IClueAnalysisProgress analysisProgress,
            IReadOnlyList<MemoryRoomId> roomIds,
            IMemoryRoomRestorationTracker restorationTracker,
            IEventBus eventBus)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _commissionSession = commissionSession ?? throw new ArgumentNullException(nameof(commissionSession));
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            _clueStorage = clueStorage ?? throw new ArgumentNullException(nameof(clueStorage));
            _analysisProgress = analysisProgress ?? throw new ArgumentNullException(nameof(analysisProgress));
            _roomIds = roomIds ?? throw new ArgumentNullException(nameof(roomIds));
            _restorationTracker = restorationTracker ?? throw new ArgumentNullException(nameof(restorationTracker));
            if (eventBus == null) throw new ArgumentNullException(nameof(eventBus));

            _view.ExitRequested += OnExitRequested;
            _view.ExitConfirmed += OnExitConfirmed;
            _view.ExitCancelled += OnExitCancelled;
            _clueAnalyzedSubscription = eventBus.Subscribe<ClueAnalyzedEvent>(_ => RefreshSummary());
            _roomRestoredSubscription = eventBus.Subscribe<MemoryRoomRestoredEvent>(_ => RefreshSummary());

            RefreshSummary();
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

        private void RefreshSummary()
        {
            var summary = MemoryExitSummaryCalculator.Calculate(
                _inventory, _clueStorage, _analysisProgress, _roomIds, _restorationTracker);

            _view.SetSummary(FormatSummary(summary));
        }

        private static string FormatSummary(MemoryExitSummary summary)
        {
            if (summary.UnanalyzedClueCount == 0 && summary.UnrestoredRoomCount == 0)
                return "더 살펴볼 단서나 방이 남아 있지 않습니다.";

            return
                $"지금 나가면 미분석 단서 {summary.UnanalyzedClueCount}개, " +
                $"복원하지 못한 방 {summary.UnrestoredRoomCount}개를 다시 볼 수 없습니다.";
        }

        public void Dispose()
        {
            _view.ExitRequested -= OnExitRequested;
            _view.ExitConfirmed -= OnExitConfirmed;
            _view.ExitCancelled -= OnExitCancelled;
            _clueAnalyzedSubscription.Dispose();
            _roomRestoredSubscription.Dispose();
        }
    }
}
