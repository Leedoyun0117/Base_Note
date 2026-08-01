using System;
using System.Collections.Generic;

namespace GameName.Core.Emotions
{
    // 보조 감정 2~4종과 각 세기의 배분을 표현하는 불변 값 타입.
    //
    // 이 타입 자체는 "감정 -> 세기" 매핑이 구조적으로 유효한지(같은 감정 중복 없음,
    // 세기는 양수)만 보장한다. 개수 범위(2~4)나 총량 상한/일치 여부, 바탕 감정과의
    // 중복 허용 여부는 아직 확정되지 않은 정책이므로 이 타입에 넣지 않고
    // IEmotionCompositionPolicy / IScentCompositionValidator 쪽으로 넘긴다.
    public sealed class EmotionBlend : IEquatable<EmotionBlend>
    {
        private readonly Dictionary<EmotionType, int> _intensityByEmotion;
        private readonly EmotionBlendEntry[] _entries;
        private readonly int _total;

        public EmotionBlend(IReadOnlyList<EmotionBlendEntry> entries)
        {
            if (entries == null)
                throw new ArgumentNullException(nameof(entries));

            _entries = new EmotionBlendEntry[entries.Count];
            _intensityByEmotion = new Dictionary<EmotionType, int>(entries.Count);

            var total = 0;
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (_intensityByEmotion.ContainsKey(entry.Emotion))
                {
                    throw new ArgumentException(
                        $"같은 감정({entry.Emotion})이 배분에 중복으로 존재한다.", nameof(entries));
                }

                _intensityByEmotion.Add(entry.Emotion, entry.Intensity);
                _entries[i] = entry;
                total += entry.Intensity;
            }

            // 판정 로직에서 반복 조회될 값이라 매번 순회하지 않도록 생성 시점에
            // 한 번만 계산해 둔다(불변 타입이므로 이후 값이 바뀔 일이 없다).
            _total = total;
        }

        public IReadOnlyList<EmotionBlendEntry> Entries => _entries;
        public IReadOnlyCollection<EmotionType> Emotions => _intensityByEmotion.Keys;
        public int Count => _entries.Length;
        public int Total => _total;

        public bool Contains(EmotionType emotion) => _intensityByEmotion.ContainsKey(emotion);

        public int IntensityOf(EmotionType emotion) =>
            _intensityByEmotion.TryGetValue(emotion, out var intensity) ? intensity : 0;

        public bool Equals(EmotionBlend other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (_intensityByEmotion.Count != other._intensityByEmotion.Count) return false;

            foreach (var pair in _intensityByEmotion)
            {
                if (!other._intensityByEmotion.TryGetValue(pair.Key, out var otherIntensity))
                    return false;
                if (otherIntensity != pair.Value)
                    return false;
            }

            return true;
        }

        public override bool Equals(object obj) => Equals(obj as EmotionBlend);

        public override int GetHashCode()
        {
            // 순서에 무관한 동등 비교를 지원하기 위해 각 항목의 해시를 XOR로 합산한다.
            var hash = 0;
            foreach (var pair in _intensityByEmotion)
                hash ^= (pair.Key.GetHashCode() * 397) ^ pair.Value.GetHashCode();
            return hash;
        }

        public static bool operator ==(EmotionBlend left, EmotionBlend right) =>
            left is null ? right is null : left.Equals(right);

        public static bool operator !=(EmotionBlend left, EmotionBlend right) => !(left == right);
    }
}
