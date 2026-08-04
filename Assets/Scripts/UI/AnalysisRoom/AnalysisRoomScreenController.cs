using System;
using GameName.UI.MemoryRoom;

namespace GameName.UI.AnalysisRoom
{
    // 세 패널 컨트롤러를 조립하고, 패널을 넘나드는 정보만 중개한다: 분석이
    // 성공하면(그 단서가 보관대에 있을 수 있으므로) 보관대 패널의 깊이 배지를
    // 새로 그리게 하고, 보관대↔인벤토리 전송이 일어나면(분석 패널의 단서
    // 목록 자체가 바뀌므로) 분석 패널을 새로 그리게 한다.
    //
    // 이동 처리·단서 분석·보관대 전송 중 어느 것도 여기서 계산하지 않는다 —
    // 전부 각 패널 컨트롤러가 이미 Core로 얻은 결론을 그대로 전달만 한다.
    public sealed class AnalysisRoomScreenController : IDisposable
    {
        private readonly MemoryRoomMapNavigationController _navigationPanel;
        private readonly AnalysisPanelController _analysisPanel;
        private readonly ClueStoragePanelController _storagePanel;

        public AnalysisRoomScreenController(
            MemoryRoomMapNavigationController navigationPanel,
            AnalysisPanelController analysisPanel,
            ClueStoragePanelController storagePanel)
        {
            _navigationPanel = navigationPanel ?? throw new ArgumentNullException(nameof(navigationPanel));
            _analysisPanel = analysisPanel ?? throw new ArgumentNullException(nameof(analysisPanel));
            _storagePanel = storagePanel ?? throw new ArgumentNullException(nameof(storagePanel));

            _analysisPanel.ClueAnalyzed += OnClueAnalyzed;
            _storagePanel.ClueTransferred += OnClueTransferred;
        }

        private void OnClueAnalyzed() => _storagePanel.Refresh();

        private void OnClueTransferred() => _analysisPanel.Refresh();

        public void Dispose()
        {
            _analysisPanel.ClueAnalyzed -= OnClueAnalyzed;
            _storagePanel.ClueTransferred -= OnClueTransferred;

            _navigationPanel.Dispose();
            _analysisPanel.Dispose();
            _storagePanel.Dispose();
        }
    }
}
