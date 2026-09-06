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

        // 제시 목록에서 기억 하나를 골라 검열 해금을 시도한다. 실린 값은
        // 그 기억의 출처 단서 id다 — 지금 손에 든 추출된 기억은 그 id로만
        // 식별된다.
        event Action<ClueId> MemoryPresented;
        event Action UnlockCancelled;

        void SetLine(string speaker, string authoredText);

        // 텍스트 선택지.
        void SetChoices(IReadOnlyList<KeyValuePair<ChoiceId, string>> choices);

        // ClueSelection 줄에서 답으로 낼 수 있는 단서 목록((식별자, 표시 이름)).
        void SetClueSelection(IReadOnlyList<KeyValuePair<ClueId, string>> clues);

        void SetNotice(string message);

        // 마스크 구간을 눌렀을 때 뜨는 팝업. memoryOptions는 지금 제시할 수 있는
        // 추출된 기억 목록((출처 단서 id, 표시 문구) — 예: "낡은 모포 (파랑)")이다.
        void ShowUnlockPrompt(string message, IReadOnlyList<KeyValuePair<ClueId, string>> memoryOptions);
        void HideUnlockPrompt();
    }
}
