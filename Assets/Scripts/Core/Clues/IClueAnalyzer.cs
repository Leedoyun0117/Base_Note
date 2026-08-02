using GameName.Core.Analysis;

namespace GameName.Core.Clues
{
    // 단서 분석 경계. ClueId만 받는다 — 호출부(UI 등)가 ClueDefinition(진실
    // 구성 포함)을 손에 쥘 필요가 없어야 하기 때문이다. 정의 조회는 구현체가
    // IMemoryRoomClueTracker를 통해 내부에서 수행한다.
    public interface IClueAnalyzer
    {
        ClueAnalysisResult Analyze(ClueId clueId, AnalysisDepth depth);
    }
}
