using System;
using GameName.Core.Clues;
using GameName.Core.Inventory;

namespace GameName.UI.MemoryRoom
{
    // 인벤토리 패널의 Core 연동을 담당한다. 이 패널 자체를 바꾸는 행동은
    // "되돌리기" 하나뿐이다(습득은 단서 패널, 소모는 시향 패널의 몫) — 그래서
    // 습득/시향에 대해서는 자체 구독 없이 화면 컨트롤러가 Refresh()를 불러
    // 주는 방식을 그대로 유지하고, 되돌리기만 이 컨트롤러가 직접 처리한다.
    //
    // 알려진 한계: 다른 화면(예: 조향실)에서 이 인벤토리로 앰플이 옮겨져도
    // 이 패널이 자동으로 갱신되지는 않는다 — 화면 전환은 이번 작업 범위가
    // 아니며, AmpouleTransferProcessor는 현재 이벤트를 발행하지 않는다. 필요해
    // 지면 인벤토리 변경 이벤트를 Core에 추가해야 한다.
    public sealed class MemoryRoomInventoryPanelController : IDisposable
    {
        private readonly MemoryRoomInventoryPanelView _view;
        private readonly IPlayerInventory _inventory;
        private readonly ClueReturnProcessor _returnProcessor;

        public MemoryRoomInventoryPanelController(
            MemoryRoomInventoryPanelView view, IPlayerInventory inventory, ClueReturnProcessor returnProcessor)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            _returnProcessor = returnProcessor ?? throw new ArgumentNullException(nameof(returnProcessor));

            _view.ClueReturnRequested += OnClueReturnRequested;

            Refresh();
        }

        public void Refresh() => _view.SetSlots(_inventory.Items, _inventory.Capacity);

        private void OnClueReturnRequested(ClueId clueId)
        {
            if (!TryFindClue(clueId, out var clue))
                return;

            var result = _returnProcessor.Return(clue);
            _view.SetReturnFailureMessage(result.Succeeded ? null : DescribeFailure(result.FailureReason.Value));

            if (result.Succeeded)
                Refresh();
        }

        private bool TryFindClue(ClueId clueId, out ClueInfo clue)
        {
            foreach (var item in _inventory.Items)
            {
                if (item is ClueInfo candidate && candidate.Id.Equals(clueId))
                {
                    clue = candidate;
                    return true;
                }
            }

            clue = null;
            return false;
        }

        private static string DescribeFailure(ClueReturnFailureReason reason)
        {
            switch (reason)
            {
                case ClueReturnFailureReason.ClueNotInInventory: return "이미 옮겨졌거나 찾을 수 없는 단서입니다.";
                case ClueReturnFailureReason.WrongRoom: return "이 방의 단서가 아닙니다.";
                default: return "되돌리기에 실패했습니다.";
            }
        }

        public void Dispose()
        {
            _view.ClueReturnRequested -= OnClueReturnRequested;
        }
    }
}
