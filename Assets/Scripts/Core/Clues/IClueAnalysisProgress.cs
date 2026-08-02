using GameName.Core.Analysis;

namespace GameName.Core.Clues
{
    // 단서별 분석 진행도(지금까지 도달한 가장 깊은 분석 깊이)를 보유하는 경계.
    // ClueAnalyzer는 이 인터페이스에 묻고 갱신을 요청할 뿐, 진행도를 직접
    // 들고 있지 않는다 — 의뢰가 바뀌면 Reset()으로 초기화할 책임이 이 타입에
    // 있어야 하기 때문이다.
    public interface IClueAnalysisProgress
    {
        bool TryGetBestDepth(ClueId clueId, out AnalysisDepth depth);
        void RecordDepth(ClueId clueId, AnalysisDepth depth);

        // 의뢰가 바뀔 때 호출한다. 이전 의뢰에서의 분석 진행도가 새 의뢰로
        // 넘어가면 안 되기 때문이다.
        void Reset();
    }
}
