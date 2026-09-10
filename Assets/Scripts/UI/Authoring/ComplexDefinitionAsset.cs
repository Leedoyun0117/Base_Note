using System;
using System.Collections.Generic;
using GameName.Core.Complexes;
using UnityEngine;

namespace GameName.UI.Authoring
{
    // 컴플렉스 하나의 저작 데이터를 에셋으로 들고 있는 자리.
    //
    // ClueDefinitionAsset과 같은 자리에 있다 — 라운드 에셋(RoomDefinitionAsset)은
    // 컴플렉스를 식별자(_startingComplexId, _complexPoolIds)로만 가리키고, 그
    // 식별자가 무엇을 뜻하는지(이름·설명·규칙)는 이 에셋이 정의한다.
    //
    // 다른 *Asset과 마찬가지로 지금은 검증·에디터 전용이다 — 런타임은
    // DemoGameData가 만든 정의를 쓴다. 실제 콘텐츠 소스가 이 에셋으로 옮겨 오면
    // ToDefinition()만 태워 GameSession 이하를 그대로 쓸 수 있다.
    [CreateAssetMenu(menuName = "GameName/Complex Definition", fileName = "ComplexDefinition")]
    public sealed class ComplexDefinitionAsset : ScriptableObject
    {
        [SerializeField] private string _complexId;
        [SerializeField] private string _displayName;

        // 화면에 그대로 나가는 관찰형 서술. 결과를 플레이어가 추론하게 두는
        // 문장이라 공략 지시는 쓰지 않는다.
        [Header("설명")]
        [TextArea(2, 4)]
        [SerializeField] private string _description;

        [Header("규칙")]
        [Min(0)]
        [SerializeField] private int _priority;

        [Min(1)]
        [SerializeField] private int _durationTurns = 3;

        [SerializeField] private ComplexKind _kind = ComplexKind.Transform;
        [SerializeField] private List<RuleEntry> _rules = new List<RuleEntry>();

        public string DisplayName => _displayName;
        public string Description => _description;

        public ComplexDefinition ToDefinition()
        {
            var rules = new List<TagTransformRule>(_rules.Count);
            foreach (var entry in _rules)
            {
                if (entry != null)
                    rules.Add(entry.ToRule());
            }

            return new ComplexDefinition(
                new ComplexId(_complexId), _priority, _durationTurns, _kind, rules,
                _displayName, _description);
        }

        // 인스펙터에서 한 줄씩 적는 규칙. TagPattern·StoryTag는 값 타입이라
        // 직렬화가 안 되므로 축·값을 풀어서 담고 ToRule()에서 되조립한다.
        // 조건 값이 비어 있으면 그 축 전체 와일드카드, 결과 값이 비어 있으면
        // 결과 없음(삭제·거부형).
        [Serializable]
        public sealed class RuleEntry
        {
            [SerializeField] private StoryTagAxis _matchAxis;
            [SerializeField] private string _matchValue;
            [SerializeField] private ComplexKind _kind = ComplexKind.Transform;
            [SerializeField] private StoryTagAxis _resultAxis;
            [SerializeField] private string _resultValue;

            public TagTransformRule ToRule()
            {
                var pattern = new TagPattern(_matchAxis, _matchValue);
                StoryTag? result = string.IsNullOrWhiteSpace(_resultValue)
                    ? (StoryTag?)null
                    : new StoryTag(_resultAxis, _resultValue);

                return new TagTransformRule(pattern, _kind, result);
            }
        }
    }
}
