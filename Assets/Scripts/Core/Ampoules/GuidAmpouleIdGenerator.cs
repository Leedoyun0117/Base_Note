using System;

namespace GameName.Core.Ampoules
{
    // IAmpouleIdGenerator의 기본 구현. Guid를 이용해 충돌 걱정 없는 식별자를
    // 만든다. 결정적인 식별자가 필요한 테스트나, 세이브/로드에서 다른 채번
    // 방식이 필요해지면 이 구현체 대신 다른 IAmpouleIdGenerator를 주입하면 된다.
    public sealed class GuidAmpouleIdGenerator : IAmpouleIdGenerator
    {
        public AmpouleId Generate() => new AmpouleId(Guid.NewGuid().ToString());
    }
}
