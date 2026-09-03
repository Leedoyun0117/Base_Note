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
    [CreateAssetMenu(menuName = "GameName/Dialogue Line", fileName = "DialogueLine")]
    public sealed class DialogueLineDefinitionAsset : ScriptableObject
    {
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

        [SerializeField] private List<ChoiceEntry> _choices = new List<ChoiceEntry>();

        public DialogueLineDefinition ToDefinition()
        {
            var choices = new List<ChoiceDefinition>(_choices.Count);
            foreach (var choice in _choices)
                choices.Add(choice.ToDefinition());

            return new DialogueLineDefinition(new DialogueLineId(_lineId), _speaker, _authoredText, choices);
        }
    }
}
