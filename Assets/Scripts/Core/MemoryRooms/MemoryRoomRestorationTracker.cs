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
    public sealed class MemoryRoomRestorationTracker : IMemoryRoomRestorationTracker
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
    }
}
