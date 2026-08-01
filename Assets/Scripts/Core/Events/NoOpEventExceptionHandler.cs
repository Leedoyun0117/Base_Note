using System;

namespace GameName.Core.Events
{
    // IEventExceptionHandler의 가장 단순한 기본 구현 — 아무 것도 하지 않는다.
    // 이 계층은 UnityEngine.Debug 같은 로깅 수단에 의존할 수 없으므로, 실제
    // 로깅/텔레메트리가 필요한 게임은 그 수단으로 예외를 기록하는 구현체를
    // 직접 만들어 주입해야 한다. 이 타입은 "아무 정책도 없을 때"의 안전한
    // 기본값일 뿐이다.
    public sealed class NoOpEventExceptionHandler : IEventExceptionHandler
    {
        public void Handle(Type eventType, Exception exception)
        {
        }
    }
}
