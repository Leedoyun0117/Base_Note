using System;
using System.Collections.Generic;
using GameName.Core.Authoring;
using GameName.Core.Events;

namespace GameName.Core.Complexes
{
    // 라운드 하나의 턴을 세고, 버텨야 하는 턴 수를 채우면 라운드 클리어를 낸다.
    //
    // 옛 RoomPhaseCoordinator + RoomCompletionArbiter 자리를 대신한다 — 조사/대화
    // 국면도, 신뢰 0 판정도 없다. 라운드는 "지정된 턴 수를 버티면 클리어"다.
    //
    // RoomStartedEvent를 구독해 라운드가 바뀔 때마다 턴 카운터를 0으로 되돌리고
    // 그 라운드의 목표 턴 수(RoomDefinition.TurnsToSurvive)를 새로 읽는다 —
    // 그래서 RunProgressor·ClueStateStore와 같은 방식으로 라운드 목록을 받는다.
    //
    // 무엇이 턴을 넘기는가(단서 사용 1회 = 1턴)는 ClueUseProcessor의 결정이고,
    // 여기서는 세고 문턱을 넘었는지만 본다.
    //
    // 래치(_survived)를 두는 이유: 한 라운드는 정확히 한 번만 클리어된다. 문턱을
    // 넘은 뒤 AdvanceTurn이 더 불려도 RoundSurvivedEvent는 한 번만 나간다.
    public sealed class TurnCoordinator : ITurnReader
    {
        private readonly IReadOnlyList<RoomDefinition> _rounds;
        private readonly IEventBus _eventBus;

        private int _turnsToSurvive;
        private bool _survived;

        public int CurrentTurn { get; private set; }
        public int TurnsToSurvive => _turnsToSurvive;
        public bool Survived => _survived;

        public TurnCoordinator(IReadOnlyList<RoomDefinition> rounds, IEventBus eventBus)
        {
            _rounds = rounds ?? throw new ArgumentNullException(nameof(rounds));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));

            // 첫 RoomStartedEvent 전에 AdvanceTurn이 불릴 일은 없지만(구성 루트가
            // 조립 뒤 RunProgressor.Start()로 첫 사건을 낸다), 방어적으로 1로 둔다.
            _turnsToSurvive = _rounds.Count > 0 ? _rounds[0].TurnsToSurvive : 1;

            _eventBus.Subscribe<RoomStartedEvent>(OnRoundStarted);
        }

        private void OnRoundStarted(RoomStartedEvent e)
        {
            CurrentTurn = 0;
            _survived = false;
            _turnsToSurvive = (e.RoomIndex >= 0 && e.RoomIndex < _rounds.Count)
                ? _rounds[e.RoomIndex].TurnsToSurvive
                : 1;
        }

        public void AdvanceTurn()
        {
            CurrentTurn++;
            _eventBus.Publish(new TurnAdvancedEvent(CurrentTurn, _turnsToSurvive));

            if (_survived || CurrentTurn < _turnsToSurvive)
                return;

            _survived = true;
            _eventBus.Publish(new RoundSurvivedEvent());
        }
    }
}
