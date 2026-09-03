using System;

namespace GameName.Core.Extraction
{
    // IExtractionBudgetSpender 기본 구현. 한 판 동안 몇 번 더 추출할 수 있는지를 든다.
    //
    // 방이 바뀌어도 리셋되지 않는다 — 추출 자원은 런 전체에 걸쳐 유한하고, 그
    // 희소함이 "무엇을 추출할지" 고민을 만든다. RoomStartedEvent를 구독하지 않는
    // 것이 곧 그 규칙이다.
    //
    // Spend는 남은 횟수가 0이면 예외를 던진다. 자원이 부족한지 먼저 확인하는
    // 책임은 처리기에 있고(부분 성공 없이 통째로 실패해야 하므로), 여기까지
    // 왔는데 0이라면 그것은 처리기의 검사 누락이라는 내부 오류다.
    public sealed class ExtractionBudget : IExtractionBudgetSpender
    {
        public int Remaining { get; private set; }

        public ExtractionBudget(int initial)
        {
            if (initial < 0)
                throw new ArgumentOutOfRangeException(nameof(initial), initial, "추출 자원은 음수로 시작할 수 없다.");

            Remaining = initial;
        }

        public void Spend()
        {
            if (Remaining <= 0)
                throw new InvalidOperationException("남은 추출 자원이 없는데 소모가 시도되었다.");

            Remaining--;
        }
    }
}
