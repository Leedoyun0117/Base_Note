using System;
using System.Collections.Generic;
using GameName.Core.Clues;

namespace GameName.Core.Dialogue
{
    // 대사 한 줄의 저작 데이터.
    //
    // Core에 문자열이 들어온 것은 이 타입부터다. 그래도 규칙은 깨지지 않는다 —
    // 금지된 것은 "코드에 적힌 대사"이지 "데이터로 흘러 들어온 대사"가 아니고,
    // 이 값들은 전부 ScriptableObject에서 변환되어 온다. 검증기가 저작 시점에
    // 대사를 읽으려면 대사가 Core 타입으로 한 번은 들어와야 한다.
    //
    // Speaker를 표시 이름 그대로 두는 것도 같은 이유다. 화자 식별자를 따로 만들면
    // 이름표 하나를 위해 또 다른 대응표가 필요해지는데, 그 표가 얻어 주는 것이
    // 지금은 없다.
    //
    // 대부분의 줄은 TextChoice다 — 걸러진 텍스트 선택지에서 하나를 고른다.
    // 일부 줄만 ClueSelection이다 — 들고 있는 단서로 답하고, 정답이면 CorrectNext,
    // 아니면 IncorrectNext로 간다. 기본 생성자는 TextChoice를 만들고, ClueSelection은
    // 별도 팩토리로 만들어 어느 필드가 유효한지 헷갈리지 않게 한다.
    public sealed class DialogueLineDefinition
    {
        public DialogueLineId Id { get; }
        public string Speaker { get; }

        // 검열 토큰이 섞인 원문. 파싱은 이 타입의 일이 아니다.
        public string AuthoredText { get; }

        public DialoguePromptKind PromptKind { get; }

        // ── TextChoice일 때 ──
        public IReadOnlyList<ChoiceDefinition> Choices { get; }

        // ── ClueSelection일 때 ──
        // 이 중 하나면 정답. 여러 개 가능(학생증·교복 둘 다 정답 등).
        public IReadOnlyList<ClueId> RequiredClueIds { get; }
        public DialogueLineId? CorrectNext { get; }

        // 오답 서브체인의 첫 줄. 그 서브체인은 기존 Choice.Next / (또는 서브체인
        // 줄이 다시 ClueSelection이 아니면 계속하기 선택지) 체인으로 이어지고,
        // 마지막 줄의 Next가 원래 메인 줄기 라인을 가리키게 저작한다.
        public DialogueLineId? IncorrectNext { get; }

        public DialogueLineDefinition(
            DialogueLineId id,
            string speaker,
            string authoredText,
            IReadOnlyList<ChoiceDefinition> choices)
        {
            Id = id;
            Speaker = speaker ?? throw new ArgumentNullException(nameof(speaker));
            AuthoredText = authoredText ?? throw new ArgumentNullException(nameof(authoredText));
            Choices = choices ?? throw new ArgumentNullException(nameof(choices));
            PromptKind = DialoguePromptKind.TextChoice;
            RequiredClueIds = Array.Empty<ClueId>();
        }

        private DialogueLineDefinition(
            DialogueLineId id,
            string speaker,
            string authoredText,
            IReadOnlyList<ClueId> requiredClueIds,
            DialogueLineId? correctNext,
            DialogueLineId? incorrectNext)
        {
            Id = id;
            Speaker = speaker ?? throw new ArgumentNullException(nameof(speaker));
            AuthoredText = authoredText ?? throw new ArgumentNullException(nameof(authoredText));
            Choices = Array.Empty<ChoiceDefinition>();
            PromptKind = DialoguePromptKind.ClueSelection;
            RequiredClueIds = requiredClueIds ?? throw new ArgumentNullException(nameof(requiredClueIds));
            CorrectNext = correctNext;
            IncorrectNext = incorrectNext;
        }

        // 분기를 nullable로 받는 이유: 저작 도구(SO)에서 종류만 ClueSelection으로
        // 골라 두고 분기 id를 아직 안 채운 껍데기 라인이 생길 수 있고, 그것이
        // ToDefinition()에서 예외로 터지는 대신 ClueSelectionLineRule의 검사에
        // 걸려야 하기 때문이다. 다 채워 넘기면 non-null이 그대로 실린다.
        public static DialogueLineDefinition ClueSelection(
            DialogueLineId id,
            string speaker,
            string authoredText,
            IReadOnlyList<ClueId> requiredClueIds,
            DialogueLineId? correctNext,
            DialogueLineId? incorrectNext) =>
            new DialogueLineDefinition(id, speaker, authoredText, requiredClueIds, correctNext, incorrectNext);
    }
}
