using System.Collections.Generic;
using GameName.Core.Clues;
using GameName.Core.Complexes;
using UnityEngine;

namespace GameName.UI.Authoring
{
    // 단서 하나의 저작 데이터를 에셋으로 들고 있는 자리.
    //
    // 단서를 라운드 에셋 안의 배열이 아니라 각자의 에셋으로 두는 이유: 컴플렉스
    // 풀이나 다른 데이터가 단서를 식별자로 가리킬 수 있어야 하기 때문이다.
    [CreateAssetMenu(menuName = "GameName/Clue Definition", fileName = "ClueDefinition")]
    public sealed class ClueDefinitionAsset : ScriptableObject
    {
        [SerializeField] private string _clueId;
        [SerializeField] private string _displayName;
        [SerializeField] private ClueKind _kind = ClueKind.FloorObject;

        [Range(0f, 1f)]
        [SerializeField] private float _positionRatio = 0.5f;

        [Header("서사")]
        [TextArea(2, 6)]
        [SerializeField] private string _story;

        [Header("태그 (인물 × 감정 × 시간대)")]
        [SerializeField] private string[] _personTags = System.Array.Empty<string>();
        [SerializeField] private string[] _emotionTags = System.Array.Empty<string>();
        [SerializeField] private string[] _timeTags = System.Array.Empty<string>();

        public string DisplayName => _displayName;

        public ClueDefinition ToDefinition()
        {
            var tags = new List<StoryTag>();
            Append(tags, _personTags, StoryTagAxis.Person);
            Append(tags, _emotionTags, StoryTagAxis.Emotion);
            Append(tags, _timeTags, StoryTagAxis.Time);

            return new ClueDefinition(
                new ClueId(_clueId),
                _kind,
                _displayName ?? string.Empty,
                new CluePositionRatio(_positionRatio),
                _story ?? string.Empty,
                tags);
        }

        private static void Append(List<StoryTag> into, string[] raw, StoryTagAxis axis)
        {
            if (raw == null) return;
            foreach (var value in raw)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    into.Add(new StoryTag(axis, value));
            }
        }
    }
}
