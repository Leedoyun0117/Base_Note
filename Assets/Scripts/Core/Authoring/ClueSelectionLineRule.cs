using System.Collections.Generic;
using GameName.Core.Clues;
using GameName.Core.Dialogue;

namespace GameName.Core.Authoring
{
    // ClueSelection 줄의 저작이 성립하는지 본다.
    //
    // 이 줄은 텍스트 선택지 대신 "가진 단서로 답하라"이고, 정답 판정을
    // RequiredTags에 기댄다. 그것이 비어 있거나 그 방 단서 중 어느 것도 갖지
    // 않은 태그를 가리키면 어떤 단서를 골라도 스치지도 못해 플레이어가 이유를
    // 알 수 없이 막힌다 — 저작 시점이 아니면 잡을 곳이 없다.
    //
    // 태그가 중심축(감정)과 곁축(장소·시간)으로 갈리면서 검사가 하나 늘었다:
    // 질문이 중심축 태그를 요구하는데 그 태그를 중심축으로 가진 단서가 방에
    // 하나도 없으면, 어떤 단서를 내도 곁으로만 스칠 뿐 완전적합 정답이 원천
    // 봉쇄된다. 곁축만 걸리는 답으로는 등급 판정기가 부분 적합까지만 주므로
    // (ITagMatchGrader), 좋은 분기로 갈 길이 애초에 없는 방을 저작해 두고도
    // 플레이 중엔 티가 안 난다 — 그래서 저작 시점에 막는다.
    //
    // 방 단위로 보는 이유: 손에 든 단서는 다음 방으로 넘어갈 때 전부 버려지므로
    // (RoomEntryInventoryClear), 방 i의 이 줄은 방 i에서 주운 단서로만 답할 수
    // 있다. 앞 방 단서 태그를 요구하면 그 방에선 낼 수 없는 답이다.
    //
    // CorrectNext/IncorrectNext가 실재하는 라인인지는 NextLineExistsRule이 본다.
    // 분기가 비어 있으면(null) 그 답으로 대화가 끝나는 것으로 본다 — 방의
    // 마지막 줄이 그렇게 저작된다.
    public sealed class ClueSelectionLineRule : IDialogueScriptRule
    {
        public IReadOnlyList<ScriptIssue> Check(RunDefinition run)
        {
            var issues = new List<ScriptIssue>();

            foreach (var room in run.Rooms)
            {
                // 이 방 단서가 가진 태그 — 축을 가리지 않은 전체(스치기라도
                // 하는지)와, 중심축으로만 가진 것(완전적합이 가능한지)을 따로 든다.
                var roomTagValues = new HashSet<string>();
                var roomCenterTagValues = new HashSet<string>();
                foreach (var clue in room.Clues)
                foreach (var tag in clue.Tags)
                {
                    roomTagValues.Add(tag.Value);
                    if (tag.Axis == ClueTagAxis.Center)
                        roomCenterTagValues.Add(tag.Value);
                }

                foreach (var line in room.EnumerateAuthoredLines())
                {
                    if (line.PromptKind != DialoguePromptKind.ClueSelection)
                        continue;

                    if (line.RequiredTags.Count == 0)
                    {
                        issues.Add(new ScriptIssue(
                            ScriptIssueSeverity.Error,
                            $"방 {room.Id}의 대사 {line.Id}는 단서로 답하는 줄인데 정답 태그(RequiredTags)가 하나도 없다."));
                        continue;
                    }

                    var hasCenterRequirement = false;
                    var someCenterAnswerable = false;

                    foreach (var required in line.RequiredTags)
                    {
                        if (!roomTagValues.Contains(required.Value))
                        {
                            issues.Add(new ScriptIssue(
                                ScriptIssueSeverity.Error,
                                $"방 {room.Id}의 대사 {line.Id}가 정답 태그로 {required}를 요구하지만, " +
                                "이 방 단서 중 이 태그를 가진 것이 없어 스치는 답조차 낼 수 없다."));
                        }

                        if (required.Axis != ClueTagAxis.Center)
                            continue;

                        hasCenterRequirement = true;
                        if (roomCenterTagValues.Contains(required.Value))
                            someCenterAnswerable = true;
                    }

                    if (hasCenterRequirement && !someCenterAnswerable)
                    {
                        issues.Add(new ScriptIssue(
                            ScriptIssueSeverity.Error,
                            $"방 {room.Id}의 대사 {line.Id}가 중심축 정답 태그를 요구하지만, " +
                            "이 방 단서 중 그 태그를 중심축으로 가진 것이 없어 완전적합 정답을 낼 방법이 없다."));
                    }
                }
            }

            return issues;
        }
    }
}
