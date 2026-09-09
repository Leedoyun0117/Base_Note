using System.Collections.Generic;
using System.Linq;
using GameName.Core.Authoring;
using GameName.Core.Clues;
using GameName.Core.Dialogue;
using GameName.Core.Memories;
using GameName.Core.MemoryRooms;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    // 저작 시점 검사가 무엇을 막고 무엇을 막지 않는지 고정하는 테스트.
    //
    // 조립을 테스트가 따로 하지 않고 DialogueScriptValidatorFactory를 그대로
    // 쓰는 이유: 규칙 하나를 조립에서 빠뜨렸을 때 에디터 메뉴는 조용해지고
    // 테스트만 통과하는 상황을 만들지 않기 위해서다.
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

        // 태그를 따로 안 주면 단서 id 자체를 태그로 삼는다 — 기존 테스트가
        // "이 단서 id로 답한다"고 표현하던 것을 태그 판정으로 옮겨도 그대로
        // 성립하게 하기 위한 기본값이다.
        private static ClueDefinition Clue(
            string id, MemoryColor hidden, float position, ClueKind kind = ClueKind.Poster,
            string[] tags = null, string[] subTags = null) =>
            new ClueDefinition(
                new ClueId(id), kind, id, new CluePositionRatio(position), hidden,
                (tags ?? new[] { id }).Select(ClueTag.Center)
                    .Concat((subTags ?? System.Array.Empty<string>()).Select(ClueTag.Sub))
                    .ToArray());

        private static ChoiceDefinition Choice(
            string id,
            bool isCorrect,
            string next = null,
            string authoredText = "",
            ChoiceCondition? condition = null) =>
            new ChoiceDefinition(
                new ChoiceId(id),
                authoredText,
                isCorrect,
                next == null ? (DialogueLineId?)null : new DialogueLineId(next),
                condition ?? ChoiceCondition.None);

        private static DialogueLineDefinition Line(
            string id, string authoredText = "", params ChoiceDefinition[] choices) =>
            new DialogueLineDefinition(new DialogueLineId(id), "화자", authoredText, choices);

        // 대사도 단서도 최소한만 든 방. 검사에 걸릴 거리가 아예 없는 상태를
        // 기준선으로 두고, 각 테스트는 여기서 한 가지만 어긋뜨린다.
        private static RoomDefinition Room(
            string id,
            IReadOnlyList<ClueDefinition> clues = null,
            string startLineId = null,
            params DialogueLineDefinition[] lines) =>
            new RoomDefinition(
                new MemoryRoomId(id),
                clues ?? new[] { Clue($"{id}-clue", MemoryColor.Red, 0.2f) },
                startLineId == null ? (DialogueLineId?)null : new DialogueLineId(startLineId),
                lines);

        private static RunDefinition Run(params RoomDefinition[] rooms) =>
            new RunDefinition(rooms, startingTrust: 50, startingHiromi: 5);

        private static RunDefinition RunWith(RoomDefinition room) =>
            Run(room, Room("room-2"), Room("room-3"));

        [Test]
        public void 대사가_전부_비어_있어도_방_세_개는_검증을_통과한다()
        {
            var issues = Validator().Validate(Run(Room("room-1"), Room("room-2"), Room("room-3")));

            CollectionAssert.IsEmpty(issues);
        }

        [Test]
        public void 방_개수가_기획과_다르면_오류다()
        {
            var issues = Validator().Validate(Run(Room("room-1"), Room("room-2")));

            Assert.AreEqual(1, Errors(issues).Count);
        }

        [Test]
        public void 없는_대사를_가리키는_선택지는_오류다()
        {
            var room = Room(
                "room-1", null, "line-1",
                Line("line-1", "", Choice("choice-1", isCorrect: true, next: "line-없음")));

            Assert.AreEqual(1, Errors(Validator().Validate(RunWith(room))).Count);
        }

        [Test]
        public void 방에_없는_시작_대사는_오류다()
        {
            var room = Room("room-1", null, "line-없음", Line("line-1"));

            Assert.AreEqual(1, Errors(Validator().Validate(RunWith(room))).Count);
        }

        [Test]
        public void 대화를_끝내는_선택지는_참조_검사에_걸리지_않는다()
        {
            var room = Room(
                "room-1", null, "line-1",
                Line("line-1", "", Choice("choice-1", isCorrect: true)));

            CollectionAssert.IsEmpty(Validator().Validate(RunWith(room)));
        }

        [Test]
        public void 정답_선택지가_하나도_없는_대사는_오류다()
        {
            var room = Room(
                "room-1", null, "line-1",
                Line("line-1", "",
                    Choice("choice-1", isCorrect: false),
                    Choice("choice-2", isCorrect: false)));

            Assert.AreEqual(1, Errors(Validator().Validate(RunWith(room))).Count);
        }

        [Test]
        public void 선택지가_아예_없는_대사는_정답_검사를_받지_않는다()
        {
            var room = Room("room-1", null, "line-1", Line("line-1", "혼잣말이다."));

            CollectionAssert.IsEmpty(Validator().Validate(RunWith(room)));
        }

        [Test]
        public void 가까이_붙은_같은_종류_단서는_오류가_아니라_경고다()
        {
            var room = Room(
                "room-1",
                new[]
                {
                    Clue("clue-1", MemoryColor.Red, 0.40f),
                    Clue("clue-2", MemoryColor.Red, 0.45f),
                });

            var issues = Validator().Validate(RunWith(room));

            CollectionAssert.IsEmpty(Errors(issues));
            Assert.AreEqual(1, Warnings(issues).Count);
        }

        [Test]
        public void 벽과_바닥은_같은_자리라도_겹치지_않는다()
        {
            var room = Room(
                "room-1",
                new[]
                {
                    Clue("clue-1", MemoryColor.Red, 0.4f, ClueKind.Poster),
                    Clue("clue-2", MemoryColor.Red, 0.4f, ClueKind.FloorObject),
                });

            CollectionAssert.IsEmpty(Validator().Validate(RunWith(room)));
        }

        [Test]
        public void 표시_이름이_없는_단서는_오류가_아니라_경고다()
        {
            var room = Room(
                "room-1",
                new[]
                {
                    new ClueDefinition(
                        new ClueId("clue-1"), ClueKind.Poster, "", new CluePositionRatio(0.2f), MemoryColor.Red),
                });

            var issues = Validator().Validate(RunWith(room));

            CollectionAssert.IsEmpty(Errors(issues));
            Assert.AreEqual(1, Warnings(issues).Count);
        }

        // ── ClueSelection 줄 검증 ─────────────────────────────────────────

        private static DialogueLineDefinition ClueSelLine(
            string id, string[] requiredTags, string correctNext, string incorrectNext) =>
            DialogueLineDefinition.ClueSelection(
                new DialogueLineId(id), "화자", "무엇을 들고 있었어?",
                System.Array.ConvertAll(requiredTags, t => new ClueTag(t)),
                new DialogueLineId(correctNext), new DialogueLineId(incorrectNext));

        private static RoomDefinition ClueSelRoom(
            DialogueLineDefinition clueLine, params DialogueLineDefinition[] rest)
        {
            var lines = new System.Collections.Generic.List<DialogueLineDefinition> { clueLine };
            lines.AddRange(rest);
            return Room(
                "room-1",
                new[]
                {
                    Clue("clue-a", MemoryColor.Red, 0.2f),
                    Clue("clue-b", MemoryColor.Red, 0.5f),
                },
                clueLine.Id.Value,
                lines.ToArray());
        }

        [Test]
        public void 정답_단서_집합이_비어_있으면_오류다()
        {
            var room = ClueSelRoom(
                ClueSelLine("q", System.Array.Empty<string>(), "right", "wrong"),
                Line("right"), Line("wrong"));

            Assert.GreaterOrEqual(Errors(Validator().Validate(RunWith(room))).Count, 1);
        }

        [Test]
        public void 정답_태그를_그_시점까지_어디서도_얻을_수_없으면_오류다()
        {
            var room = ClueSelRoom(
                ClueSelLine("q", new[] { "clue-없음" }, "right", "wrong"),
                Line("right"), Line("wrong"));

            Assert.GreaterOrEqual(Errors(Validator().Validate(RunWith(room))).Count, 1);
        }

        [Test]
        public void 앞_방_단서_태그로만_답할_수_있는_ClueSelection은_오류다()
        {
            // 방 2의 줄이 방 1에서만 주울 수 있는 단서 태그를 정답으로 요구한다.
            // 손에 든 단서는 방을 넘어갈 때 전부 버려지므로(RoomEntryInventoryClear)
            // 방 2에선 낼 수 없는 답 — 저작 오류다.
            var room1 = Room("room-1", new[] { Clue("room1-key", MemoryColor.Red, 0.2f) }, null);

            var clueLine = DialogueLineDefinition.ClueSelection(
                new DialogueLineId("q"), "화자", "그때 뭘 쥐고 있었어?",
                new[] { new ClueTag("room1-key") },
                new DialogueLineId("right"), new DialogueLineId("wrong"));
            var room2 = Room(
                "room-2",
                new[] { Clue("room-2-clue", MemoryColor.Red, 0.2f) },
                "q", clueLine, Line("right"), Line("wrong"));

            Assert.GreaterOrEqual(
                Errors(Validator().Validate(Run(room1, room2, Room("room-3")))).Count, 1);
        }

        [Test]
        public void ClueSelection_분기가_없는_대사를_가리키면_오류다()
        {
            var room = ClueSelRoom(
                ClueSelLine("q", new[] { "clue-a" }, "right", "없는줄"),
                Line("right"));

            Assert.GreaterOrEqual(Errors(Validator().Validate(RunWith(room))).Count, 1);
        }

        [Test]
        public void 제대로_저작된_ClueSelection_줄은_문제가_없다()
        {
            var room = ClueSelRoom(
                ClueSelLine("q", new[] { "clue-a", "clue-b" }, "right", "wrong-1"),
                Line("right", "", Choice("r-end", isCorrect: true, next: "merge")),
                Line("wrong-1", "", Choice("w1", isCorrect: true, next: "merge")),
                Line("merge", "", Choice("m-end", isCorrect: true)));

            CollectionAssert.IsEmpty(Errors(Validator().Validate(RunWith(room))));
        }

        [Test]
        public void 질문의_중심축_태그를_중심축으로_가진_단서가_방에_없으면_오류다()
        {
            // 이 방 단서는 "held"를 곁축으로만 갖는다 — 그걸로 답해 봤자 스치기만
            // 하고 완전적합은 못 된다. 그런데 질문은 "held"를 중심축으로 요구한다.
            var clue = Clue(
                "clue-a", MemoryColor.Red, 0.2f,
                tags: new[] { "clue-a.topic" }, subTags: new[] { "held" });

            var room = Room(
                "room-1",
                new[] { clue },
                "q",
                DialogueLineDefinition.ClueSelection(
                    new DialogueLineId("q"), "화자", "그때 뭘 쥐고 있었어?",
                    new[] { new ClueTag("held") },
                    new DialogueLineId("right"), new DialogueLineId("wrong")),
                Line("right", "", Choice("r-end", isCorrect: true)),
                Line("wrong", "", Choice("w-end", isCorrect: true)));

            Assert.IsTrue(
                Errors(Validator().Validate(RunWith(room))).Any(e => e.Description.Contains("완전적합")),
                "중심축 정답을 낼 단서가 없는 방은 저작 시점에 걸려야 한다.");
        }

        [Test]
        public void 곁축_정답_태그는_중심축_단서로_커버되면_오류가_아니다()
        {
            var clue = Clue(
                "clue-a", MemoryColor.Red, 0.2f,
                tags: new[] { "held" }, subTags: new[] { "rooftop" });

            var room = Room(
                "room-1",
                new[] { clue },
                "q",
                DialogueLineDefinition.ClueSelection(
                    new DialogueLineId("q"), "화자", "그때 옥상에서 뭘 쥐고 있었어?",
                    new[] { new ClueTag("held"), ClueTag.Sub("rooftop") },
                    new DialogueLineId("right"), new DialogueLineId("wrong")),
                Line("right", "", Choice("r-end", isCorrect: true)),
                Line("wrong", "", Choice("w-end", isCorrect: true)));

            CollectionAssert.IsEmpty(Errors(Validator().Validate(RunWith(room))));
        }

        // ── 분기 풀 검증 ─────────────────────────────────────────────────

        // 후보들은 슬롯 id를 공유한다. tag(AuthoredText)로만 서로 구분된다.
        private static DialogueLineDefinition Candidate(
            string slotId, string tag, params ChoiceDefinition[] choices) =>
            new DialogueLineDefinition(new DialogueLineId(slotId), "화자", tag, choices);

        private static BranchPool Pool(string slotId, int count) =>
            new BranchPool(Enumerable.Range(1, count)
                .Select(i => Candidate(
                    slotId, $"{slotId}-{i}", Choice($"{slotId}-{i}-c", isCorrect: true)))
                .ToArray());

        private static RoomDefinition RoomWithPools(
            IReadOnlyList<DialogueLineDefinition> lines,
            IReadOnlyList<BranchPool> pools,
            string startLineId = "line-1") =>
            new RoomDefinition(
                new MemoryRoomId("room-1"),
                new[] { Clue("room-1-clue", MemoryColor.Red, 0.2f) },
                startLineId == null ? (DialogueLineId?)null : new DialogueLineId(startLineId),
                lines,
                pools);

        [Test]
        public void 분기_풀_후보가_하나뿐이면_오류다()
        {
            var room = RoomWithPools(
                new[] { Line("line-1", "", Choice("c", isCorrect: true, next: "slot")) },
                new[] { Pool("slot", 1) });

            Assert.GreaterOrEqual(Errors(Validator().Validate(RunWith(room))).Count, 1);
        }

        [Test]
        public void 분기_풀_후보들이_서로_다른_라인_id를_쓰면_오류다()
        {
            var pool = new BranchPool(new[]
            {
                Candidate("slot", "a", Choice("a-c", isCorrect: true)),
                Candidate("slot-다른", "b", Choice("b-c", isCorrect: true)),
            });
            var room = RoomWithPools(
                new[] { Line("line-1", "", Choice("c", isCorrect: true, next: "slot")) },
                new[] { pool });

            Assert.GreaterOrEqual(Errors(Validator().Validate(RunWith(room))).Count, 1);
        }

        [Test]
        public void 분기_풀_슬롯_id가_같은_방의_고정_라인_id와_겹치면_오류다()
        {
            var room = RoomWithPools(
                new[]
                {
                    Line("line-1", "", Choice("c", isCorrect: true, next: "slot")),
                    Line("slot", "고정인데 슬롯 id와 같다"),
                },
                new[] { Pool("slot", 2) });

            Assert.GreaterOrEqual(Errors(Validator().Validate(RunWith(room))).Count, 1);
        }

        [Test]
        public void 두_분기_풀이_같은_슬롯_id를_쓰면_오류다()
        {
            var room = RoomWithPools(
                new[] { Line("line-1", "", Choice("c", isCorrect: true, next: "slot")) },
                new[] { Pool("slot", 2), Pool("slot", 2) });

            Assert.GreaterOrEqual(Errors(Validator().Validate(RunWith(room))).Count, 1);
        }

        [Test]
        public void 분기_풀_후보의_끊긴_Next는_오류다()
        {
            var pool = new BranchPool(new[]
            {
                Candidate("slot", "a", Choice("a-c", isCorrect: true, next: "line-없음")),
                Candidate("slot", "b", Choice("b-c", isCorrect: true)),
            });
            var room = RoomWithPools(
                new[] { Line("line-1", "", Choice("c", isCorrect: true, next: "slot")) },
                new[] { pool });

            Assert.GreaterOrEqual(Errors(Validator().Validate(RunWith(room))).Count, 1);
        }

        [Test]
        public void 모든_후보가_막다른_길인_분기_풀은_오류다()
        {
            var pool = new BranchPool(new[]
            {
                Candidate("slot", "a", Choice("a-c", isCorrect: false)),
                Candidate("slot", "b", Choice("b-c", isCorrect: false)),
            });
            var room = RoomWithPools(
                new[] { Line("line-1", "", Choice("c", isCorrect: true, next: "slot")) },
                new[] { pool });

            Assert.GreaterOrEqual(Errors(Validator().Validate(RunWith(room))).Count, 1);
        }

        [Test]
        public void 백본이_분기_풀_슬롯_id로_Next를_걸어도_참조_검사에_안_걸린다()
        {
            // 슬롯 id는 "없는 대사"가 아니다 — 뽑힌 후보가 그 자리에 들어온다.
            var room = RoomWithPools(
                new[] { Line("line-1", "", Choice("c", isCorrect: true, next: "slot")) },
                new[] { Pool("slot", 2) });

            CollectionAssert.IsEmpty(Errors(Validator().Validate(RunWith(room))));
        }

        [Test]
        public void 제대로_저작된_분기_풀은_문제가_없다()
        {
            var pool = new BranchPool(new[]
            {
                Candidate("slot", "짧은 답", Choice("slot-short", isCorrect: true, next: "line-2")),
                Candidate("slot", "긴 답", Choice("slot-long", isCorrect: true, next: "line-2")),
            });
            var room = RoomWithPools(
                new[]
                {
                    Line("line-1", "", Choice("to-slot", isCorrect: true, next: "slot")),
                    Line("line-2", "", Choice("end", isCorrect: true)),
                },
                new[] { pool });

            CollectionAssert.IsEmpty(Errors(Validator().Validate(RunWith(room))));
        }

        [Test]
        public void 풀_후보로_넣은_ClueSelection_라인도_성립_검사를_받는다()
        {
            // 정답 태그 집합이 비어 있는 ClueSelection 후보 — 어떤 단서를 내밀어도
            // 오답이라 성립하지 않는다.
            var broken = DialogueLineDefinition.ClueSelection(
                new DialogueLineId("slot"), "화자", "무엇을 쥐고 있었어?",
                System.Array.Empty<ClueTag>(), null, null);
            var ok = DialogueLineDefinition.ClueSelection(
                new DialogueLineId("slot"), "화자", "그때 손엔 뭐가?",
                new[] { new ClueTag("room-1-clue") },
                new DialogueLineId("line-2"), new DialogueLineId("line-2"));

            var room = RoomWithPools(
                new[]
                {
                    Line("line-1", "", Choice("c", isCorrect: true, next: "slot")),
                    Line("line-2", "", Choice("end", isCorrect: true)),
                },
                new[] { new BranchPool(new[] { broken, ok }) });

            Assert.GreaterOrEqual(Errors(Validator().Validate(RunWith(room))).Count, 1);
        }
    }
}
