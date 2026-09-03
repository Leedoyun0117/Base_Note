using System;
using System.Collections.Generic;
using GameName.Core.Memories;
using GameName.Core.MemoryRooms;

namespace GameName.Core.Authoring
{
    // 대사에 걸린 검열이 그 방 안에서 풀릴 수 있는지 본다.
    //
    // 이 규칙이 가장 중요한 이유: 풀 수 없는 검열은 화면에 아무 문제 없이
    // 보인다. 플레이어는 그 방의 단서를 전부 추출해 보고 나서야 그 말이 영영
    // 열리지 않는다는 것을 알게 되는데, 추출 자원은 한 판에 정해져 있어 그때는
    // 이미 되돌릴 수 없다. 다른 규칙들은 플레이 중에 티라도 나지만 이것은
    // 저작 시점이 아니면 잡을 곳이 없다.
    //
    // 선택지 문구도 함께 검사한다 — 선택지 역시 검열될 수 있고, 열 수 없는
    // 검열이 선택지에 박히면 무엇을 고르는지 모른 채 고르게 된다. 그 "어디까지
    // 검사하는가"를 이 규칙이 직접 정하지 않고 CensorTokenIndex에 맡기는 것이
    // 요점이다: 대사가 붙는 자리가 늘어날 때 규칙마다 따로 기억하지 않아도 된다.
    //
    // 방 단위로 보는 이유: 검열을 푸는 단서는 그 방에 놓여 있어야 한다. 다음 방
    // 단서로 이전 방 대사가 열린다면 플레이어는 이미 지나온 대사를 다시 읽으러
    // 돌아가야 하는데, 그런 되돌아감은 기획에 없다.
    public sealed class CensoredColorRevealableRule : IDialogueScriptRule
    {
        private readonly ICensorTokenIndexSource _tokens;

        public CensoredColorRevealableRule(ICensorTokenIndexSource tokens)
        {
            _tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
        }

        public IReadOnlyList<ScriptIssue> Check(RunDefinition run)
        {
            var issues = new List<ScriptIssue>();
            var revealableByRoom = new Dictionary<MemoryRoomId, HashSet<MemoryColor>>();

            foreach (var room in run.Rooms)
            {
                var colors = new HashSet<MemoryColor>();
                foreach (var clue in room.Clues)
                    colors.Add(clue.HiddenColor);

                revealableByRoom[room.Id] = colors;
            }

            foreach (var use in _tokens.For(run).Uses)
            {
                if (revealableByRoom.TryGetValue(use.RoomId, out var revealable) &&
                    revealable.Contains(use.Color))
                {
                    continue;
                }

                issues.Add(new ScriptIssue(
                    ScriptIssueSeverity.Error,
                    $"{use.Location}가 {use.Color} 검열({use.Key})을 쓰지만, " +
                    "그 방 단서 중 이 색을 내주는 것이 없어 영영 풀리지 않는다."));
            }

            return issues;
        }
    }
}
