using System;
using GameName.Core.Events;
using GameName.Core.Trust;

namespace GameName.UI.MemoryRoom.Space
{
    // 신뢰 상태를 카메라 흔들림으로 옮긴다.
    //
    //   · 신뢰가 한 칸 깎이는 순간 → 짧은 임펄스 한 번. 0으로 떨어졌으면 더 크게.
    //   · 신뢰가 문턱 이하로 내려가 있는 동안 → 지속 떨림을 켠 채로 둔다.
    //
    // "얼마나" 흔들지는 CameraShake(인스펙터)가, "언제"는 이 컨트롤러가 정한다.
    // 문턱은 생성 시 주입받는다 — 기본은 1(마지막 한 칸)이다.
    public sealed class MemoryRoomCameraShakeController : IDisposable
    {
        private readonly CameraShake _cameraShake;
        private readonly ITrustReader _trust;
        private readonly int _continuousAtOrBelow;
        private readonly IDisposable[] _subscriptions;

        public MemoryRoomCameraShakeController(
            CameraShake cameraShake, ITrustReader trust, IEventBus eventBus, int continuousAtOrBelow = 1)
        {
            _cameraShake = cameraShake ?? throw new ArgumentNullException(nameof(cameraShake));
            _trust = trust ?? throw new ArgumentNullException(nameof(trust));
            if (eventBus == null) throw new ArgumentNullException(nameof(eventBus));

            _continuousAtOrBelow = continuousAtOrBelow;

            _subscriptions = new[]
            {
                eventBus.Subscribe<TrustChangedEvent>(OnTrustChanged),
                // 방이 바뀌면 신뢰가 시작값으로 리셋되므로 지속 떨림도 다시 판정한다.
                eventBus.Subscribe<RoomStartedEvent>(_ => SyncContinuous()),
            };

            SyncContinuous();
        }

        private void OnTrustChanged(TrustChangedEvent change)
        {
            if (change.Current < change.Previous)
                _cameraShake.Shake(strong: change.Current <= 0);

            SyncContinuous();
        }

        private void SyncContinuous() =>
            _cameraShake.SetContinuous(_trust.Current <= _continuousAtOrBelow);

        public void Dispose()
        {
            foreach (var subscription in _subscriptions)
                subscription.Dispose();

            _cameraShake.SetContinuous(false);
        }
    }
}
