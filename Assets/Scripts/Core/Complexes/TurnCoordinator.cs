using System;
using GameName.Core.Events;

namespace GameName.Core.Complexes
{
    // 라운드 하나의 턴을 세고, 버텨야 하는 턴 수를 채우면 라운드 클리어를 낸다.
    //
    // 옛 RoomPhaseCoordinator + RoomCompletionArbiter 자리를 대신한다 — 조사/대화
    // 국면도, 신뢰 0 판정도 없다. 라운드는 "지정된 턴 수를 버티면 클리어"다.
    //
    // 얇게 유지한다. 무엇이 턴을 넘기는가(단서 사용 1회? 명시적 턴 종료 버튼?)는
    // 이 타입을 부르는 상위 계층의 결정이고, 여기서는 세고 문턱을 넘었는지만
    // 본다. RoomStartedEvent를 구독하지 않는다 — 라운드별 목표 턴 수를
    // RoomDefinition에서 읽어 이 코디네이터를 다시 만드는 배선은 후속 단계의 몫이다.
    //
    // 래치(_survived)를 두는 이유는 RoomCompletionArbiter와 같다: 한 라운드는
    // 정확히 한 번만 클리어된다. 문턱을 넘은 뒤 AdvanceTurn이 더 불려도
    // RoundSurvivedEvent는 한 번만 나간다.
    public sealed class TurnCoordinator : ITurnReader
    {
        private readonly int _turnsToSurvive;
        private readonly IEventBus _eventBus;

        private bool _survived;

        public int CurrentTurn { get; private set; }
        public int TurnsToSurvive => _turnsToSurvive;
        public bool Survived => _survived;

        public TurnCoordinator(int turnsToSurvive, IEventBus eventBus)
        {
            if (turnsToSurvive < 1)
                throw new ArgumentOutOfRangeException(
                    nameof(turnsToSurvive), turnsToSurvive, "버텨야 하는 턴 수는 1 이상이어야 한다.");

            _turnsToSurvive = turnsToSurvive;
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
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
