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

        public string DisplayName => _displayName;

        public ClueDefinition ToDefinition() =>
            new ClueDefinition(
                new ClueId(_clueId),
                _kind,
                _displayName ?? string.Empty,
                new CluePositionRatio(_positionRatio),
                _hiddenColor);
    }
}
