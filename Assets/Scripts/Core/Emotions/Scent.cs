using System;

namespace GameName.Core.Emotions
{
    // 향 = 바탕 감정 1종 + 보조 감정 배분.
    // 바탕 감정은 배분(EmotionBlend)에 포함되지 않는 별도 슬롯이다.
    //
    // struct가 아니라 class로 만든 이유: struct는 매개변수 없는 생성자를 막을 수
    // 없어 배열/리스트 할당(new Scent[n], 초기화 누락 등)만으로 생성자의 null 검사를
    // 우회한 default(Scent)가 만들어진다. 이 default 값은 BaseEmotion이 그럴듯하게
    // 첫 번째 열거값으로 채워져 있어 겉보기엔 멀쩡하지만 SupportingBlend는 null이라,
    // 실제 사용 시점에야 뒤늦게 NullReferenceException이 터진다(앰플을 배열/리스트로
    // 관리할 때 실제로 발생). class로 두면 초기화되지 않은 슬롯은 처음부터 명확히
    // null이므로 실수를 즉시 드러낸다. EmotionBlend를 class로 둔 이유와 동일하다.
    public sealed class Scent : IEquatable<Scent>
    {
        public EmotionType BaseEmotion { get; }
        public EmotionBlend SupportingBlend { get; }

        public Scent(EmotionType baseEmotion, EmotionBlend supportingBlend)
        {
            BaseEmotion = baseEmotion;
            SupportingBlend = supportingBlend ?? throw new ArgumentNullException(nameof(supportingBlend));
        }

        public bool Equals(Scent other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return BaseEmotion == other.BaseEmotion && SupportingBlend.Equals(other.SupportingBlend);
        }

        public override bool Equals(object obj) => Equals(obj as Scent);

        public override int GetHashCode() =>
            (BaseEmotion.GetHashCode() * 397) ^ SupportingBlend.GetHashCode();

        public static bool operator ==(Scent left, Scent right) =>
            left is null ? right is null : left.Equals(right);

        public static bool operator !=(Scent left, Scent right) => !(left == right);
    }
}
