using System;
using System.Collections.Generic;
using GameName.Core.Dialogue;

namespace GameName.Core.Authoring
{
    // 방 대화의 한 자리에 끼는 랜덤 분기 풀 — 후보 라인 여럿 중 런 시작 시
    // 시드로 하나가 확정되고, 그 런 안에서는 바뀌지 않는다.
    //
    // 후보들은 전부 같은 DialogueLineId(= 슬롯 id)를 쓴다. 백본 라인은 그
    // 슬롯 id로 Next를 걸고, BranchResolver가 뽑힌 후보를 그 자리에 꽂는다.
    // 어느 후보가 뽑혔는지 백본이 몰라도 되도록 하는 규약이며, 후보들이 같은
    // id를 공유하는지는 DialogueScriptValidator가 저작 시점에 확인한다.
    //
    // 가중치는 없다 — 기본 균등 추첨이다. 후보가 하나뿐인 풀은 분기가 아니므로
    // 검증기가 오류로 잡는다.
    public sealed class BranchPool
    {
        public IReadOnlyList<DialogueLineDefinition> Candidates { get; }

        public BranchPool(IReadOnlyList<DialogueLineDefinition> candidates)
        {
            Candidates = candidates ?? throw new ArgumentNullException(nameof(candidates));
        }

        // 이 풀을 가리키는 슬롯 id. 후보가 없으면 성립하지 않는다(검증기가 잡는다).
        public DialogueLineId SlotLineId => Candidates[0].Id;
    }
}
