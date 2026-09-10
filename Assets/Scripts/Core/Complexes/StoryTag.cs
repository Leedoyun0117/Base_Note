using System;

namespace GameName.Core.Complexes
{
    // 단서(또는 컴플렉스 체인의 중간 결과)에 붙는 서사 태그 하나.
    //
    // 옛 ClueTag와 다른 점: 동치 비교가 축까지 본다. 옛 모델에서는 같은 말이
    // 한 단서에선 중심축, 다른 단서에선 곁축일 수 있어 Value로만 비교했지만,
    // 3축 모델에서는 "Yuki"는 언제나 Person 축이고 "그리움"은 언제나 Emotion
    // 축이다 — 축이 곧 정체성의 일부다.
    //
    // 문자열을 타입으로 감싸는 이유는 ClueId와 같다: 오타 하나가 컴플렉스
    // 규칙과 영영 맞물리지 않는 태그를 만들 수 있어, 어디서나 같은 비교 규칙
    // (정확히 일치)이 강제되어야 한다.
    public readonly struct StoryTag : IEquatable<StoryTag>
    {
        public StoryTagAxis Axis { get; }
        public string Value { get; }

        public StoryTag(StoryTagAxis axis, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("서사 태그 값은 비어 있을 수 없다.", nameof(value));

            Axis = axis;
            Value = value;
        }

        // 저작·규칙 호출부에서 축이 눈에 띄게 — new StoryTag(StoryTagAxis.Emotion, x)
        // 보다 StoryTag.Emotion(x)가 규칙 목록을 훑을 때 읽힌다.
        public static StoryTag Person(string value) => new StoryTag(StoryTagAxis.Person, value);
        public static StoryTag Emotion(string value) => new StoryTag(StoryTagAxis.Emotion, value);
        public static StoryTag Time(string value) => new StoryTag(StoryTagAxis.Time, value);

        public bool Equals(StoryTag other) => Axis == other.Axis && Value == other.Value;
        public override bool Equals(object obj) => obj is StoryTag other && Equals(other);
        public override int GetHashCode() => unchecked(((int)Axis * 397) ^ (Value?.GetHashCode() ?? 0));
        public override string ToString() => $"{Axis}:{Value}";

        public static bool operator ==(StoryTag left, StoryTag right) => left.Equals(right);
        public static bool operator !=(StoryTag left, StoryTag right) => !left.Equals(right);
    }
}
