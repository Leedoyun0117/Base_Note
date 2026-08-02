using System;
using GameName.Core.Inventory;

namespace GameName.UI.MemoryRoom
{
    // 인벤토리 패널의 Core 연동을 담당한다. 이 패널 자체를 바꾸는 행동은 이
    // 화면에 없다(습득은 단서 패널, 소모는 시향 패널의 몫) — 그래서 자체
    // 구독 없이, 화면 컨트롤러가 그 두 패널의 변경 알림을 듣고 Refresh()를
    // 호출해 주는 방식으로만 갱신된다.
    //
    // 알려진 한계: 다른 화면(예: 조향실)에서 이 인벤토리로 앰플이 옮겨져도
    // 이 패널이 자동으로 갱신되지는 않는다 — 화면 전환은 이번 작업 범위가
    // 아니며, AmpouleTransferProcessor는 현재 이벤트를 발행하지 않는다. 필요해
    // 지면 인벤토리 변경 이벤트를 Core에 추가해야 한다.
    public sealed class MemoryRoomInventoryPanelController
    {
        private readonly MemoryRoomInventoryPanelView _view;
        private readonly IPlayerInventory _inventory;

        public MemoryRoomInventoryPanelController(MemoryRoomInventoryPanelView view, IPlayerInventory inventory)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));

            Refresh();
        }

        public void Refresh() => _view.SetSlots(_inventory.Items, _inventory.Capacity);
    }
}
