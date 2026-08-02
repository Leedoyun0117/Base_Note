using System;
using GameName.Core.Analysis;
using GameName.Core.Clues;

namespace GameName.Core.Journal
{
    // 단서 분석 결과 하나의 불변 기록. EmotionAnalysisResult는 이미 겉보기
    // 구성만 담는 안전한 타입이므로(진실/정답 타입 아님) 그대로 보관한다.
    public sealed class AnalysisRecord
    {
        public ClueId ClueId { get; }
        public EmotionAnalysisResult Result { get; }

        public AnalysisRecord(ClueId clueId, EmotionAnalysisResult result)
        {
            ClueId = clueId;
            Result = result ?? throw new ArgumentNullException(nameof(result));
        }
    }
}
