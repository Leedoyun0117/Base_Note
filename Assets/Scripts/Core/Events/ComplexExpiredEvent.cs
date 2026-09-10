using GameName.Core.Complexes;

namespace GameName.Core.Events
{
    // 컴플렉스 하나가 지속 턴을 다 써 소멸했다는 사실.
    //
    // ActiveComplexList가 매 턴 지속 턴을 줄이다 0이 되면 목록에서 빼고 이
    // 사건을 낸다. 화면·해석 로그가 이걸 듣고 다시 그린다.
    public readonly struct ComplexExpiredEvent
    {
        public ComplexId ComplexId { get; }

        public ComplexExpiredEvent(ComplexId complexId)
        {
            ComplexId = complexId;
        }
    }
}
