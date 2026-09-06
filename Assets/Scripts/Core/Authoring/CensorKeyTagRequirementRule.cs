using System;
using System.Collections.Generic;
using System.Linq;
using GameName.Core.Clues;
using GameName.Core.Dialogue;
using GameName.Core.MemoryRooms;

namespace GameName.Core.Authoring
{
    // 대사에 걸린 검열이 실제로 풀릴 수 있는지 본다.
    //
    // 이 규칙이 가장 중요한 이유: 풀 수 없는 검열은 화면에 아무 문제 없이
    // 보인다. 플레이어는 그 방까지의 단서를 전부 추출해 보고 나서야 그 말이
    // 영영 열리지 않는다는 것을 알게 되는데, 그때는 이미 되돌릴 수 없다. 다른
    // 규칙들은 플레이 중에 티라도 나지만 이것은 저작 시점이 아니면 잡을 곳이 없다.
    //
    // 판정 축이 색이 아니라 태그인 이유: 런타임(CensorUnlockProcessor)이 제시한
    // 기억의 태그가 그 키의 요구 태그와 하나라도 겹칠 때만 검열을 푼다. 색은
    // 추출 순간 드러나는 힌트일 뿐 정답을 정하지 않는다 — 그래서 이 규칙도
    // 색이 아니라 CensorKeyTagRequirement 목록과 단서 태그를 대조한다.
    //
    // 방 0..i 누적으로 보는 이유: 검열을 푸는 데 쓰는 추출된 기억은 런 전체에
    // 걸쳐 남으므로(ExtractedMemoryStore), 방 i의 대사는 방 0..i 어디서 얻은
    // 단서로도 풀 수 있다. 반대로 뒤 방 단서로 앞 방 대사가 열리는 것은 이미
    // 지나온 대사를 다시 읽으러 돌아가야 한다는 뜻이라 기획에 없다 — 그래서
    // 그 방까지(포함)의 누적만 인정한다.
    public sealed class CensorKeyTagRequirementRule : IDialogueScriptRule
    {
        private readonly ICensorTokenIndexSource _tokens;

        public CensorKeyTagRequirementRule(ICensorTokenIndexSource tokens)
        {
            _tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
        }

        public IReadOnlyList<ScriptIssue> Check(RunDefinition run)
        {
            var issues = new List<ScriptIssue>();

            // 방 i까지(포함) 얻을 수 있는 단서 태그의 누적 합집합.
            var roomIndex = new Dictionary<MemoryRoomId, int>();
            var tagsUpToRoom = new List<HashSet<ClueTag>>();
            var running = new HashSet<ClueTag>();
            for (var i = 0; i < run.Rooms.Count; i++)
            {
                var room = run.Rooms[i];
                roomIndex[room.Id] = i;
                foreach (var clue in room.Clues)
                foreach (var tag in clue.Tags)
                    running.Add(tag);
                tagsUpToRoom.Add(new HashSet<ClueTag>(running));
            }

            // 요구 태그 맵(첫 항목 우선 — CensorKeyRequiredTagMap과 같은 관례) +
            // 같은 키를 여러 번 저작한 실수는 경고로.
            var requiredByKey = new Dictionary<CensorKey, IReadOnlyList<ClueTag>>();
            var duplicateReported = new HashSet<CensorKey>();
            foreach (var requirement in run.CensorKeyTagRequirements)
            {
                if (requiredByKey.ContainsKey(requirement.Key))
                {
                    if (duplicateReported.Add(requirement.Key))
                        issues.Add(new ScriptIssue(
                            ScriptIssueSeverity.Warning,
                            $"검열 키 {requirement.Key}의 요구 태그가 여러 번 저작됐다. " +
                            "첫 번째 목록만 쓰인다."));
                    continue;
                }

                requiredByKey[requirement.Key] = requirement.RequiredTags;
            }

            // 실제로 대사·선택지에 쓰인 키만 본다. 키마다 "가장 먼저 등장한 방"을
            // 기준으로 한 번씩만 판정·보고한다 — 같은 키가 여러 줄에 걸쳐도 문제는
            // 하나이고, 가장 이른 방에서 풀 수 있으면 뒤 방에서는 당연히 풀린다.
            var earliest = new Dictionary<CensorKey, (int RoomIdx, string Location, MemoryRoomId RoomId)>();
            foreach (var use in _tokens.For(run).Uses)
            {
                var idx = roomIndex.TryGetValue(use.RoomId, out var ri) ? ri : int.MaxValue;
                if (!earliest.TryGetValue(use.Key, out var current) || idx < current.RoomIdx)
                    earliest[use.Key] = (idx, use.Location, use.RoomId);
            }

            foreach (var entry in earliest.OrderBy(e => e.Value.RoomIdx).ThenBy(e => e.Key.Value))
            {
                var key = entry.Key;
                var (roomIdx, location, roomId) = entry.Value;

                if (!requiredByKey.TryGetValue(key, out var required))
                {
                    issues.Add(new ScriptIssue(
                        ScriptIssueSeverity.Error,
                        $"{location}가 검열 키 {key}를 쓰지만, 이 키를 푸는 데 필요한 단서 " +
                        "태그(CensorKeyTagRequirement)가 저작되지 않아 런타임에서 영영 풀리지 않는다."));
                    continue;
                }

                if (required.Count == 0)
                {
                    issues.Add(new ScriptIssue(
                        ScriptIssueSeverity.Error,
                        $"검열 키 {key}의 요구 태그 목록이 비어 있어 어떤 기억을 제시해도 풀리지 않는다."));
                    continue;
                }

                var reachable = roomIdx >= 0 && roomIdx < tagsUpToRoom.Count
                    ? tagsUpToRoom[roomIdx]
                    : running;

                if (!required.Any(reachable.Contains))
                {
                    issues.Add(new ScriptIssue(
                        ScriptIssueSeverity.Error,
                        $"{location}가 검열 키 {key}를 쓰지만, 방 {roomId}까지 얻을 수 있는 어떤 " +
                        "단서 태그도 이 키의 요구 태그와 겹치지 않아 그 방에서는 풀 수 없다."));
                }
            }

            return issues;
        }
    }
}
