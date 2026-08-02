using GameName.Core.Analysis;
using UnityEngine.UIElements;

namespace GameName.UI.Shared
{
    // 감정 하나를 색 스와치 + 세기(또는 "미공개") 칩으로 그리는 순수 표시용
    // 유틸리티. 기록지(분석 기록)와 분석실(분석 결과)이 똑같은 형식을 쓴다 —
    // 여기 하나로 모아 두 화면이 각자 다시 만들지 않게 한다.
    //
    // "emotion-chip", "emotion-chip__swatch", "emotion-chip__value",
    // "emotion-chip__value--hidden" 클래스는 각 화면의 USS에 동일한 값으로
    // 선언되어 있어야 한다 — UI Toolkit은 UIDocument마다 스타일시트가
    // 독립적이라 클래스 "이름"만 공유되고 선언 자체는 각자 필요하다.
    internal static class EmotionChipFactory
    {
        public static VisualElement Create(DetectedEmotion detected)
        {
            var chip = new VisualElement();
            chip.AddToClassList("emotion-chip");

            var swatch = new VisualElement();
            swatch.AddToClassList("emotion-chip__swatch");
            swatch.AddToClassList(EmotionDisplay.ColorClass(detected.Emotion));
            chip.Add(swatch);

            var valueLabel = new Label(detected.Intensity.HasValue ? detected.Intensity.Value.ToString() : "미공개");
            valueLabel.AddToClassList(detected.Intensity.HasValue ? "emotion-chip__value" : "emotion-chip__value--hidden");
            chip.Add(valueLabel);

            return chip;
        }
    }
}
