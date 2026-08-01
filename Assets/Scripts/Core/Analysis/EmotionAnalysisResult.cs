using System;
using System.Collections.Generic;

namespace GameName.Core.Analysis
{
    // 분석 결과. 일반/고급 분석이 같은 타입으로 표현되며, Depth로 세기 공개 여부를
    // 구분한다(Basic이면 모든 DetectedEmotion.Intensity가 null이어야 한다).
    public sealed class EmotionAnalysisResult
    {
        public AnalysisDepth Depth { get; }
        public IReadOnlyList<DetectedEmotion> DetectedEmotions { get; }

        public EmotionAnalysisResult(AnalysisDepth depth, IReadOnlyList<DetectedEmotion> detectedEmotions)
        {
            if (detectedEmotions == null)
                throw new ArgumentNullException(nameof(detectedEmotions));

            if (depth == AnalysisDepth.Basic)
            {
                foreach (var detected in detectedEmotions)
                {
                    if (detected.Intensity.HasValue)
                        throw new ArgumentException("일반 분석 결과는 세기를 포함할 수 없다.", nameof(detectedEmotions));
                }
            }

            Depth = depth;
            DetectedEmotions = new List<DetectedEmotion>(detectedEmotions);
        }
    }
}
