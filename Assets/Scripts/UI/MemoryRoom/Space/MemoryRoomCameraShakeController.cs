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
    //
    // 신뢰는 이제 런 전체에 걸쳐 이어지므로(방마다 리셋되지 않는다) 방 시작을
    // 계기로 다시 판정할 것이 없다 — TrustChangedEvent 하나만 듣는다.
    public sealed class MemoryRoomCameraShakeController : IDisposable
    {
        private readonly CameraShake _cameraShake;
        private readonly ITrustReader _trust;
        private readonly int _continuousAtOrBelow;
        private readonly IDisposable _subscription;

        public MemoryRoomCameraShakeController(
            CameraShake cameraShake, ITrustReader trust, IEventBus eventBus, int continuousAtOrBelow = 1)
        {
            _cameraShake = cameraShake ?? throw new ArgumentNullException(nameof(cameraShake));
            _trust = trust ?? throw new ArgumentNullException(nameof(trust));
            if (eventBus == null) throw new ArgumentNullException(nameof(eventBus));

            _continuousAtOrBelow = continuousAtOrBelow;

            _subscription = eventBus.Subscribe<TrustChangedEvent>(OnTrustChanged);

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
            _subscription.Dispose();
            _cameraShake.SetContinuous(false);
        }
    }
}
