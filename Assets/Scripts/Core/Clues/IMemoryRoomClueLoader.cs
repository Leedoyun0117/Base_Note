using System.Collections.Generic;

namespace GameName.Core.Clues
{
    // 방별 단서 정의와 최초 배치 전체를 다른 구성으로 통째로 갈아 끼우는
    // 권한 하나만 표현하는 좁은 경계. 새 정의 집합을 들여오는 것 자체가
    // "지금까지 무엇을 습득했는지"도 의미 없게 만들므로, 습득 기록과 배치
    // 초기화는 별도 Reset 호출 없이 이 Load 안에서 함께 처리된다.
    // IMemoryRoomClueTracker(정상 조회 인터페이스)에는 이 능력이 없다 — 오직
    // 구성 루트(GameSession)만 받는다.
    public interface IMemoryRoomClueLoader
    {
        void Load(IReadOnlyList<CluePlacement> placements);
    }
}
