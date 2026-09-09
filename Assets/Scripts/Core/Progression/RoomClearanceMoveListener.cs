using System;
using GameName.Core.Events;
using GameName.Core.Hiromi;

namespace GameName.Core.Progression
{
    // 방을 다 보고 "다음으로"를 누르면(RoomClearedEvent) 다음 기억으로 넘어가는
    // 것을 "다음 기억으로 이동"과 같은 경로로 처리한다 — 넘어가는 것 자체가
    // 히로민을 쓰는 행동이다. 가진 히로민이 이동 비용 이상이면 그만큼 쓰고
    // 넘어가고, 모자라면 가진 만큼 다 쓰고 기회 하나를 대신 치른다(기회가 0이면
    // 그 자리에서 런이 끝난다). 그 판단은 전부 MemoryMoveProcessor.Move()에 있다.
    //
    // 신뢰 0 실패(RoomFailedEvent)는 이 경로를 타지 않는다 — 쫓겨나는 것이라
    // 대가가 없고, RunProgressor가 그대로 받아 런을 끝낸다. 그래서 "방을 다 봤다"는
    // 사실(화면의 버튼)과 "그래서 어떻게 넘어가는가"를 분리해 둔다.
    //
    // RunProgressor가 RoomClearedEvent를 직접 받지 않는 이유가 이것이다.
    public sealed class RoomClearanceMoveListener
    {
        public RoomClearanceMoveListener(MemoryMoveProcessor move, IEventBus eventBus)
        {
            if (move == null) throw new ArgumentNullException(nameof(move));
            if (eventBus == null) throw new ArgumentNullException(nameof(eventBus));

            eventBus.Subscribe<RoomClearedEvent>(_ => move.Move());
        }
    }
}
