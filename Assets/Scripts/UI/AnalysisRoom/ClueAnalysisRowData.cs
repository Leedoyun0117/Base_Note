using GameName.Core.Analysis;
using GameName.Core.Clues;

namespace GameName.UI.AnalysisRoom
{
    // 분석 패널이 단서 한 줄을 그리는 데 필요한 정보만 모은 UI 전용 DTO.
    // BasicEnabled/AdvancedEnabled는 화면이 미리 보여주는 값일 뿐이며, 실제
    // 가부 판단은 여전히 ClueAnalyzer.Analyze가 한다.
    public readonly struct ClueAnalysisRowData
    {
        public ClueInfo Clue { get; }
        public AnalysisDepth? AnalyzedDepth { get; }
        public bool IsSelected { get; }
        public bool BasicEnabled { get; }
        public bool AdvancedEnabled { get; }

        public ClueAnalysisRowData(
            ClueInfo clue, AnalysisDepth? analyzedDepth, bool isSelected, bool basicEnabled, bool advancedEnabled)
        {
            Clue = clue;
            AnalyzedDepth = analyzedDepth;
            IsSelected = isSelected;
            BasicEnabled = basicEnabled;
            AdvancedEnabled = advancedEnabled;
        }
    }
}
