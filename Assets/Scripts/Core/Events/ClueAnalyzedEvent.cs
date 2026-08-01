using GameName.Core.Analysis;

namespace GameName.Core.Events
{
    // 단서 분석(일반/고급)이 끝났을 때 발행된다.
    public readonly struct ClueAnalyzedEvent
    {
        public string ClueId { get; }
        public EmotionAnalysisResult Result { get; }

        public ClueAnalyzedEvent(string clueId, EmotionAnalysisResult result)
        {
            ClueId = clueId;
            Result = result;
        }
    }
}
