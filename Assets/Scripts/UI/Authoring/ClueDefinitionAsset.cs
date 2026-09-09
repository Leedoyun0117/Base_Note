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

        // 이 단서의 중심축 태그(예: "Yuki.toy") — 그 단서가 "정말로 무엇에
        // 대한 것인가". 검열 해금·ClueSelection 정답 판정이 보는 값이고, 질문의
        // 중심축과 맞을 때만 완전적합 정답이 된다. 색은 힌트일 뿐이다.
        [SerializeField] private string[] _tags = System.Array.Empty<string>();

        // 곁축 태그(장소·시간 등). 중심축은 어긋났지만 스치기는 한 답에 부분
        // 점수를 주는 근거다 — 등급 판정기만 축을 구분해서 본다.
        [SerializeField] private string[] _subTags = System.Array.Empty<string>();

        public string DisplayName => _displayName;

        public ClueDefinition ToDefinition()
        {
            var tags = new List<ClueTag>((_tags?.Length ?? 0) + (_subTags?.Length ?? 0));
            AppendTags(tags, _tags, ClueTagAxis.Center);
            AppendTags(tags, _subTags, ClueTagAxis.Sub);

            return new ClueDefinition(
                new ClueId(_clueId),
                _kind,
                _displayName ?? string.Empty,
                new CluePositionRatio(_positionRatio),
                _hiddenColor,
                tags);
        }

        // 인스펙터에서 비어 있는 칸 하나 때문에 단서 전체를 못 읽게 만들지 않는다.
        private static void AppendTags(List<ClueTag> into, string[] raw, ClueTagAxis axis)
        {
            if (raw == null) return;
            foreach (var tag in raw)
            {
                if (!string.IsNullOrWhiteSpace(tag))
                    into.Add(new ClueTag(tag, axis));
            }
        }
    }
}
