using System;
using GameName.Core.Events;
using GameName.Core.MemoryRooms;
using GameName.Core.Trust;

namespace GameName.Core.Progression
{
    // 방이 신뢰 0으로 무너졌는지 판정하는 심판.
    //
    // 신뢰가 0에 닿으면(TrustChangedEvent) 그 방은 실패로 닫힌다 — RoomFailedEvent를
    // 낸다. RunProgressor가 그것을 받아 런을 끝낸다(다음 방으로 넘어가지 않는다).
    //
    // 대화 완주로 방을 떠나는 것은 여기서 판정하지 않는다 — 대화가 끝나면
    // 화면(대화 패널)에 "다음으로" 버튼이 뜨고, 플레이어가 그것을 눌러야
    // RoomClearedEvent가 나간다. 방을 떠나는 것은 플레이어의 결정이지 자동이
    // 아니다.
    //
    // 래치(_decided)를 두는 이유: 한 방은 정확히 한 번만 무너진다. 신뢰가 0인
    // 채로 TrustChangedEvent가 여러 번 와도 RoomFailedEvent는 한 번만 나간다.
    public sealed class RoomCompletionArbiter
    {
        private readonly IEventBus _eventBus;

        private MemoryRoomId _currentRoomId;
        private bool _hasRoom;
        private bool _decided;

        public RoomCompletionArbiter(ITrustReader trust, IEventBus eventBus)
        {
            if (trust == null) throw new ArgumentNullException(nameof(trust));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));

            _eventBus.Subscribe<RoomStartedEvent>(OnRoomStarted);
            _eventBus.Subscribe<TrustChangedEvent>(OnTrustChanged);
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
    }
}
