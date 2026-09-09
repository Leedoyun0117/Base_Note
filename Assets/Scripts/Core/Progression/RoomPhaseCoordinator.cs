using System;
using GameName.Core.Events;
using GameName.Core.MemoryRooms;

namespace GameName.Core.Progression
{
    // 방 하나 안의 국면(조사 → 대화)을 붙잡고, 대화 국면으로 넘어갈 때
    // DialoguePhaseStartedEvent를 낸다.
    //
    // 얇게 유지한다. 방이 시작되면 조사 국면으로 두기만 하고, 대화 국면으로
    // 넘기는 것은 RoomInvestigationCounter가 조사 횟수를 다 쓸 때 BeginDialogue()를
    // 불러 한다. BeginDialogue()가 public인 것은 그 카운터가, 그리고 나중에
    // "대화 시작" 버튼이 부를 수 있게 하기 위해서다.
    public sealed class RoomPhaseCoordinator : IRoomPhaseReader
    {
        private readonly IEventBus _eventBus;

        private MemoryRoomId _roomId;
        private int _roomIndex;

        public RoomPhase Current { get; private set; } = RoomPhase.Investigation;

        public RoomPhaseCoordinator(IEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _eventBus.Subscribe<RoomStartedEvent>(OnRoomStarted);
        }

        private void OnRoomStarted(RoomStartedEvent e)
        {
            _roomId = e.RoomId;
            _roomIndex = e.RoomIndex;

            Current = RoomPhase.Investigation;
            _eventBus.Publish(new RoomPhaseChangedEvent(RoomPhase.Investigation));
        }

        // 대화 국면으로 넘어간다. 이미 대화 국면이면 조용히 무시한다 — 자동
        // 전환과 (다음 단계에서 붙을) 명시적 호출이 겹칠 수 있어서다.
        public void BeginDialogue()
        {
            if (Current == RoomPhase.Dialogue)
                return;

            Current = RoomPhase.Dialogue;
            _eventBus.Publish(new RoomPhaseChangedEvent(RoomPhase.Dialogue));
            _eventBus.Publish(new DialoguePhaseStartedEvent(_roomId, _roomIndex));
        }
    }
}
