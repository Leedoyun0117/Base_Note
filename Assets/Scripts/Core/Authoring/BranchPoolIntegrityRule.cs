using System.Collections.Generic;
using GameName.Core.Dialogue;

namespace GameName.Core.Authoring
{
    // 분기 풀의 구조가 성립하는지 본다.
    //
    // 백본은 풀의 슬롯 id로 Next를 걸고, BranchResolver는 뽑힌 후보를 그 id
    // 자리에 꽂는다. 이 규약이 깨지면 — 후보들이 서로 다른 id를 쓰거나, 슬롯
    // id가 고정 라인이나 다른 풀과 겹치면 — 확정본에서 라인 하나가 다른 라인을
    // 덮어써서 대화가 엉뚱한 데로 튀거나 그냥 끊긴다. 실행 시점에는 "왜 이 방만
    // 이상하지"로만 보이므로 저작 시점이 아니면 잡을 곳이 없다.
    //
    // 후보가 하나뿐인 풀도 여기서 오류로 잡는다 — 뽑을 것이 없으면 분기가 아니다.
    public sealed class BranchPoolIntegrityRule : IDialogueScriptRule
    {
        public IReadOnlyList<ScriptIssue> Check(RunDefinition run)
        {
            var issues = new List<ScriptIssue>();

            foreach (var room in run.Rooms)
            {
                var fixedIds = new HashSet<DialogueLineId>();
                foreach (var line in room.DialogueLines)
                    fixedIds.Add(line.Id);

                var slotIds = new HashSet<DialogueLineId>();

                for (var p = 0; p < room.BranchPools.Count; p++)
                {
                    var pool = room.BranchPools[p];

                    if (pool.Candidates.Count == 0)
                    {
                        issues.Add(new ScriptIssue(
                            ScriptIssueSeverity.Error,
                            $"방 {room.Id}의 {p + 1}번째 분기 풀에 후보가 하나도 없다."));
                        continue;
                    }

                    if (pool.Candidates.Count == 1)
                    {
                        issues.Add(new ScriptIssue(
                            ScriptIssueSeverity.Error,
                            $"방 {room.Id}의 분기 풀 {pool.Candidates[0].Id}에 후보가 하나뿐이라 " +
                            "분기할 것이 없다."));
                    }

                    var slotId = pool.Candidates[0].Id;

                    var allShare = true;
                    foreach (var candidate in pool.Candidates)
                    {
                        if (candidate.Id.Equals(slotId))
                            continue;

                        allShare = false;
                        break;
                    }

                    if (!allShare)
                    {
                        issues.Add(new ScriptIssue(
                            ScriptIssueSeverity.Error,
                            $"방 {room.Id}의 분기 풀 {slotId}의 후보들이 서로 다른 대사 id를 쓴다 — " +
                            "한 풀의 후보는 모두 같은 슬롯 id를 공유해야 한다."));
                    }

                    if (fixedIds.Contains(slotId))
                    {
                        issues.Add(new ScriptIssue(
                            ScriptIssueSeverity.Error,
                            $"방 {room.Id}의 분기 풀 슬롯 id {slotId}가 같은 방의 고정 대사 id와 겹친다."));
                    }

                    if (!slotIds.Add(slotId))
                    {
                        issues.Add(new ScriptIssue(
                            ScriptIssueSeverity.Error,
                            $"방 {room.Id}에 슬롯 id {slotId}를 쓰는 분기 풀이 둘 이상이다."));
                    }
                }
            }

            return issues;
        }
    }
}
