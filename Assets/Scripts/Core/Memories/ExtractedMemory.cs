using System;
using System.Collections.Generic;
using GameName.Core.Clues;

namespace GameName.Core.Memories
{
    // 단서 하나를 추출해서 얻은 기억 한 조각.
    //
    // 색은 얻는 순간 드러나는 힌트일 뿐이라 여기 그대로 남지만, 검열 해금이나
    // ClueSelection 같은 정답 판정이 실제로 보는 값은 Tags다 — 추출한 시점의
    // 출처 단서(ClueDefinition)가 갖고 있던 태그를 그대로 옮겨 담는다. 옮겨
    // 담는 이유는 추출 이후 저작 데이터가 이 기억과 독립적으로 살아야 하기
    // 때문이다(단서 정의를 매번 다시 찾아가지 않는다).
    public sealed class ExtractedMemory
    {
        public ClueId SourceClueId { get; }
        public MemoryColor Color { get; }
        public IReadOnlyList<ClueTag> Tags { get; }

        public ExtractedMemory(ClueId sourceClueId, MemoryColor color, IReadOnlyList<ClueTag> tags)
        {
            SourceClueId = sourceClueId;
            Color = color;
            Tags = tags ?? throw new ArgumentNullException(nameof(tags));
        }
    }
}
