using System;
using GameName.Core.Ampoules;
using GameName.Core.MemoryRooms;
using GameName.UI.MemoryRoom;

namespace GameName.UI.Perfumery
{
    // 네 패널 컨트롤러를 조립하고, 패널을 넘나드는 정보만 중개한다:
    // 선택된 방(좌측 목록에서 선택) -> 목표 방/요구 총량(중앙). 우측(상태)과
    // 이동 패널은 더 이상 다른 패널의 상태에 의존하지 않는다 — 정신력/보관함/
    // 인벤토리/위치는 전부 Core 이벤트로 스스로 갱신한다.
    //
    // 이동(지도, 계단·분석실로 나가기)과 조향 목표 방 선택(목록)은 서로 다른
    // 동작이라 별도 컨트롤러로 분리해 둔다 — 지도는 기억 방/분석실 화면과
    // 같은 MemoryRoomMapNavigationController를 그대로 재사용하고, 목록은
    // 최종 조향 화면과 같은 RoomSelectionPanelController를 그대로 재사용한다.
    //
    // 이 클래스도 게임 규칙을 계산하지 않는다 — 언제나 각 패널이나 Core
    // 처리기가 이미 낸 결론을 그대로 전달만 한다. AmpouleCraftingProcessor를
    // 실제로 호출하는 유일한 지점이며, 그 결과(성공/실패 사유)를 조향
    // 패널(대기열이 있는 곳)에 그대로 넘긴다.
    public sealed class PerfumeryScreenController : IDisposable
    {
        private readonly MemoryRoomMapNavigationController _navigationPanel;
        private readonly RoomSelectionPanelController _roomPanel;
        private readonly PerfumeryCompositionPanelController _compositionPanel;
        private readonly StatusPanelController _statusPanel;
        private readonly AmpouleCraftingProcessor _craftingProcessor;

        public PerfumeryScreenController(
            MemoryRoomMapNavigationController navigationPanel,
            RoomSelectionPanelController roomPanel,
            PerfumeryCompositionPanelController compositionPanel,
            StatusPanelController statusPanel,
            AmpouleCraftingProcessor craftingProcessor)
        {
            _navigationPanel = navigationPanel ?? throw new ArgumentNullException(nameof(navigationPanel));
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

            _navigationPanel.Dispose();
            _roomPanel.Dispose();
            _compositionPanel.Dispose();
            _statusPanel.Dispose();
        }
    }
}
