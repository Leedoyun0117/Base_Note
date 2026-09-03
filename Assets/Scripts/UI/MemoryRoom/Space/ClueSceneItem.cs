using GameName.Core.Clues;
using UnityEngine;

namespace GameName.UI.MemoryRoom.Space
{
    // 방 안에 단서 하나를 그리기 위해 컨트롤러가 표시 쪽에 넘기는 지시.
    //
    // ClueInfo를 그대로 넘기지 않는다 — 표시 쪽이 겉보기 구성 같은 게임 정보에
    // 손을 댈 이유가 없고, 실제로 필요한 것은 "무엇을(식별자) 어떤 모양으로
    // 어디에" 뿐이기 때문이다. 이 지시를 받아 만들어진 씬 오브젝트는 그중에서도
    // 식별자 하나만 기억한다.
    public readonly struct ClueSceneItem
    {
        public ClueId ClueId { get; }
        public ClueKind Kind { get; }
        public Vector2 Position { get; }

        // 지금 가시 비율 안에 있어 집을 수 있는가. false면 회색 처리 + 콜라이더
        // 비활성으로 그린다 — 판정 자체는 컨트롤러가 Core 정책으로 이미 했다.
        public bool Accessible { get; }

        public ClueSceneItem(ClueId clueId, ClueKind kind, Vector2 position, bool accessible)
        {
            ClueId = clueId;
            Kind = kind;
            Position = position;
            Accessible = accessible;
        }
    }
}
