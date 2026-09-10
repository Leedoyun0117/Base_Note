using GameName.Core.Complexes;

namespace GameName.Core.Events
{
    // 컴플렉스 하나가 활성 목록에 새로 들어왔다는 사실 — 라운드 시작 컴플렉스든,
    // 안정 축 이탈로 확률에 걸려 발생한 것이든.
    //
    // 화면(활성 컴플렉스 표시)과 해석 로그가 이 사건으로 목록을 다시 읽는다.
    public readonly struct ComplexActivatedEvent
    {
        public ComplexId ComplexId { get; }
        public int RemainingTurns { get; }

        public ComplexActivatedEvent(ComplexId complexId, int remainingTurns)
        {
            ComplexId = complexId;
            RemainingTurns = remainingTurns;
        }
    }
}
