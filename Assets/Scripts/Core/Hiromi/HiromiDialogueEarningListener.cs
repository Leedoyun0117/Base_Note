using System;
using GameName.Core.Events;

namespace GameName.Core.Hiromi
{
    // 대화 한 번마다 히로민 3을 벌어들이는 리스너.
    //
    // "대화 한 번"을 텍스트 선택지 하나 고르는 것(ChoiceSelectedEvent)과 단서로
    // 답하는 줄에서 답하거나 넘어가는 것(ClueAnsweredEvent) 둘 다로 본다 —
    // 어느 쪽이든 플레이어가 이번 대사에 응답했다는 사실은 같다. 오답이나
    // 넘어가기도 포함한다 — 이 적립은 정답을 맞혔다는 보상이 아니라 대화를
    // 이어간다는 것 자체에 대한 보상이기 때문이다.
    public sealed class HiromiDialogueEarningListener
    {
        private const int EarnedPerTurn = 3;

        public HiromiDialogueEarningListener(IHiromiMutator hiromi, IEventBus eventBus)
        {
            if (hiromi == null) throw new ArgumentNullException(nameof(hiromi));
            if (eventBus == null) throw new ArgumentNullException(nameof(eventBus));

            eventBus.Subscribe<ChoiceSelectedEvent>(_ => hiromi.Earn(EarnedPerTurn));
            eventBus.Subscribe<ClueAnsweredEvent>(_ => hiromi.Earn(EarnedPerTurn));
        }
    }
}
