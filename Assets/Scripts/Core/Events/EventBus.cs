using System;
using System.Collections.Generic;

namespace GameName.Core.Events
{
    // IEventBus 기본 구현. 싱글톤이 아니다 — 필요한 쪽이 생성자로 주입받아 쓴다.
    //
    // 발행 도중 구독자가 추가/제거되어도 예외가 나지 않도록, 발행 시점의 구독자
    // 목록을 배열로 스냅샷 떠서 순회한다. 스냅샷 이후의 구독/해제는 원본 리스트에만
    // 반영되고 이번 발행에는 영향을 주지 않는다.
    //
    // 한 구독자가 예외를 던져도 나머지 구독자는 계속 알림을 받아야 하고, Publish
    // 자체도 예외 없이 끝나야 한다 — 발행자가 이미 상태를 바꾼 뒤에 발행하는
    // 경우가 많아, 구독자 예외가 발행자 쪽으로 역류하면 UI
    // 구독자 하나의 버그로 게임 로직 전체가 멈춰버리기 때문이다. 그렇다고 조용히
    // 버리지는 않고 IEventExceptionHandler에 위임해 처리 방침을 밖에서 정하게 한다.
    public sealed class EventBus : IEventBus
    {
        private readonly Dictionary<Type, List<Delegate>> _handlersByEventType =
            new Dictionary<Type, List<Delegate>>();
        private readonly IEventExceptionHandler _exceptionHandler;

        public EventBus(IEventExceptionHandler exceptionHandler)
        {
            _exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));
        }

        public void Publish<TEvent>(TEvent gameEvent)
        {
            if (!_handlersByEventType.TryGetValue(typeof(TEvent), out var handlers) || handlers.Count == 0)
                return;

            var snapshot = handlers.ToArray();
            foreach (var handler in snapshot)
            {
                try
                {
                    ((Action<TEvent>)handler).Invoke(gameEvent);
                }
                catch (Exception exception)
                {
                    _exceptionHandler.Handle(typeof(TEvent), exception);
                }
            }
        }

        public IDisposable Subscribe<TEvent>(Action<TEvent> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            List<Delegate> handlers;
            if (!_handlersByEventType.TryGetValue(typeof(TEvent), out handlers))
            {
                handlers = new List<Delegate>();
                _handlersByEventType[typeof(TEvent)] = handlers;
            }

            handlers.Add(handler);
            return new Subscription(this, typeof(TEvent), handler);
        }

        private void RemoveHandler(Type eventType, Delegate handler)
        {
            if (!_handlersByEventType.TryGetValue(eventType, out var handlers))
                return;

            handlers.Remove(handler);

            // 구독자가 하나도 안 남으면 빈 리스트를 위해 Dictionary 항목까지 정리한다.
            if (handlers.Count == 0)
                _handlersByEventType.Remove(eventType);
        }

        // Subscribe가 돌려주는 구독 해제 토큰. 발행 중인 스냅샷은 이미 복사본이라
        // 이 시점의 제거로부터 영향을 받지 않는다.
        private sealed class Subscription : IDisposable
        {
            private readonly EventBus _owner;
            private readonly Type _eventType;
            private Delegate _handler;

            public Subscription(EventBus owner, Type eventType, Delegate handler)
            {
                _owner = owner;
                _eventType = eventType;
                _handler = handler;
            }

            public void Dispose()
            {
                if (_handler == null)
                    return;

                _owner.RemoveHandler(_eventType, _handler);
                _handler = null;
            }
        }
    }
}
