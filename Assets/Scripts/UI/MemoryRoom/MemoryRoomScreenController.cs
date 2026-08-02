using System;

namespace GameName.UI.MemoryRoom
{
    // 네 패널 컨트롤러를 조립하고, 패널을 넘나드는 정보만 중개한다: 단서
    // 습득(단서 패널) 또는 시향 소모(시향 패널)로 인벤토리 내용이 바뀌면
    // 인벤토리 패널에 새로고침을 지시한다. 그 외에는 각 패널이 자기 몫의
    // Core 이벤트를 직접 구독해 스스로 갱신하므로 이 타입이 할 일이 없다.
    //
    // 이동 처리·단서 판정·시향 판정 중 어느 것도 여기서 계산하지 않는다 —
    // 전부 각 패널 컨트롤러가 이미 Core로 얻은 결론을 그대로 전달만 한다.
    public sealed class MemoryRoomScreenController : IDisposable
    {
        private readonly RoomNavigationPanelController _navigationPanel;
        private readonly ClueCollectionPanelController _cluePanel;
        private readonly MemoryRoomInventoryPanelController _inventoryPanel;
        private readonly ScentTestingPanelController _testingPanel;

        public MemoryRoomScreenController(
            RoomNavigationPanelController navigationPanel,
            ClueCollectionPanelController cluePanel,
            MemoryRoomInventoryPanelController inventoryPanel,
            ScentTestingPanelController testingPanel)
        {
            _navigationPanel = navigationPanel ?? throw new ArgumentNullException(nameof(navigationPanel));
            _cluePanel = cluePanel ?? throw new ArgumentNullException(nameof(cluePanel));
            _inventoryPanel = inventoryPanel ?? throw new ArgumentNullException(nameof(inventoryPanel));
            _testingPanel = testingPanel ?? throw new ArgumentNullException(nameof(testingPanel));

            _cluePanel.ClueCollected += OnInventoryChanged;
            _testingPanel.AmpouleTested += OnInventoryChanged;
        }

        private void OnInventoryChanged() => _inventoryPanel.Refresh();

        public void Dispose()
        {
            _cluePanel.ClueCollected -= OnInventoryChanged;
            _testingPanel.AmpouleTested -= OnInventoryChanged;

            _navigationPanel.Dispose();
            _cluePanel.Dispose();
            _testingPanel.Dispose();
        }
    }
}
