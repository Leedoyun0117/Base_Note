using System;

namespace GameName.Core.Emotions
{
    // 보조 감정 하나와 그 세기를 묶은 최소 단위.
    public readonly struct EmotionBlendEntry
    {
        public EmotionType Emotion { get; }
        public int Intensity { get; }

        public EmotionBlendEntry(EmotionType emotion, int intensity)
        {
            if (intensity <= 0)
                throw new ArgumentOutOfRangeException(nameof(intensity), "세기는 1 이상이어야 한다.");

            Emotion = emotion;
            Intensity = intensity;
        }
    }
}
