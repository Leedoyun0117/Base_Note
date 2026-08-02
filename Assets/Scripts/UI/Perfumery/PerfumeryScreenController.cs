using System;
using GameName.Core.Ampoules;
using GameName.Core.MemoryRooms;

namespace GameName.UI.Perfumery
{
    // 세 패널 컨트롤러를 조립하고, 패널을 넘나드는 정보만 중개한다:
    // 선택된 방(좌측) -> 목표 방/요구 총량(중앙). 우측(상태)은 더 이상 다른
    // 패널의 상태에 의존하지 않는다 — 정신력/보관함/인벤토리는 전부 Core
    // 이벤트로 스스로 갱신하고, 옮기기도 자기 안에서 완결된다.
    //
    // 이 클래스도 게임 규칙을 계산하지 않는다 — 언제나 각 패널이나 Core
    // 처리기가 이미 낸 결론을 그대로 전달만 한다. AmpouleCraftingProcessor를
    // 실제로 호출하는 유일한 지점이며, 그 결과(성공/실패 사유)를 조향
    // 패널(대기열이 있는 곳)에 그대로 넘긴다.
    public sealed class PerfumeryScreenController : IDisposable
    {
        private readonly RoomSelectionPanelController _roomPanel;
        private readonly PerfumeryCompositionPanelController _compositionPanel;
        private readonly StatusPanelController _statusPanel;
        private readonly AmpouleCraftingProcessor _craftingProcessor;

        public PerfumeryScreenController(
            RoomSelectionPanelController roomPanel,
            PerfumeryCompositionPanelController compositionPanel,
            StatusPanelController statusPanel,
            AmpouleCraftingProcessor craftingProcessor)
        {
            _roomPanel = roomPanel ?? throw new ArgumentNullException(nameof(roomPanel));
            _compositionPanel = compositionPanel ?? throw new ArgumentNullException(nameof(compositionPanel));
            _statusPanel = statusPanel ?? throw new ArgumentNullException(nameof(statusPanel));
            _craftingProcessor = craftingProcessor ?? throw new ArgumentNullException(nameof(craftingProcessor));

            _roomPanel.SelectedRoomChanged += OnSelectedRoomChanged;
            _compositionPanel.CraftQueueRequested += OnCraftQueueRequested;
        }

        private void OnSelectedRoomChanged(MemoryRoomId roomId, MemoryRoomPublicInfo info) =>
            _compositionPanel.SetTargetRoom(roomId, info.RequiredSupportingIntensityTotal);

        private void OnCraftQueueRequested()
        {
            var queue = _compositionPanel.Queue;
            if (queue.Count == 0)
                return;

            var result = _craftingProcessor.Craft(queue);
            if (result.Succeeded)
            {
                _compositionPanel.ClearQueue();
                _compositionPanel.ClearCraftResultMessage();
            }
            else
            {
                _compositionPanel.ShowCraftFailure(result.FailureReason.Value);
            }
        }

        public void Dispose()
        {
            _roomPanel.SelectedRoomChanged -= OnSelectedRoomChanged;
            _compositionPanel.CraftQueueRequested -= OnCraftQueueRequested;

            _roomPanel.Dispose();
            _compositionPanel.Dispose();
            _statusPanel.Dispose();
        }
    }
}
