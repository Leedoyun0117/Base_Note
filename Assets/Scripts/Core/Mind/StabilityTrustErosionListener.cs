using System;
using GameName.Core.Events;
using GameName.Core.Trust;

namespace GameName.Core.Mind
{
    // 나츠의 안정 축이 중심에서 멀리 벗어나 있으면, 답변할 때마다 유키의
    // 인내심(신뢰)이 조금씩 깎이는 리스너.
    //
    // 규칙: |안정 위치| 가 자유 폭(freeBand) 이내면 아무 일도 없다. 넘어서면
    // round((|위치| - freeBand) / divisor) 만큼 깎는다 — 정수 반올림(반올림
    // 반은 올림)을 (초과분 + divisor/2) / divisor 로 계산한다. 답변이 정답인지
    // 오답인지는 보지 않는다 — 이 깎임은 답의 질이 아니라 "지금 나츠가 얼마나
    // 불안정한 상태로 대화하고 있는가"에 매긴다.
    //
    // "답변 한 번"은 단서로 답하거나 넘어가는 것(ClueAnsweredEvent)으로 본다 —
    // 히로민 적립(HiromiDialogueEarningListener)이 같은 사건을 세는 것과 같다.
    // 텍스트 선택지(ChoiceSelectedEvent)는 콘텐츠 없는 레거시 경로라 여기서는
    // 세지 않는다.
    //
    // 얼마를 깎을지만 계산하고 0 하한·이벤트·런 종료 판정은 전부 게이지와 그
    // 뒤의 심판에게 맡긴다 — TrustGauge가 "무엇 때문에" 깎이는지 모르는 것과
    // 짝을 이룬다.
    public sealed class StabilityTrustErosionListener
    {
        private readonly IStabilityReader _stability;
        private readonly ITrustGauge _trust;
        private readonly int _freeBand;
        private readonly int _divisor;

        public StabilityTrustErosionListener(
            IStabilityReader stability, ITrustGauge trust, int freeBand, int divisor, IEventBus eventBus)
        {
            _stability = stability ?? throw new ArgumentNullException(nameof(stability));
            _trust = trust ?? throw new ArgumentNullException(nameof(trust));
            if (freeBand < 0)
                throw new ArgumentOutOfRangeException(nameof(freeBand), freeBand, "자유 폭은 음수일 수 없다.");
            if (divisor < 1)
                throw new ArgumentOutOfRangeException(nameof(divisor), divisor, "제수는 1 이상이어야 한다.");
            if (eventBus == null) throw new ArgumentNullException(nameof(eventBus));

            _freeBand = freeBand;
            _divisor = divisor;

            eventBus.Subscribe<ClueAnsweredEvent>(_ => Erode());
        }

        private void Erode()
        {
            var distance = Math.Abs(_stability.Position);
            if (distance <= _freeBand)
                return;

            var over = distance - _freeBand;
            var amount = (over + _divisor / 2) / _divisor;
            _trust.Decrease(amount);
        }
    }
}
