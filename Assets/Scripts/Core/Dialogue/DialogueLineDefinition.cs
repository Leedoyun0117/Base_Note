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
        // 손에 든 단서가 이 중 하나라도 걸치면 정답. 판정 단위가 ClueId가 아니라
        // ClueTag인 이유: 여러 단서가 같은 사실을 가리킬 수 있고(리본도 편지도
        // "그때 쥐고 있던 것"), 그것을 단서 id를 하나씩 나열하는 대신 태그
        // 하나로 묶으면 새 단서가 늘 때마다 이 줄을 고치지 않아도 된다.
        public IReadOnlyList<ClueTag> RequiredTags { get; }
        public DialogueLineId? CorrectNext { get; }

        // 오답 서브체인의 첫 줄. 그 서브체인은 기존 Choice.Next / (또는 서브체인
        // 줄이 다시 ClueSelection이 아니면 계속하기 선택지) 체인으로 이어지고,
        // 마지막 줄의 Next가 원래 메인 줄기 라인을 가리키게 저작한다.
        public DialogueLineId? IncorrectNext { get; }

        // 이 줄로 들어설 때 나츠의 안정 축을 움직이는 양(음수는 침체, 양수는
        // 흥분 쪽). 유키의 피드백 대사가 나츠의 심리를 미는 것이 이 값이다 —
        // 질문 줄은 0으로 둔다. 등급에 따라 델타가 달라지는 것(같은 피드백 줄도
        // 완전적합으로 왔을 때와 오답으로 왔을 때 다르게)은 [10]에서 얹는다.
        public int StabilityDelta { get; }

        public DialogueLineDefinition(
            DialogueLineId id,
            string speaker,
            string authoredText,
            IReadOnlyList<ChoiceDefinition> choices,
            int stabilityDelta = 0)
        {
            Id = id;
            Speaker = speaker ?? throw new ArgumentNullException(nameof(speaker));
            AuthoredText = authoredText ?? throw new ArgumentNullException(nameof(authoredText));
            Choices = choices ?? throw new ArgumentNullException(nameof(choices));
            PromptKind = DialoguePromptKind.TextChoice;
            RequiredTags = Array.Empty<ClueTag>();
            StabilityDelta = stabilityDelta;
        }

        private DialogueLineDefinition(
            DialogueLineId id,
            string speaker,
            string authoredText,
            IReadOnlyList<ClueTag> requiredTags,
            DialogueLineId? correctNext,
            DialogueLineId? incorrectNext,
            int stabilityDelta)
        {
            Id = id;
            Speaker = speaker ?? throw new ArgumentNullException(nameof(speaker));
            AuthoredText = authoredText ?? throw new ArgumentNullException(nameof(authoredText));
            Choices = Array.Empty<ChoiceDefinition>();
            PromptKind = DialoguePromptKind.ClueSelection;
            RequiredTags = requiredTags ?? throw new ArgumentNullException(nameof(requiredTags));
            CorrectNext = correctNext;
            IncorrectNext = incorrectNext;
            StabilityDelta = stabilityDelta;
        }

        // 분기를 nullable로 받는 이유: 저작 도구(SO)에서 종류만 ClueSelection으로
        // 골라 두고 분기 id를 아직 안 채운 껍데기 라인이 생길 수 있고, 그것이
        // ToDefinition()에서 예외로 터지는 대신 ClueSelectionLineRule의 검사에
        // 걸려야 하기 때문이다. 다 채워 넘기면 non-null이 그대로 실린다.
        public static DialogueLineDefinition ClueSelection(
            DialogueLineId id,
            string speaker,
            string authoredText,
            IReadOnlyList<ClueTag> requiredTags,
            DialogueLineId? correctNext,
            DialogueLineId? incorrectNext,
            int stabilityDelta = 0) =>
            new DialogueLineDefinition(
                id, speaker, authoredText, requiredTags, correctNext, incorrectNext, stabilityDelta);
    }
}
