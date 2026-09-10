using System;
using System.Collections.Generic;
using GameName.Core.Events;
using GameName.Core.Mind;

namespace GameName.Core.Complexes
{
    // 단서 해석이 끝나면(ClueInterpretedEvent) 그 최종 태그의 감정 축을 보고
    // 나츠의 안정 축을 움직이는 리스너.
    //
    // 옛 DialogueProgressor가 대사 줄의 StabilityDelta를 직접 밀던 것을 대체한다.
    // 이제 축을 움직이는 것은 저작된 대사가 아니라 "단서 원본 태그가 활성
    // 컴플렉스 체인을 통과한 결과"다 — 같은 단서라도 그때 어떤 컴플렉스가
    // 걸려 있었느냐에 따라 감정이 달라지고, 그 달라진 감정이 축을 민다.
    //
    // 감정 값 → 이동량 대응은 밸런싱 데이터라 표로 주입받는다("그리움" -3,
    // "분노" +8 처럼). 표에 없는 감정은 0으로 친다 — 저작이 아직 수치를 안
    // 매긴 감정이 축을 요동치게 하지 않는다.
    //
    // 얼마를 밀지만 계산하고 양 끝 자르기·이벤트는 축에 맡긴다 —
    // StabilityTrustErosionListener가 게이지에 맡기던 것과 같은 분업이다.
    public sealed class ClueInterpretationStabilityListener
    {
        private readonly IStabilityAxis _stability;
        private readonly IReadOnlyDictionary<string, int> _shiftByEmotion;

        public ClueInterpretationStabilityListener(
            IStabilityAxis stability,
            IReadOnlyDictionary<string, int> shiftByEmotion,
            IEventBus eventBus)
        {
            _stability = stability ?? throw new ArgumentNullException(nameof(stability));
            _shiftByEmotion = shiftByEmotion ?? throw new ArgumentNullException(nameof(shiftByEmotion));
            if (eventBus == null) throw new ArgumentNullException(nameof(eventBus));

            eventBus.Subscribe<ClueInterpretedEvent>(OnInterpreted);
        }

        private void OnInterpreted(ClueInterpretedEvent e)
        {
            if (e.FinalTags == null)
                return;

            var total = 0;
            foreach (var tag in e.FinalTags)
            {
                if (tag.Axis == StoryTagAxis.Emotion
                    && _shiftByEmotion.TryGetValue(tag.Value, out var shift))
                {
                    total += shift;
                }
            }

            if (total != 0)
                _stability.Shift(total);
        }
    }
}
