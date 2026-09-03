using System;
using GameName.Core.Events;
using GameName.Core.MemoryRooms;
using GameName.Core.Trust;

namespace GameName.Core.Progression
{
    // 방 하나가 어떻게 닫히는지를 정하는 심판.
    //
    // 두 가지 종료 사유를 듣는다: 대화가 끝까지 진행됐다(DialogueEndedEvent)와
    // 신뢰도가 0에 닿았다(TrustChangedEvent). 앞의 것은 성공, 뒤의 것은 실패다.
    // 둘이 동시에 성립하면 — 마지막 오답이 신뢰도를 0으로 만들면서 다음 대사도
    // 없는 경우 — 실패가 이긴다. 신뢰도 0은 TrustChangedEvent로 먼저 동기
    // 발행되므로 그 시점에 이미 판정이 끝나 있고, 뒤이어 오는 DialogueEndedEvent는
    // 래치에 막힌다.
    //
    // 래치(_decided)를 두는 이유: 한 방은 정확히 한 번만 닫혀야 한다. 신뢰도가
    // 0인 채로 TrustChangedEvent가 여러 번 오거나(다른 사유로 0 재확인) 종료
    // 이벤트가 겹쳐 와도 RoomFailedEvent/RoomClearedEvent는 한 번만 나간다.
    public sealed class RoomCompletionArbiter
    {
        private readonly ITrustReader _trust;
        private readonly IEventBus _eventBus;

        private MemoryRoomId _currentRoomId;
        private bool _hasRoom;
        private bool _decided;

        public RoomCompletionArbiter(ITrustReader trust, IEventBus eventBus)
        {
            _trust = trust ?? throw new ArgumentNullException(nameof(trust));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));

            _eventBus.Subscribe<RoomStartedEvent>(OnRoomStarted);
            _eventBus.Subscribe<TrustChangedEvent>(OnTrustChanged);
            _eventBus.Subscribe<DialogueEndedEvent>(OnDialogueEnded);
        }

        private void OnRoomStarted(RoomStartedEvent e)
        {
            _currentRoomId = e.RoomId;
            _hasRoom = true;
            _decided = false;
        }

        private void OnTrustChanged(TrustChangedEvent e)
        {
            if (_decided || !_hasRoom || e.Current != 0)
                return;

            _decided = true;
            _eventBus.Publish(new RoomFailedEvent(_currentRoomId));
        }

        private void OnDialogueEnded(DialogueEndedEvent e)
        {
            if (_decided || !_hasRoom)
                return;

            _decided = true;
            if (_trust.Current == 0)
                _eventBus.Publish(new RoomFailedEvent(_currentRoomId));
            else
                _eventBus.Publish(new RoomClearedEvent(_currentRoomId));
        }
    }
}
