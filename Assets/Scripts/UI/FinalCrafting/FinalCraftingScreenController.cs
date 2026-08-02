using System;
using GameName.Core.MemoryRooms;
using GameName.UI.Perfumery;

namespace GameName.UI.FinalCrafting
{
    // 세 패널(방 선택, 최종 조향, 제출)을 조립하고 패널을 넘나드는 정보만
    // 중개한다 — 조향실 화면(PerfumeryScreenController)과 같은 역할이다.
    public sealed class FinalCraftingScreenController : IDisposable
    {
        private readonly RoomSelectionPanelController _roomPanel;
        private readonly FinalCraftingPanelController _craftingPanel;
        private readonly FinalCraftingSubmitPanelController _submitPanel;

        public FinalCraftingScreenController(
            RoomSelectionPanelController roomPanel,
            FinalCraftingPanelController craftingPanel,
            FinalCraftingSubmitPanelController submitPanel)
        {
            _roomPanel = roomPanel ?? throw new ArgumentNullException(nameof(roomPanel));
            _craftingPanel = craftingPanel ?? throw new ArgumentNullException(nameof(craftingPanel));
            _submitPanel = submitPanel ?? throw new ArgumentNullException(nameof(submitPanel));

            _roomPanel.SelectedRoomChanged += OnSelectedRoomChanged;
            _craftingPanel.RoomFinalized += OnRoomFinalized;
        }

        private void OnSelectedRoomChanged(MemoryRoomId roomId, MemoryRoomPublicInfo info) =>
            _craftingPanel.SetTargetRoom(roomId, info.RequiredSupportingIntensityTotal);

        private void OnRoomFinalized() => _submitPanel.Refresh();

        public void Dispose()
        {
            _roomPanel.SelectedRoomChanged -= OnSelectedRoomChanged;
            _craftingPanel.RoomFinalized -= OnRoomFinalized;

            _roomPanel.Dispose();
            _craftingPanel.Dispose();
            _submitPanel.Dispose();
        }
    }
}
