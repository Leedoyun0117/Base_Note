using System;
using System.Collections.Generic;
using GameName.Core.Memories;

namespace GameName.Core.Dialogue
{
    // 파싱을 마친 대사 한 줄. 조각 목록 그 자체이며, 무엇이 풀렸는지는 모른다.
    //
    // 조각 배열(IReadOnlyList)을 그대로 돌리지 않고 타입을 씌운 이유는 두 가지다.
    // 하나는 "이 목록은 한 줄 전체다"라는 사실을 타입으로 못박기 위해서고, 다른
    // 하나는 RequiredColors 때문이다 — 이 줄을 완전히 읽으려면 어떤 색이
    // 필요한가는 검증기와 힌트 표시가 모두 묻게 되는 질문인데, 그 계산을 묻는
    // 쪽마다 다시 하게 두면 같은 순회가 곳곳에 흩어진다.
    public sealed class CensoredText
    {
        public static readonly CensoredText Empty =
            new CensoredText(Array.Empty<CensoredTextSegment>());

        public IReadOnlyList<CensoredTextSegment> Segments { get; }

        // 이 줄에 걸린 검열을 전부 풀기 위해 필요한 색. 중복은 접는다.
        public IReadOnlyList<MemoryColor> RequiredColors { get; }

        public CensoredText(IReadOnlyList<CensoredTextSegment> segments)
        {
            Segments = segments ?? throw new ArgumentNullException(nameof(segments));

            var colors = new List<MemoryColor>();
            foreach (var segment in segments)
            {
                if (segment.Color.HasValue && !colors.Contains(segment.Color.Value))
                    colors.Add(segment.Color.Value);
            }

            RequiredColors = colors;
        }
    }
}
