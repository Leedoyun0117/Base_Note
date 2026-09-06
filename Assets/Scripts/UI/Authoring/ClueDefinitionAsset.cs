using System.Collections.Generic;
using GameName.Core.Clues;
using GameName.Core.Memories;
using UnityEngine;

namespace GameName.UI.Authoring
{
    // 단서 하나의 저작 데이터를 에셋으로 들고 있는 자리.
    //
    // 단서를 방 에셋 안의 배열이 아니라 각자의 에셋으로 두는 이유: 대사와
    // 선택지가 단서를 식별자로 가리키기 때문에, 단서가 방 데이터 안에만 있으면
    // 그 참조를 인스펙터에서 확인할 방법이 없다.
    [CreateAssetMenu(menuName = "GameName/Clue Definition", fileName = "ClueDefinition")]
    public sealed class ClueDefinitionAsset : ScriptableObject
    {
        [SerializeField] private string _clueId;

        // 플레이어에게 보이는 이름. Core의 ClueDefinition.DisplayName으로 넘어가
        // ClueInfo까지 실린다 — 방에 들어서면 그냥 보이는 사실이라 Kind와 같은
        // 층위로 다룬다.
        [SerializeField] private string _displayName;

        [SerializeField] private ClueKind _kind = ClueKind.FloorObject;

        [Range(0f, 1f)]
        [SerializeField] private float _positionRatio = 0.5f;

        [Header("감춰진 것")]
        [SerializeField] private MemoryColor _hiddenColor = MemoryColor.Red;

        // 이 단서가 무엇에 대한 것인지 나타내는 저작 태그(예: "Yuki.toy").
        // 검열 해금·ClueSelection 정답 판정이 실제로 보는 값이다 — 색은 힌트일
        // 뿐이고 이 배열이 진짜 기준이다.
        [SerializeField] private string[] _tags = System.Array.Empty<string>();

        public string DisplayName => _displayName;

        public ClueDefinition ToDefinition()
        {
            var tags = new List<ClueTag>(_tags?.Length ?? 0);
            if (_tags != null)
            {
                foreach (var tag in _tags)
                {
                    // 인스펙터에서 비어 있는 칸 하나 때문에 단서 전체를 못 읽게
                    // 만들지 않는다.
                    if (!string.IsNullOrWhiteSpace(tag))
                        tags.Add(new ClueTag(tag));
                }
            }

            return new ClueDefinition(
                new ClueId(_clueId),
                _kind,
                _displayName ?? string.Empty,
                new CluePositionRatio(_positionRatio),
                _hiddenColor,
                tags);
        }
    }
}
