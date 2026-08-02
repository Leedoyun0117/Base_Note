using System;
using System.Collections.Generic;
using GameName.Core.Events;
using GameName.Core.Judging;

namespace GameName.Core.MemoryRooms
{
    // IMemoryRoomRestorationTracker 기본 구현.
    // 완전 복원의 정의: 시향 판정 결과가 최상위 피드백 단계(PianoAndViolinAndDrum)인
    // 경우다. 이미 복원된 방을 다시 보고해도 HashSet.Add가 false를 돌려주는 것을
    // 그대로 이용해 이벤트가 중복 발행되지 않도록 막는다.
    // IResettable도 함께 구현한다 — 복원 기록을 통째로 지우는 권한은
    // IMemoryRoomRestorationTracker(정상 동작 인터페이스)가 아니라 오직 이
    // 좁은 인터페이스로만 노출되고, CommissionSession만 그 권한을 받는다.
    public sealed class MemoryRoomRestorationTracker : IMemoryRoomRestorationTracker, IResettable
    {
        private readonly IEventBus _eventBus;
        private readonly HashSet<MemoryRoomId> _restoredRooms = new HashSet<MemoryRoomId>();

        public MemoryRoomRestorationTracker(IEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        public bool IsRestored(MemoryRoomId roomId) => _restoredRooms.Contains(roomId);

        public void ReportJudgement(MemoryRoomId roomId, ScentJudgementResult result)
        {
            if (result.Stage != FeedbackStage.PianoAndViolinAndDrum)
                return;

            if (!_restoredRooms.Add(roomId))
                return;

            _eventBus.Publish(new MemoryRoomRestoredEvent(roomId));
        }

        // 초기화는 "아직 아무것도 복원되지 않았다"는 사실 하나일 뿐, 개별 방이
        // 복원되었다가 취소된 사건이 아니므로 MemoryRoomRestoredEvent를 발행하지
        // 않는다.
        public void Reset() => _restoredRooms.Clear();
    }
}
