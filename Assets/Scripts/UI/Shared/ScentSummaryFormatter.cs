using System.Collections.Generic;
using GameName.Core.Emotions;

namespace GameName.UI.Shared
{
    // 향을 사람이 읽을 수 있는 짧은 문자열로 요약한다. 조향실/기록지 등 여러
    // 화면이 같은 형식을 쓰기 위한 순수 표시용 유틸리티 — 게임 규칙과 무관하다.
    internal static class ScentSummaryFormatter
    {
        public static string Summarize(Scent scent) => Summarize(scent.SupportingBlend);

        // 단서의 겉보기 구성(EmotionBlend)처럼 Scent로 감싸여 있지 않은 배분도
        // 같은 형식으로 요약해야 하는 소비자를 위한 오버로드. Summarize(Scent)도
        // 이 메서드로 위임해, 조인 형식이 한 곳에만 있게 한다.
        public static string Summarize(EmotionBlend blend)
        {
            var parts = new List<string>();
            foreach (var entry in blend.Entries)
                parts.Add($"{entry.Emotion}:{entry.Intensity}");

            return string.Join(", ", parts);
        }
    }
}
