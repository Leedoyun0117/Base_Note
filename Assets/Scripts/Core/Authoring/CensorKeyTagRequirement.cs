using System;
using System.Collections.Generic;
using GameName.Core.Clues;
using GameName.Core.Dialogue;

namespace GameName.Core.Authoring
{
    // 검열 키 하나를 풀려면 제시할 기억이 어느 태그를 가져야 하는지 적은
    // 저작 데이터.
    //
    // 대사 원문의 토큰([[색:키:말]])이 저절로 주는 키→색 대응과 달리, 이
    // 대응은 토큰에서 뽑아낼 수 없어 따로 저작한다 — 색은 "그 기억이 어떤
    // 색으로 드러나는가"라는 추출 결과일 뿐이고, 무엇을 물어야 풀리는가는
    // 색과 무관한 별개의 사실이기 때문이다(색이 같아도 태그가 다르면 안
    // 풀려야 한다는 규칙이 여기서 나온다).
    //
    // 런 전체에 걸쳐 하나의 목록으로 두는 이유는 색 대응과 같다: 같은 키가
    // 여러 방의 대사에 걸쳐 쓰일 수 있어 방 하나에 매인 데이터가 아니다.
    public readonly struct CensorKeyTagRequirement
    {
        public CensorKey Key { get; }
        public IReadOnlyList<ClueTag> RequiredTags { get; }

        public CensorKeyTagRequirement(CensorKey key, IReadOnlyList<ClueTag> requiredTags)
        {
            Key = key;
            RequiredTags = requiredTags ?? throw new ArgumentNullException(nameof(requiredTags));
        }
    }
}
