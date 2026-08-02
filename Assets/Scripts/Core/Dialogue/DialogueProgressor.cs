using System;
using System.Collections.Generic;
using GameName.Core.Events;
using GameName.Core.Journal;

namespace GameName.Core.Dialogue
{
    // IDialogueProgressor 기본 구현.
    //
    // 지금 보여주는 대사가 바뀔 때마다(Reset으로 처음 줄을 보여줄 때, Advance로
    // 다음 줄로 넘어갈 때) DialogueLineShownEvent를 직접 발행한다 — 기록지가
    // 이 이벤트 하나만 구독하면 되므로, 화면(UI)이 별도로 IJournal을 호출해
    // 기록을 남기는 두 번째 경로가 생기지 않는다. 화면은 몇 번을 다시 그리든
    // 이 타입이 실제로 줄을 옮길 때만 이벤트가 나가므로 중복 기록도 생기지
    // 않는다.
    // IDialogueScriptLoader도 함께 구현한다 — 이 객체는 FlowOverlay라는 상시
    // 컴포넌트가 참조를 들고 있어 의뢰가 바뀌어도 새로 만들 수 없으므로,
    // 대본 교체는 객체 정체성을 유지한 채 내부 대본만 바꾸는 LoadScript로만
    // 가능하다.
    public sealed class DialogueProgressor : IDialogueProgressor, IDialogueScriptLoader
    {
        private readonly IEventBus _eventBus;
        private DialogueScript _script;
        private int _currentIndex;

        public DialogueProgressor(DialogueScript script, IEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _script = script ?? throw new ArgumentNullException(nameof(script));
            _currentIndex = 0;
        }

        public DialogueLine CurrentLine => _script.Nodes[_currentIndex].Line;
        public IReadOnlyList<DialogueOption> CurrentOptions => _script.Nodes[_currentIndex].Options;
        public bool IsFinished => CurrentOptions.Count == 0;

        public bool Advance(int optionIndex)
        {
            var options = CurrentOptions;
            if (optionIndex < 0 || optionIndex >= options.Count)
                return false;

            _currentIndex = options[optionIndex].NextNodeIndex;
            _eventBus.Publish(new DialogueLineShownEvent(CurrentLine));
            return true;
        }

        // 대본 자체를 교체한다. 새 의뢰의 대사가 이전 의뢰와 전혀 다르므로
        // "처음으로 되돌리기"가 아니라 "무엇으로 되돌릴지"부터 바뀌어야 하기
        // 때문에 인자 없는 Reset이 아니라 이 메서드로 노출한다.
        public void LoadScript(DialogueScript script)
        {
            _script = script ?? throw new ArgumentNullException(nameof(script));
            _currentIndex = 0;
            _eventBus.Publish(new DialogueLineShownEvent(CurrentLine));
        }
    }
}
