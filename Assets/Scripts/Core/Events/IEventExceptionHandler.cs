using System;

namespace GameName.Core.Events
{
    // 구독자 핸들러가 던진 예외를 어떻게 처리할지 결정하는 경계.
    // Publish는 이 정책에 예외를 넘기고, 발행 자체는 항상 정상적으로 끝난다 —
    // 그래야 구독자 하나의 버그가 발행자(정신력 게이지 등, 이미 상태를 바꾼
    // 뒤에 발행하는 경우가 많다)쪽으로 역류해 게임 로직을 멈추는 일이 없다.
    public interface IEventExceptionHandler
    {
        void Handle(Type eventType, Exception exception);
    }
}
