using System;
using System.Collections.Generic;
using GameName.Core.Authoring;
using GameName.Core.Events;

namespace GameName.Core.Progression
{
    // 라운드들을 한 줄로 이어 순차 진행시키는 코디네이터.
    //
    // 얇게 유지한다. 라운드가 끝났다는 사실을 듣고 다음 라운드의 RoomStartedEvent
    // (또는 런의 끝이면 RunCompletedEvent)를 내보내는 것이 전부다. 라운드마다
    // 리셋되어야 하는 것들(단서 상태, 턴 카운터, 활성 컴플렉스)은 각자
    // RoomStartedEvent를 구독해 스스로 초기화한다 — 그래야 리셋 대상이 늘어도
    // 이 타입이 God Class로 자라지 않는다.
    //
    // 넘어가는 계기는 하나다: 지정된 턴 수를 버티면(RoundSurvivedEvent) 다음
    // 라운드로. 자원 페널티로 라운드가 무너지는 실패 경로는 아직 없다 — 설계되면
    // 그 심판이 EndRun()을 부른다.
    public sealed class RunProgressor
    {
        private readonly IReadOnlyList<RoomDefinition> _rounds;
        private readonly IEventBus _eventBus;

        private int _index = -1;
        private bool _ended;

        public RunProgressor(IReadOnlyList<RoomDefinition> rounds, IEventBus eventBus)
        {
            _rounds = rounds ?? throw new ArgumentNullException(nameof(rounds));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));

            _eventBus.Subscribe<RoundSurvivedEvent>(_ => Advance());
        }

        // 첫 라운드로 진입한다. 구성 루트가 조립을 끝낸 뒤 한 번 호출한다.
        public void Start()
        {
            if (_index >= 0)
                throw new InvalidOperationException("런은 한 번만 시작할 수 있다.");

            _index = 0;
            if (_rounds.Count == 0)
            {
                _ended = true;
                _eventBus.Publish(new RunCompletedEvent());
                return;
            }

            _eventBus.Publish(new RoomStartedEvent(_rounds[0].Id, 0));
        }

        // 다음 라운드로 넘어간다. 이미 런이 끝난 뒤라면 조용히 무시한다.
        public void Advance()
        {
            if (_ended)
                return;

            _index++;
            if (_index >= _rounds.Count)
            {
                _ended = true;
                _eventBus.Publish(new RunCompletedEvent());
                return;
            }

            _eventBus.Publish(new RoomStartedEvent(_rounds[_index].Id, _index));
        }

        // 자원 페널티로 라운드가 무너지면 런은 여기서 끝난다. 이미 끝난 뒤라면
        // 조용히 무시한다.
        public void EndRun()
        {
            if (_ended)
                return;

            _ended = true;
            _eventBus.Publish(new RunCompletedEvent());
        }
    }
}
