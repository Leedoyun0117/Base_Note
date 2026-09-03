using System;
using System.Collections.Generic;
using GameName.Core.Clues;
using GameName.Core.Dialogue;

namespace GameName.UI.MemoryRoom.Dialogue
{
    // 대화 패널 화면 표면. 컨트롤러는 이 경계만 보고, 실제 UI Toolkit 요소를
    // 다루는 것은 DialoguePanelView다.
    //
    // 경계를 둔 이유는 다른 스크린 컨트롤러 테스트와 같다 — 컨트롤러의 규칙
    // (기억제가 없으면 해금하지 않고 안내만, 선택지 클릭은 진행기에 위임,
    // ClueSelection 줄에서는 텍스트 선택지 대신 단서 목록)은 UIDocument를
    // 세우지 않고도 검증되어야 한다.
    public interface IDialoguePanelView
    {
        event Action<CensorKey> MaskClicked;
        event Action<ChoiceId> ChoiceClicked;

        // ClueSelection 줄에서 단서 하나를 답으로 골랐다.
        event Action<ClueId> ClueAnswerClicked;

        // ClueSelection 줄에서 단서로 답하지 않고 넘어간다.
        event Action SkipClueAnswerClicked;

        event Action UnlockConfirmed;
        event Action UnlockCancelled;

        void SetLine(string speaker, string authoredText);

        // 텍스트 선택지.
        void SetChoices(IReadOnlyList<KeyValuePair<ChoiceId, string>> choices);

        // ClueSelection 줄에서 답으로 낼 수 있는 단서 목록((식별자, 표시 이름)).
        void SetClueSelection(IReadOnlyList<KeyValuePair<ClueId, string>> clues);

        void SetNotice(string message);
        void ShowUnlockPrompt(string message);
        void HideUnlockPrompt();
    }
}
