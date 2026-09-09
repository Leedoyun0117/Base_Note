using System;
using GameName.Core.Events;
using GameName.Core.MemoryRooms;

namespace GameName.Core.Progression
{
    // 조사 국면에서 단서를 방당 몇 개까지 집을 수 있는지 세고, 다 쓰면
    // 대화 국면으로 넘긴다.
    //
    // "조사 한 번" = 단서 하나 수집(ClueCollectedEvent). 그 수가 방당 한도에
    // 닿으면 RoomPhaseCoordinator.BeginDialogue()를 부른다 — [8]에서 방이
    // 시작되는 즉시 하던 자동 전환을 이 카운터가 대체한다.
    //
    // 한도는 RunDefinition 데이터(InvestigationsPerRoom)다. 0이면 조사 없이
    // 방이 시작되자마자 대화로 넘어간다.
    public sealed class RoomInvestigationCounter
    {
        private readonly RoomPhaseCoordinator _phase;
        private readonly int _perRoom;

        private int _count;

        public RoomInvestigationCounter(
            RoomPhaseCoordinator phase, int investigationsPerRoom, IEventBus eventBus)
        {
            _phase = phase ?? throw new ArgumentNullException(nameof(phase));
            if (investigationsPerRoom < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(investigationsPerRoom), investigationsPerRoom, "조사 한도는 음수일 수 없다.");
            if (eventBus == null) throw new ArgumentNullException(nameof(eventBus));

            _perRoom = investigationsPerRoom;

            eventBus.Subscribe<RoomPhaseChangedEvent>(OnPhaseChanged);
            eventBus.Subscribe<ClueCollectedEvent>(_ => OnInvestigated());
        }

        private void OnPhaseChanged(RoomPhaseChangedEvent e)
        {
            if (e.Phase != RoomPhase.Investigation)
                return;

            _count = 0;

            // 한도가 0이면 집을 것 없이 곧장 대화로.
            if (_perRoom == 0)
                _phase.BeginDialogue();
        }

        private void OnInvestigated()
        {
            if (_phase.Current != RoomPhase.Investigation)
                return;

            _count++;
            if (_count >= _perRoom)
                _phase.BeginDialogue();
        }
    }
}
