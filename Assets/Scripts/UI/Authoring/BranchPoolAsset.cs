using System.Collections.Generic;
using GameName.Core.Authoring;
using UnityEngine;

namespace GameName.UI.Authoring
{
    // 랜덤 분기 풀의 저작 데이터를 에셋으로 들고 있는 자리.
    //
    // 후보는 별도 타입이 아니라 기존 DialogueLineDefinitionAsset 그대로다 —
    // 풀에 들어가는 후보도 결국 대사 한 줄이고, 저작·검증 표면을 새로 만들지
    // 않는다. 후보 SO들의 _lineId를 전부 같은 값(= 슬롯 id)으로 적어야 하며,
    // 그 규약은 DialogueScriptValidator가 확인한다.
    //
    // 가중치 필드는 없다. 기본 균등 추첨이고, 필요해지면 그때 별도로 추가한다.
    [CreateAssetMenu(menuName = "GameName/Branch Pool", fileName = "BranchPool")]
    public sealed class BranchPoolAsset : ScriptableObject
    {
        [SerializeField] private List<DialogueLineDefinitionAsset> _candidates =
            new List<DialogueLineDefinitionAsset>();

        public BranchPool ToDefinition()
        {
            var candidates = new List<Core.Dialogue.DialogueLineDefinition>(_candidates.Count);
            foreach (var candidate in _candidates)
            {
                // 비어 있는 칸 하나 때문에 풀 전체를 못 읽게 만들지 않는다 —
                // 후보 수가 규약과 어긋나면 검증기가 그것대로 잡는다.
                if (candidate != null)
                    candidates.Add(candidate.ToDefinition());
            }

            return new BranchPool(candidates);
        }
    }
}
