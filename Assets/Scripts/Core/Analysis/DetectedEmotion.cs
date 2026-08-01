using GameName.Core.Emotions;

namespace GameName.Core.Analysis
{
    // 분석으로 드러난 감정 1건. 일반 분석 결과라면 Intensity가 항상 비어 있다.
    public readonly struct DetectedEmotion
    {
        public EmotionType Emotion { get; }
        public int? Intensity { get; }

        public DetectedEmotion(EmotionType emotion, int? intensity)
        {
            Emotion = emotion;
            Intensity = intensity;
        }
    }
}
