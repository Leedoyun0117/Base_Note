using System;
using GameName.Core.Events;
using GameName.Core.MemoryRooms;

namespace GameName.Core.Progression
{
    // 방 하나 안의 국면(조사 → 대화)을 붙잡고, 대화 국면으로 넘어갈 때
    // DialoguePhaseStartedEvent를 낸다.
    //
    // 얇게 유지한다. 방이 시작되면 조사 국면으로 두고, 대화 국면으로 넘어가는
    // 조건(방당 조사 횟수 소진, 또는 플레이어가 먼저 대화를 걸기)은 이 개편의
    // 다음 단계에서 붙는다. 그때까지는 방이 시작되는 즉시 대화 국면으로 넘겨
    // 지금까지와 똑같이 동작하게 둔다 — BeginDialogue()가 public인 것은 다음
    // 단계가 이 자동 전환을 걷어내고 스스로 부를 수 있게 하기 위해서다.
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

            // 다음 단계에서 조사 횟수 카운터가 이 자동 전환을 대체한다.
            BeginDialogue();
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
