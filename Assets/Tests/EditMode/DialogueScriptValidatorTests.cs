using System.Collections.Generic;
using System.Linq;
using GameName.Core.Authoring;
using GameName.Core.Clues;
using GameName.Core.MemoryRooms;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    // 저작 시점 검사가 무엇을 막고 무엇을 막지 않는지 고정한다.
    //
    // 3차 개편에서 대사 관련 규칙이 전부 빠졌고, 남은 것은 라운드 개수와 단서
    // 배치(겹침·표시 이름)뿐이다. 조립을 테스트가 따로 하지 않고
    // DialogueScriptValidatorFactory를 그대로 쓴다 — 규칙 하나를 조립에서
    // 빠뜨렸을 때 에디터 메뉴는 조용해지고 테스트만 통과하는 상황을 막기 위해서다.
    public class DialogueScriptValidatorTests
    {
        private const int ExpectedRoomCount = 3;
        private const float MinimumSeparation = 0.12f;

        private static DialogueScriptValidator Validator() =>
            DialogueScriptValidatorFactory.Create(ExpectedRoomCount, MinimumSeparation);

        private static IReadOnlyList<ScriptIssue> Errors(IReadOnlyList<ScriptIssue> issues) =>
            issues.Where(i => i.Severity == ScriptIssueSeverity.Error).ToArray();

        private static IReadOnlyList<ScriptIssue> Warnings(IReadOnlyList<ScriptIssue> issues) =>
            issues.Where(i => i.Severity == ScriptIssueSeverity.Warning).ToArray();

        private static ClueDefinition Clue(
            string id, float position, ClueKind kind = ClueKind.Poster, string displayName = null) =>
            new ClueDefinition(
                new ClueId(id), kind, displayName ?? id, new CluePositionRatio(position));

        private static RoomDefinition Round(string id, params ClueDefinition[] clues) =>
            new RoomDefinition(
                new MemoryRoomId(id),
                clues.Length == 0 ? new[] { Clue($"{id}-clue", 0.2f) } : clues,
                turnsToSurvive: 3);

        private static RunDefinition Run(params RoomDefinition[] rounds) => new RunDefinition(rounds);

        private static RunDefinition RunWith(RoomDefinition round) =>
            Run(round, Round("round-2"), Round("round-3"));

        [Test]
        public void 최소한만_든_라운드_세_개는_검증을_통과한다()
        {
            CollectionAssert.IsEmpty(
                Validator().Validate(Run(Round("round-1"), Round("round-2"), Round("round-3"))));
        }

        [Test]
        public void 라운드_개수가_기획과_다르면_오류다()
        {
            Assert.AreEqual(1, Errors(Validator().Validate(Run(Round("round-1"), Round("round-2")))).Count);
        }

        [Test]
        public void 가까이_붙은_같은_종류_단서는_오류가_아니라_경고다()
        {
            var round = Round("round-1", Clue("clue-1", 0.40f), Clue("clue-2", 0.45f));

            var issues = Validator().Validate(RunWith(round));

            CollectionAssert.IsEmpty(Errors(issues));
            Assert.AreEqual(1, Warnings(issues).Count);
        }

        [Test]
        public void 벽과_바닥은_같은_자리라도_겹치지_않는다()
        {
            var round = Round(
                "round-1",
                Clue("clue-1", 0.4f, ClueKind.Poster),
                Clue("clue-2", 0.4f, ClueKind.FloorObject));

            CollectionAssert.IsEmpty(Validator().Validate(RunWith(round)));
        }

        [Test]
        public void 표시_이름이_없는_단서는_오류가_아니라_경고다()
        {
            var round = Round("round-1", Clue("clue-1", 0.2f, displayName: ""));

            var issues = Validator().Validate(RunWith(round));

            CollectionAssert.IsEmpty(Errors(issues));
            Assert.AreEqual(1, Warnings(issues).Count);
        }
    }
}
