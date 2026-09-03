using System.Collections.Generic;
using GameName.Core.Dialogue;
using GameName.Core.MemoryRooms;

namespace GameName.Core.Authoring
{
    // 참조된 대사가 실제로 있는지 본다. 시작 대사와 선택지의 Next 양쪽 모두.
    //
    // 없는 대사를 가리키는 선택지는 실행 시점에는 "대화가 그냥 끝났다"와 구분되지
    // 않는다. 그래서 저작 시점에 잡지 않으면 대사 하나를 지웠을 때 어디가 끊겼는지
    // 아무도 모르는 채로 남는다.
    //
    // 같은 방 안에서만 찾는 이유: 방을 넘나드는 대사 참조는 기획에 없다. 방을
    // 옮기면 대화 자체가 새로 시작되므로, 다른 방 대사를 가리켰다면 그것은 의도가
    // 아니라 복사 실수다.
    //
    // 분기 풀도 함께 본다. 백본 라인은 풀의 슬롯 id로 Next를 걸고, 풀 후보의
    // 선택지는 다시 백본이나 다른 슬롯을 가리킨다 — 그래서 슬롯 id도 "있는 대사"에
    // 포함시키고, 후보 라인의 참조도 같은 기준으로 검사한다.
    public sealed class NextLineExistsRule : IDialogueScriptRule
    {
        public IReadOnlyList<ScriptIssue> Check(RunDefinition run)
        {
            var issues = new List<ScriptIssue>();

            foreach (var room in run.Rooms)
            {
                var known = new HashSet<DialogueLineId>();
                foreach (var line in room.DialogueLines)
                    known.Add(line.Id);
                foreach (var pool in room.BranchPools)
                {
                    if (pool.Candidates.Count > 0)
                        known.Add(pool.Candidates[0].Id);
                }

                if (room.StartLineId.HasValue && !known.Contains(room.StartLineId.Value))
                {
                    issues.Add(new ScriptIssue(
                        ScriptIssueSeverity.Error,
                        $"방 {room.Id}의 시작 대사 {room.StartLineId.Value}가 그 방에 없다."));
                }

                foreach (var line in room.DialogueLines)
                    CheckLine(issues, known, room.Id, line);

                foreach (var pool in room.BranchPools)
                foreach (var candidate in pool.Candidates)
                    CheckLine(issues, known, room.Id, candidate);
            }

            return issues;
        }

        private static void CheckLine(
            ICollection<ScriptIssue> issues,
            HashSet<DialogueLineId> known,
            MemoryRoomId roomId,
            DialogueLineDefinition line)
        {
            foreach (var choice in line.Choices)
            {
                if (!choice.Next.HasValue || known.Contains(choice.Next.Value))
                    continue;

                issues.Add(new ScriptIssue(
                    ScriptIssueSeverity.Error,
                    $"방 {roomId}의 대사 {line.Id}에 달린 선택지 {choice.Id}가 " +
                    $"없는 대사 {choice.Next.Value}를 가리킨다."));
            }

            // ClueSelection 줄의 정답/오답 분기도 같은 방에 실재해야 한다.
            CheckBranch(issues, known, roomId, line.Id, "정답 분기(CorrectNext)", line.CorrectNext);
            CheckBranch(issues, known, roomId, line.Id, "오답 분기(IncorrectNext)", line.IncorrectNext);
        }

        private static void CheckBranch(
            ICollection<ScriptIssue> issues,
            HashSet<DialogueLineId> known,
            MemoryRoomId roomId,
            DialogueLineId lineId,
            string label,
            DialogueLineId? target)
        {
            if (!target.HasValue || known.Contains(target.Value))
                return;

            issues.Add(new ScriptIssue(
                ScriptIssueSeverity.Error,
                $"방 {roomId}의 대사 {lineId}의 {label}가 없는 대사 {target.Value}를 가리킨다."));
        }
    }
}
