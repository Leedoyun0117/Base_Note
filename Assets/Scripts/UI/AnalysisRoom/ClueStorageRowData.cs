using GameName.Core.Analysis;
using GameName.Core.Clues;

namespace GameName.UI.AnalysisRoom
{
    // 보관대 패널이 단서 한 줄을 그리는 데 필요한 정보만 모은 UI 전용 DTO.
    // AnalyzedDepth를 함께 담아, "뭘 더 분석해야 하는지" 화면이 바로 보여줄
    // 수 있게 한다.
    public readonly struct ClueStorageRowData
    {
        public ClueInfo Clue { get; }
        public AnalysisDepth? AnalyzedDepth { get; }

        public ClueStorageRowData(ClueInfo clue, AnalysisDepth? analyzedDepth)
        {
            Clue = clue;
            AnalyzedDepth = analyzedDepth;
        }
    }
}
