namespace GameName.Core.Validation
{
    // 보조 감정 구성에 대한 정책 수치.
    // 개수 범위나 바탕 감정과의 중복 허용 여부가 아직 미정이므로 매직 넘버 없이
    // 전부 외부 설정으로 주입받는다.
    public interface IEmotionCompositionPolicy
    {
        int MinSupportingEmotionCount { get; }
        int MaxSupportingEmotionCount { get; }
        bool AllowSupportingEmotionSameAsBase { get; }
    }
}
