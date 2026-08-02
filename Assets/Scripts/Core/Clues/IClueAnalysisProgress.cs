using GameName.Core.Analysis;

namespace GameName.Core.Clues
{
    // 단서별 분석 진행도(지금까지 도달한 가장 깊은 분석 깊이)를 보유하는 경계.
    // ClueAnalyzer는 이 인터페이스에 묻고 갱신을 요청할 뿐, 진행도를 직접
    // 들고 있지 않는다. 의뢰가 바뀔 때 이 진행도를 통째로 비우는 권한은
    // IResettable로만 노출된다.
    public interface IClueAnalysisProgress
    {
        bool TryGetBestDepth(ClueId clueId, out AnalysisDepth depth);
        void RecordDepth(ClueId clueId, AnalysisDepth depth);
    }
}
