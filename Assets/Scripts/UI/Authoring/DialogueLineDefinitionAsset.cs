using System;
using System.Collections.Generic;
using GameName.Core.Clues;
using GameName.Core.Dialogue;
using UnityEngine;

namespace GameName.UI.Authoring
{
    // 대사 한 줄의 저작 데이터를 에셋으로 들고 있는 자리.
    //
    // 선택지는 별도 에셋이 아니라 이 안의 배열이다. 선택지는 자기가 달린 대사를
    // 떠나 존재할 수 없고 다른 대사가 공유하지도 않으므로, 에셋으로 쪼개면 파일만
    // 몇 배로 늘고 얻는 것이 없다.
    //
    // 줄의 종류는 _lineKind가 정한다 — TextChoice면 걸러진 텍스트 선택지에서
    // 하나를 고르고, ClueSelection이면 가진 단서로 답한다. Unity 직렬화가 이런
    // 합타입을 표현하지 못해, 고르지 않은 쪽 칸은 그냥 무시된다(아래 ChoiceEntry의
    // _conditionKind와 같은 방식).
    [CreateAssetMenu(menuName = "GameName/Dialogue Line", fileName = "DialogueLine")]
    public sealed class DialogueLineDefinitionAsset : ScriptableObject
    {
        public enum LineKind
        {
            // 지금까지의 방식: 걸러진 텍스트 선택지 목록에서 하나를 고른다.
            TextChoice,

            // "이 질문엔 가진 단서로 답하라": 들고 있는 단서로 답해 정답이면
            // CorrectNext, 아니면 IncorrectNext(오답 서브체인)로 간다. 이 종류의
            // 줄에는 텍스트 선택지를 두지 않는다.
            ClueSelection
        }

        // 인스펙터에서 한 줄로 접히지 않도록 조건 값들을 나란히 둔다. 어떤 값이
        // 쓰이는지는 _conditionKind가 정한다 — Unity 직렬화가 이런 합타입을
        // 표현하지 못해, 쓰이지 않는 칸은 그냥 무시된다.
        [Serializable]
        private sealed class ChoiceEntry
        {
            [SerializeField] private string _choiceId;

            [TextArea(1, 3)]
            [SerializeField] private string _authoredText;

            [SerializeField] private bool _isCorrect;

            // 비워 두면 이 선택지에서 대화가 끝난다.
            [SerializeField] private string _nextLineId;

            [Header("표시 조건")]
            [SerializeField] private ChoiceConditionKind _conditionKind = ChoiceConditionKind.None;
            // 대사 원문의 토큰에 적은 키를 그대로 적는다.
            [SerializeField] private string _requiredCensorKey;
            [SerializeField] private string _requiredClueId;

            public ChoiceDefinition ToDefinition() =>
                new ChoiceDefinition(
                    new ChoiceId(_choiceId),
                    _authoredText,
                    _isCorrect,
                    AuthoredIds.OptionalLine(_nextLineId),
                    ToCondition());

            private ChoiceCondition ToCondition()
            {
                switch (_conditionKind)
                {
                    case ChoiceConditionKind.CensorKeyRevealed:
                        return ChoiceCondition.RequiresCensorKeyRevealed(new CensorKey(_requiredCensorKey));
                    case ChoiceConditionKind.ClueUsed:
                        return ChoiceCondition.ClueUsed(new ClueId(_requiredClueId));
                    default:
                        return ChoiceCondition.None;
                }
            }
        }

        [SerializeField] private string _lineId;
        [SerializeField] private string _speaker;

        // 검열 토큰이 섞인 원문. 토큰은 [[색:키:가려진 말]] 순서다.
        // 예: 우리가 [[B:beach-house:해변의 작은 집]]에서 보냈던 시절이 그리워.
        //
        // 키는 "무엇이 가려져 있는가"라서, 같은 사실을 가리키는 구간은 다른 대사에
        // 있어도 같은 키를 적어야 함께 풀린다.
        [TextArea(3, 10)]
        [SerializeField] private string _authoredText;

        [SerializeField] private LineKind _lineKind = LineKind.TextChoice;

        [Header("텍스트 선택지 (LineKind = TextChoice)")]
        [SerializeField] private List<ChoiceEntry> _choices = new List<ChoiceEntry>();

        [Header("단서로 답하기 (LineKind = ClueSelection)")]
        // 이 중 하나면 정답. 여러 개 가능(학생증·교복 둘 다 정답 등).
        [SerializeField] private List<string> _requiredClueIds = new List<string>();
        // 정답 단서를 냈을 때 가는 줄, 오답이거나 넘겼을 때 가는 줄(오답 서브체인의
        // 첫 줄). 아직 안 채웠으면 비워 둔다 — 검증기가 껍데기 줄을 잡는다.
        [SerializeField] private string _correctNextLineId;
        [SerializeField] private string _incorrectNextLineId;

        public DialogueLineDefinition ToDefinition()
        {
            var id = new DialogueLineId(_lineId);

            if (_lineKind == LineKind.ClueSelection)
            {
                var required = new List<ClueId>(_requiredClueIds.Count);
                foreach (var clueId in _requiredClueIds)
                {
                    // 인스펙터에서 비어 있는 칸 하나 때문에 줄 전체를 못 읽게 만들지
                    // 않는다. 정답 단서가 하나도 없다는 것 자체는 검증기가 잡는다.
                    if (!string.IsNullOrWhiteSpace(clueId))
                        required.Add(new ClueId(clueId));
                }

                return DialogueLineDefinition.ClueSelection(
                    id, _speaker, _authoredText, required,
                    AuthoredIds.OptionalLine(_correctNextLineId),
                    AuthoredIds.OptionalLine(_incorrectNextLineId));
            }

            var choices = new List<ChoiceDefinition>(_choices.Count);
            foreach (var choice in _choices)
                choices.Add(choice.ToDefinition());

            return new DialogueLineDefinition(id, _speaker, _authoredText, choices);
        }
    }
}
