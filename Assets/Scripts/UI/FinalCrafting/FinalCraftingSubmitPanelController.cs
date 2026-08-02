using System;
using System.Collections.Generic;
using GameName.Core.Commissions;
using GameName.Core.FinalCrafting;
using GameName.Core.MemoryRooms;
using GameName.Core.Rewards;

namespace GameName.UI.FinalCrafting
{
    // 제출 패널의 입력 처리와 Core 연동을 담당한다. "모든 방이 채워졌는가"는
    // IFinalCraftingBoard.IsCompleteFor에게 그대로 물어보고, 실제 제공은
    // CommissionCompletionProcessor 한 곳에서만 처리한다 — 이 컨트롤러는 그
    // 결과를 화면에 옮길 뿐이다.
    public sealed class FinalCraftingSubmitPanelController : IDisposable
    {
        private readonly FinalCraftingSubmitPanelView _view;
        private readonly IFinalCraftingBoard _board;
        private readonly IReadOnlyList<MemoryRoomId> _roomIds;
        private readonly CommissionCompletionProcessor _completionProcessor;
        private readonly Func<RewardTable> _rewardTableProvider;

        // 완료 화면이 결과를 보여줘야 하므로, 성공한 결과를 밖으로 알린다 —
        // 화면 전환 자체는 CommissionStageChangedEvent를 구독하는
        // SceneScreenSwitcher가 하고, 이 이벤트는 오직 "보여줄 결과 데이터를
        // 어딘가에 보관해 달라"는 목적으로만 쓰인다.
        public event Action<CommissionCompletionResult> Completed;

        public FinalCraftingSubmitPanelController(
            FinalCraftingSubmitPanelView view,
            IFinalCraftingBoard board,
            IReadOnlyList<MemoryRoomId> roomIds,
            CommissionCompletionProcessor completionProcessor,
            Func<RewardTable> rewardTableProvider)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _board = board ?? throw new ArgumentNullException(nameof(board));
            _roomIds = roomIds ?? throw new ArgumentNullException(nameof(roomIds));
            _completionProcessor = completionProcessor ?? throw new ArgumentNullException(nameof(completionProcessor));
            _rewardTableProvider = rewardTableProvider ?? throw new ArgumentNullException(nameof(rewardTableProvider));

            _view.ProvideRequested += OnProvideRequested;
            Refresh();
        }

        public void Refresh()
        {
            var filledCount = 0;
            foreach (var roomId in _roomIds)
            {
                if (_board.TryGet(roomId, out _))
                    filledCount++;
            }

            _view.SetStatus(filledCount, _roomIds.Count);
            _view.SetProvideButtonEnabled(_board.IsCompleteFor(_roomIds));
        }

        private void OnProvideRequested()
        {
            // 성공하면 CommissionSession의 단계가 Completed로 바뀌고, 그 변화를
            // 구독하는 SceneScreenSwitcher가 완료 화면으로 전환한다 — 이 컨트롤러가
            // 직접 화면을 바꾸지 않는다.
            var result = _completionProcessor.Complete(_roomIds, _rewardTableProvider());
            if (!result.Succeeded)
            {
                _view.SetResultMessage(DescribeFailure(result.FailureReason.Value));
                return;
            }

            _view.SetResultMessage(null);
            Completed?.Invoke(result);
        }

        private static string DescribeFailure(CommissionCompletionFailureReason reason)
        {
            switch (reason)
            {
                case CommissionCompletionFailureReason.RoomsIncomplete: return "아직 모든 방의 최종 향이 확정되지 않았습니다.";
                case CommissionCompletionFailureReason.WrongStage: return "지금은 제공할 수 없는 시점입니다.";
                default: return "제공에 실패했습니다.";
            }
        }

        public void Dispose()
        {
            _view.ProvideRequested -= OnProvideRequested;
        }
    }
}
