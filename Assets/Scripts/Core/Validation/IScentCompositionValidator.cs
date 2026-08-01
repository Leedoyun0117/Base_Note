using GameName.Core.Emotions;

namespace GameName.Core.Validation
{
    // 배합 유효성 검증 경계.
    // 이 동작은 조향실 UI가 호출하므로, UI가 알아도 되는 정보만 시그니처에 노출한다.
    // - 세기 총량이 상한이 아니라 정확히 일치해야 한다는 규칙은 정답 전체
    //   (MemoryRoomAnswer) 대신 "요구 총량" 정수 하나만 받아 검증한다. 정답 향의
    //   실제 감정 구성까지 UI 레이어에 넘길 이유가 없기 때문이다.
    // - 개수/중복 허용 같은 정책(IEmotionCompositionPolicy)은 인자로 받지 않는다.
    //   호출부는 정책의 존재 자체를 몰라도 되며, 검증기 구현체가 생성자로
    //   주입받아 내부적으로 사용한다.
    public interface IScentCompositionValidator
    {
        CompositionValidationResult Validate(Scent scent, int requiredSupportingIntensityTotal);
    }
}
