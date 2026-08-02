using GameName.Core.Emotions;

namespace GameName.UI.Shared
{
    // 감정 타입을 화면에 표시하기 위한 순수 매핑(색 클래스, 한글 라벨)만
    // 제공한다. 조향실/기록지 등 여러 화면이 같은 표시 형식을 쓰기 위한
    // 유틸리티이며, 게임 규칙과는 무관하다.
    internal static class EmotionDisplay
    {
        public static string ColorClass(EmotionType emotion) => "emotion--" + emotion.ToString().ToLowerInvariant();

        public static string Label(EmotionType emotion)
        {
            switch (emotion)
            {
                case EmotionType.Joy: return "기쁨";
                case EmotionType.Love: return "사랑";
                case EmotionType.Anger: return "분노";
                case EmotionType.Sadness: return "슬픔";
                case EmotionType.Fear: return "두려움";
                default: return emotion.ToString();
            }
        }
    }
}
