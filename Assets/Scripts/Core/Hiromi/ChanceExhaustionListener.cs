using System;
using GameName.Core.Events;

namespace GameName.Core.Hiromi
{
    // 기회가 0에 닿으면 런을 끝내는 리스너.
    //
    // RunCompletedEvent를 재사용한다 — 방을 전부 마쳐 끝나는 것과 기회가
    // 바닥나 끝나는 것은 사유가 다르지만, 이 사건을 듣는 화면(DialoguePanelController의
    // 임시 종료 표시)은 지금 "런이 여기서 끝났다"는 사실 하나만 안다. 사유를
    // 구분해 보여 주는 일은 정식 결과 화면이 생길 때의 몫으로 남긴다.
    public sealed class ChanceExhaustionListener
    {
        public ChanceExhaustionListener(IEventBus eventBus)
        {
            if (eventBus == null) throw new ArgumentNullException(nameof(eventBus));

            eventBus.Subscribe<ChanceChangedEvent>(e =>
            {
                if (e.Current == 0)
                    eventBus.Publish(new RunCompletedEvent());
            });
        }
    }
}
