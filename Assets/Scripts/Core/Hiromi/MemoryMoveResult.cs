namespace GameName.Core.Hiromi
{
    // "다음 기억으로 이동" 시도의 결과. 이동 자체는 늘 일어난다(플레이어가
    // 마음대로 떠날 수 있어야 한다는 것이 이 행동의 취지다) — 다만 히로민이
    // 모자라 강제로 떠났는지(Forced)는 화면이 경고하려면 알아야 한다.
    public sealed class MemoryMoveResult
    {
        public bool Forced { get; }

        // 이 이동으로 기회가 바닥나 런이 끝났는가. 그렇다면 다음 방은 없다.
        public bool RunEnded { get; }

        private MemoryMoveResult(bool forced, bool runEnded)
        {
            Forced = forced;
            RunEnded = runEnded;
        }

        public static MemoryMoveResult Normal() => new MemoryMoveResult(false, false);
        public static MemoryMoveResult ForcedMove(bool runEnded) => new MemoryMoveResult(true, runEnded);
    }
}
