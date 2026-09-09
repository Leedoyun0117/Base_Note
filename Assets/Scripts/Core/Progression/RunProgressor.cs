using System;
using System.Collections.Generic;
using GameName.Core.Authoring;
using GameName.Core.Events;

namespace GameName.Core.Progression
{
    // 방들을 한 줄로 이어 순차 진행시키는 코디네이터.
    //
    // 얇게 유지한다. 방이 닫혔다는 사실을 듣고 다음 방의 RoomStartedEvent(또는
    // 런의 끝이면 RunCompletedEvent)를 내보내는 것이 전부다. 방마다 리셋되어야 하는
    // 것들(신뢰 게이지, 단서 단계, 대화 진행)은 이 코디네이터가 붙잡고 흔드는
    // 대신 각자 RoomStartedEvent를 구독해 스스로 초기화한다 — 그래야 리셋 대상이
    // 늘어도 이 타입이 God Class로 자라지 않는다.
    //
    // 넘어가는 계기는 둘이다. "다음으로" 버튼(대화를 다 본 뒤)은 히로민을
    // 치르는 이동이라 RoomClearanceMoveListener → MemoryMoveProcessor.Move()를
    // 거쳐 Advance()로 오고, 신뢰 0 실패(RoomFailedEvent)는 대가 없이 쫓겨나는
    // 것이라 이 타입이 곧장 받아 EndRun()으로 런을 끝낸다 — 실패한 방을 지나
    // 다음 방으로 데려가지 않는다.
    // 추출된 기억, 히로민, 기회, 검열 해금 로그는 이 타입이 건드리지 않으므로
    // (재생성도 안 한다) 런이 이어지는 동안 살아남는다.
    //
    // Advance()가 public인 이유: "다음으로" 버튼도, 가방의 이동 레버도 결국
    // MemoryMoveProcessor.Move()를 거쳐 이 메서드를 부른다 — 어디서 왔든
    // "다음 방으로 넘어간다"는 동작은 하나로 남는다.
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

            // "다음으로" 버튼으로 넘어가는 것(RoomClearedEvent)은 여기서 직접
            // 받지 않는다 — 히로민을 치르는 이동이라 RoomClearanceMoveListener가
            // MemoryMoveProcessor를 거쳐 Advance()를 부른다. 신뢰 0 실패만 곧장 받는다.
            _eventBus.Subscribe<RoomFailedEvent>(_ => EndRun());
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

        // 신뢰 0으로 방이 무너지면 런은 여기서 끝난다 — 남은 방이 있어도
        // 넘어가지 않는다. 이미 끝난 뒤라면 조용히 무시한다.
        public void EndRun()
        {
            if (_ended)
                return;

            _ended = true;
            _eventBus.Publish(new RunCompletedEvent());
        }
    }
}
