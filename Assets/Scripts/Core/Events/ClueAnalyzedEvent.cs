using GameName.Core.Analysis;
using GameName.Core.Clues;

namespace GameName.Core.Events
{
    // 단서 분석(일반/고급)이 끝났을 때 발행된다.
    public readonly struct ClueAnalyzedEvent
    {
        public ClueId ClueId { get; }
        public EmotionAnalysisResult Result { get; }

        public ClueAnalyzedEvent(ClueId clueId, EmotionAnalysisResult result)
        {
            ClueId = clueId;
            Result = result;
        }
    }
}
