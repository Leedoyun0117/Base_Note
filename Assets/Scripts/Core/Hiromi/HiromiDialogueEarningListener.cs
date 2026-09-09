using System;
using GameName.Core.Events;
using GameName.Core.Mind;

namespace GameName.Core.Hiromi
{
    // 대화 한 번마다 히로민을 회복시키는 리스너.
    //
    // 회복량 = Base + round(max(0, BonusBand - |안정 위치|) / BonusDivisor).
    // 안정(0)에 가까울수록 더 벌고, |위치|가 BonusBand를 넘어서면 Base만 회복한다 —
    // 나츠가 흔들릴수록 추출에 쓸 밑천이 마른다. 세 수치는 전부 RunDefinition
    // 데이터다.
    //
    // "대화 한 번"을 텍스트 선택지 하나 고르는 것(ChoiceSelectedEvent)과 단서로
    // 답하는 줄에서 답하거나 넘어가는 것(ClueAnsweredEvent) 둘 다로 본다 —
    // 어느 쪽이든 플레이어가 이번 대사에 응답했다는 사실은 같다. 오답이나
    // 넘어가기도 포함한다 — 이 회복은 정답을 맞혔다는 보상이 아니라 대화를
    // 이어간다는 것 자체에 대한 것이기 때문이다.
    public sealed class HiromiDialogueEarningListener
    {
        private readonly IHiromiMutator _hiromi;
        private readonly IStabilityReader _stability;
        private readonly int _base;
        private readonly int _bonusBand;
        private readonly int _bonusDivisor;

        public HiromiDialogueEarningListener(
            IHiromiMutator hiromi,
            IStabilityReader stability,
            int dialogueHiromiBase,
            int stabilityBonusBand,
            int stabilityBonusDivisor,
            IEventBus eventBus)
        {
            _hiromi = hiromi ?? throw new ArgumentNullException(nameof(hiromi));
            _stability = stability ?? throw new ArgumentNullException(nameof(stability));
            if (dialogueHiromiBase < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(dialogueHiromiBase), dialogueHiromiBase, "기본 회복량은 음수일 수 없다.");
            if (stabilityBonusBand < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(stabilityBonusBand), stabilityBonusBand, "보너스 폭은 음수일 수 없다.");
            if (stabilityBonusDivisor < 1)
                throw new ArgumentOutOfRangeException(
                    nameof(stabilityBonusDivisor), stabilityBonusDivisor, "제수는 1 이상이어야 한다.");
            if (eventBus == null) throw new ArgumentNullException(nameof(eventBus));

            _base = dialogueHiromiBase;
            _bonusBand = stabilityBonusBand;
            _bonusDivisor = stabilityBonusDivisor;

            eventBus.Subscribe<ChoiceSelectedEvent>(_ => Earn());
            eventBus.Subscribe<ClueAnsweredEvent>(_ => Earn());
        }

        private void Earn()
        {
            var within = _bonusBand - Math.Abs(_stability.Position);
            var bonus = within <= 0 ? 0 : (within + _bonusDivisor / 2) / _bonusDivisor;
            _hiromi.Earn(_base + bonus);
        }
    }
}
