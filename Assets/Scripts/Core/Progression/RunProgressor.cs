using System;
using System.Collections.Generic;
using GameName.Core.Authoring;
using GameName.Core.Events;

namespace GameName.Core.Progression
{
    // 방들을 한 줄로 이어 순차 진행시키는 코디네이터.
    //
    // 얇게 유지한다. 방이 닫혔다는 사실(RoomClearedEvent / RoomFailedEvent)을 듣고
    // 다음 방의 RoomStartedEvent를 내보내는 것이 전부다. 방마다 리셋되어야 하는
    // 것들(신뢰 게이지, 단서 단계, 대화 진행)은 이 코디네이터가 붙잡고 흔드는
    // 대신 각자 RoomStartedEvent를 구독해 스스로 초기화한다 — 그래야 리셋 대상이
    // 늘어도 이 타입이 God Class로 자라지 않는다.
    //
    // 종료 사유는 보지 않는다. 클리어든 실패든 똑같이 다음 방으로 넘어가고,
    // 실패한 방에서 얻지 못한 태그·검열 해금은 그대로 없는 채로 이어진다.
    // 추출된 기억, 히로민, 기회, 검열 해금 로그는 이 타입이 건드리지 않으므로
    // (재생성도 안 한다) 런 전체에 걸쳐 살아남는다.
    //
    // Advance()가 public인 이유: 대화 종료·신뢰 0(RoomClearedEvent/RoomFailedEvent)
    // 뿐 아니라 MemoryMoveProcessor의 "다음 기억으로 이동"도 같은 전환을
    // 일으킨다 — 플레이어가 히로민을 써서 대화를 끝까지 보지 않고도 떠날 수
    // 있어야 하기 때문이다. 두 경로가 같은 메서드를 부르므로 "다음 방으로
    // 넘어간다"는 동작 자체는 어디서 왔든 하나로 남는다.
    public sealed class RunProgressor
    {
        private readonly IReadOnlyList<RoomDefinition> _rooms;
        private readonly IEventBus _eventBus;

        private int _index = -1;
        private bool _ended;

        public RunProgressor(IReadOnlyList<RoomDefinition> rooms, IEventBus eventBus)
        {
            _rooms = rooms ?? throw new ArgumentNullException(nameof(rooms));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));

            _eventBus.Subscribe<RoomClearedEvent>(_ => Advance());
            _eventBus.Subscribe<RoomFailedEvent>(_ => Advance());
        }

        // 첫 방으로 진입한다. 구성 루트가 조립을 끝낸 뒤 한 번 호출한다.
        public void Start()
        {
            if (_index >= 0)
                throw new InvalidOperationException("런은 한 번만 시작할 수 있다.");

            _index = 0;
            if (_rooms.Count == 0)
            {
                _ended = true;
                _eventBus.Publish(new RunCompletedEvent());
                return;
            }

            _eventBus.Publish(new RoomStartedEvent(_rooms[0].Id, 0));
        }

        // 다음 방으로 넘어간다. 이미 런이 끝난 뒤라면 조용히 무시한다 — 기회
        // 소진으로 런이 먼저 끝난 채로 이동이 다시 시도되는 경우를 안전하게
        // 흡수하기 위해서다.
        public void Advance()
        {
            if (_ended)
                return;

            _index++;
            if (_index >= _rooms.Count)
            {
                _ended = true;
                _eventBus.Publish(new RunCompletedEvent());
                return;
            }

            _eventBus.Publish(new RoomStartedEvent(_rooms[_index].Id, _index));
        }
    }
}
