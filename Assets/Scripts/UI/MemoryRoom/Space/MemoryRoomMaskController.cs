using System;
using GameName.Core.Events;
using GameName.Core.Trust;

namespace GameName.UI.MemoryRoom.Space
{
    // 신뢰 → 가시 비율을 계산해 마스크 연출과 단서 접근성을 함께 움직인다.
    //
    // 비율 계산은 IVisibilityPolicy에 그대로 위임한다(새 정책을 만들지 않는다).
    // 이 컨트롤러가 하는 일은 "언제 다시 계산하는가"(방 시작, 신뢰 변화)와
    // "그 결과를 누구에게 전하는가"(마스크 뷰, 그리고 VisibleRatioChanged를 듣는
    // 방 공간 컨트롤러)뿐이다.
    //
    // 두 소비자에게 같은 값을 흘려보내는 것이 핵심이다 — 마스크가 덮는 구간과
    // 단서 콜라이더가 꺼지는 구간이 갈라지면 "가려졌는데 집히는" 어긋남이 된다.
    public sealed class MemoryRoomMaskController : IDisposable
    {
        private readonly MemoryRoomMaskView _view;
        private readonly IVisibilityPolicy _visibilityPolicy;
        private readonly ITrustReader _trust;
        private readonly IDisposable[] _subscriptions;

        // 지금 가시 비율. 마스크 뿐 아니라 방 공간 컨트롤러도 이 값으로 단서
        // 접근성을 다시 판정해야 한다.
        public event Action<float> VisibleRatioChanged;

        // 마지막으로 계산된 비율. 구독을 늦게 건 쪽(화면 컨트롤러)이 지금 값을
        // 한 번 받아 가도록 노출한다.
        public float CurrentRatio { get; private set; } = 1f;

        public MemoryRoomMaskController(
            MemoryRoomMaskView view,
            IVisibilityPolicy visibilityPolicy,
            ITrustReader trust,
            IEventBus eventBus)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _visibilityPolicy = visibilityPolicy ?? throw new ArgumentNullException(nameof(visibilityPolicy));
            _trust = trust ?? throw new ArgumentNullException(nameof(trust));
            if (eventBus == null) throw new ArgumentNullException(nameof(eventBus));

            _subscriptions = new[]
            {
                eventBus.Subscribe<RoomStartedEvent>(_ => Apply()),
                eventBus.Subscribe<TrustChangedEvent>(_ => Apply()),
            };

            // 첫 RoomStartedEvent는 이 컨트롤러가 만들어지기 전에 지나갔다.
            Apply();
        }

        private void Apply()
        {
            var ratio = _visibilityPolicy.GetVisibleRatio(_trust.Current);
            CurrentRatio = ratio;
            _view.SetVisibleRatio(ratio);
            VisibleRatioChanged?.Invoke(ratio);
        }

        public void Dispose()
        {
            foreach (var subscription in _subscriptions)
                subscription.Dispose();

            VisibleRatioChanged = null;
        }
    }
}
