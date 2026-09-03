using System;
using GameName.Core.Clues;
using GameName.Core.Inventory;

namespace GameName.UI.Inventory
{
    // 인벤토리(가방) 오버레이의 Core 연동.
    //
    // 이제 인벤토리는 표시 전용이다 — 단서를 담고 빼는 것은 전부 ClueState의
    // 투영(InventoryProjection)이 수집·추출 사건을 듣고 처리한다.
    //
    // 이 화면은 오버레이라 열려 있는 동안 인벤토리가 바깥에서 바뀔 일이 없다.
    // 그래서 이벤트를 상시 구독하지 않고 열릴 때 Refresh만 한다(가시성을
    // 관리하는 OverlayPanelHost가 그 시점에 불러 준다).
    //
    // 채워진 칸을 누르면 그 단서를 ClueUseRequested로 올린다 — "대화에 사용 /
    // 기억 추출"을 고르는 패널은 별도 컨트롤러가 소유한다.
    public sealed class InventoryScreenController : IDisposable
    {
        private readonly InventoryScreenView _view;
        private readonly IPlayerInventory _inventory;

        public event Action<ClueInfo> ClueUseRequested;

        public InventoryScreenController(InventoryScreenView view, IPlayerInventory inventory)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));

            _view.SlotClicked += OnSlotClicked;
        }

        public void Refresh()
        {
            _view.SetSlots(_inventory.Items, _inventory.Capacity);
            _view.SetMessage(null);
        }

        private void OnSlotClicked(int index)
        {
            if (index < 0 || index >= _inventory.Items.Count)
                return;

            if (_inventory.Items[index] is ClueInfo clue)
                ClueUseRequested?.Invoke(clue);
        }

        public void Dispose()
        {
            _view.SlotClicked -= OnSlotClicked;
        }
    }
}
