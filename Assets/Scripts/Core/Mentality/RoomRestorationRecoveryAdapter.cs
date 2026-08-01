using System;
using GameName.Core.Events;

namespace GameName.Core.Mentality
{
    // MemoryRoomRestoredEvent를 구독해 정신력을 회복시키는 어댑터.
    // 게이지와 방 시스템이 서로 직접 참조하지 않고 이벤트로만 연결되도록 하기
    // 위한 접착 코드이며, 그 자체는 다른 상태를 갖지 않는다. 더 이상 필요
    // 없어지면 Dispose로 구독을 해제한다.
    public sealed class RoomRestorationRecoveryAdapter : IDisposable
    {
        private readonly IDisposable _subscription;

        public RoomRestorationRecoveryAdapter(
            IEventBus eventBus, IMentalityGauge mentalityGauge, IMentalityCostSettings costSettings)
        {
            if (eventBus == null) throw new ArgumentNullException(nameof(eventBus));
            if (mentalityGauge == null) throw new ArgumentNullException(nameof(mentalityGauge));
            if (costSettings == null) throw new ArgumentNullException(nameof(costSettings));

            _subscription = eventBus.Subscribe<MemoryRoomRestoredEvent>(
                _ => mentalityGauge.Restore(costSettings.MemoryRoomFullRestorationRecovery));
        }

        public void Dispose() => _subscription.Dispose();
    }
}
