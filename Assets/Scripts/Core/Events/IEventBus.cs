using System;

namespace GameName.Core.Events
{
    // 시스템 간 통신 경계.
    // 시스템끼리 서로를 직접 참조하지 않고 이벤트 타입만으로 발행/구독하게 하여
    // 결합을 낮춘다.
    public interface IEventBus
    {
        void Publish<TEvent>(TEvent gameEvent);
        IDisposable Subscribe<TEvent>(Action<TEvent> handler);
    }
}
