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
            string id, MemoryColor hidden, float position, ClueKind kind = ClueKind.Poster, string[] tags = null) =>
            new ClueDefinition(
                new ClueId(id), kind, id, new CluePositionRatio(position), hidden,
                (tags ?? new[] { id }).Select(t => new ClueTag(t)).ToArray());

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
        public void 그_방_단서가_내주지_않는_색의_검열은_오류다()
        {
            var room = Room(
                "room-1",
                new[] { Clue("clue-1", MemoryColor.Red, 0.2f) },
                "line-1",
                Line("line-1", "우리가 [[B:beach-house:해변의 작은 집]]에서 보냈던 시절이 그리워."));

            Assert.AreEqual(1, Errors(Validator().Validate(RunWith(room))).Count);
        }

        [Test]
        public void 그_방_단서가_내주는_색의_검열은_통과한다()
        {
            var room = Room(
                "room-1",
                new[] { Clue("clue-1", MemoryColor.Blue, 0.2f) },
                "line-1",
                Line("line-1", "우리가 [[B:beach-house:해변의 작은 집]]에서 보냈던 시절이 그리워."));

            CollectionAssert.IsEmpty(Validator().Validate(RunWith(room)));
        }

        [Test]
        public void 선택지_문구에_걸린_검열도_같은_검사를_받는다()
        {
            var room = Room(
                "room-1",
                new[] { Clue("clue-1", MemoryColor.Red, 0.2f) },
                "line-1",
                Line("line-1", "", Choice("choice-1", isCorrect: true, authoredText: "[[G:that-name:그 이름]]을 말한다")));

            Assert.AreEqual(1, Errors(Validator().Validate(RunWith(room))).Count);
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
        public void 같은_키를_다른_색으로_태깅하면_오류다()
        {
            // 저작자가 같은 사실을 다른 색으로 잘못 적은 상황. 두 색 다 그 방에서
            // 나올 수 있으므로 다른 규칙은 아무 말도 하지 않는다 — 오직 이 규칙만
            // 잡을 수 있는 어긋남이라는 것이 이 테스트의 요점이다.
            var room = Room(
                "room-1",
                new[]
                {
                    Clue("clue-1", MemoryColor.Blue, 0.2f),
                    Clue("clue-2", MemoryColor.Red, 0.7f),
                },
                "line-1",
                Line("line-1", "그 시절 [[B:beach-house:해변의 작은 집]]이 그립다."),
                Line("line-2", "[[R:beach-house:그 집]]은 이제 없다."));

            Assert.AreEqual(1, Errors(Validator().Validate(RunWith(room))).Count);
        }

        [Test]
        public void 같은_키를_같은_색으로_여러_줄에서_쓰는_것은_정상이다()
        {
            var room = Room(
                "room-1",
                new[] { Clue("clue-1", MemoryColor.Blue, 0.2f) },
                "line-1",
                Line("line-1", "그 시절 [[B:beach-house:해변의 작은 집]]이 그립다."),
                Line("line-2", "[[B:beach-house:그 집]]은 이제 없다."));

            CollectionAssert.IsEmpty(Validator().Validate(RunWith(room)));
        }

        [Test]
        public void 방이_달라도_같은_키의_색이_어긋나면_오류다()
        {
            // 한 사실이 여러 방에 걸쳐 언급되는 것이 이 게임의 전제라, 방마다
            // 따로 검사했다면 놓쳤을 어긋남이다.
            var first = Room(
                "room-1",
                new[] { Clue("clue-1", MemoryColor.Blue, 0.2f) },
                "line-1",
                Line("line-1", "[[B:beach-house:해변의 작은 집]]"));

            var second = Room(
                "room-2",
                new[] { Clue("clue-2", MemoryColor.Red, 0.2f) },
                "line-2",
                Line("line-2", "[[R:beach-house:그 집]]"));

            Assert.AreEqual(1, Errors(Validator().Validate(Run(first, second, Room("room-3")))).Count);
        }

        [Test]
        public void 한_키가_여러_군데에서_어긋나도_문제는_하나로_보고된다()
        {
            var room = Room(
                "room-1",
                new[]
                {
                    Clue("clue-1", MemoryColor.Blue, 0.2f),
                    Clue("clue-2", MemoryColor.Red, 0.7f),
                },
                "line-1",
                Line("line-1", "[[B:beach-house:해변의 작은 집]]"),
                Line("line-2", "[[R:beach-house:그 집]]"),
                Line("line-3", "[[R:beach-house:거기]]"));

            Assert.AreEqual(1, Errors(Validator().Validate(RunWith(room))).Count);
        }

        [Test]
        public void 선택지_문구에_적힌_키도_같은_일관성_검사를_받는다()
        {
            var room = Room(
                "room-1",
                new[]
                {
                    Clue("clue-1", MemoryColor.Blue, 0.2f),
                    Clue("clue-2", MemoryColor.Red, 0.7f),
                },
                "line-1",
                Line("line-1", "[[B:beach-house:해변의 작은 집]]",
                    Choice("choice-1", isCorrect: true, authoredText: "[[R:beach-house:그 집]] 이야기를 꺼낸다")));

            Assert.AreEqual(1, Errors(Validator().Validate(RunWith(room))).Count);
        }

        [Test]
        public void 판_안에_없는_검열_키를_요구하는_선택지는_오류다()
        {
            // 키에 오타를 냈거나, 대사의 토큰만 고치고 그 키를 걸어 둔 선택지를
            // 잊은 상황. 다른 규칙은 전부 통과하고 게임도 멀쩡히 돌아가는데,
            // 그 선택지만 한 번도 화면에 나오지 않는다.
            var room = Room(
                "room-1",
                new[] { Clue("clue-1", MemoryColor.Blue, 0.2f) },
                "line-1",
                Line("line-1", "그 시절 [[B:beach-house:해변의 작은 집]]이 그립다.",
                    Choice("choice-1", isCorrect: true,
                        condition: ChoiceCondition.RequiresCensorKeyRevealed(new CensorKey("the-ring")))));

            Assert.AreEqual(1, Errors(Validator().Validate(RunWith(room))).Count);
        }

        [Test]
        public void 같은_방_대사에_있는_검열_키를_요구하면_통과한다()
        {
            var room = Room(
                "room-1",
                new[] { Clue("clue-1", MemoryColor.Blue, 0.2f) },
                "line-1",
                Line("line-1", "그 시절 [[B:beach-house:해변의 작은 집]]이 그립다.",
                    Choice("choice-1", isCorrect: true,
                        condition: ChoiceCondition.RequiresCensorKeyRevealed(new CensorKey("beach-house")))));

            CollectionAssert.IsEmpty(Validator().Validate(RunWith(room)));
        }

        [Test]
        public void 다른_방_대사에_있는_검열_키를_요구해도_통과한다()
        {
            // 검사가 판 단위이기 때문이다. 앞선 방에서 알아낸 사실을 뒤 방에서
            // 꺼내는 것이 이 게임의 진행이라, 방 안에서만 찾았다면 정상적인
            // 저작이 오류로 잡혔을 것이다.
            var first = Room(
                "room-1",
                new[] { Clue("clue-1", MemoryColor.Blue, 0.2f) },
                "line-1",
                Line("line-1", "[[B:beach-house:해변의 작은 집]]"));

            var second = Room(
                "room-2",
                null,
                "line-2",
                Line("line-2", "",
                    Choice("choice-1", isCorrect: true,
                        condition: ChoiceCondition.RequiresCensorKeyRevealed(new CensorKey("beach-house")))));

            CollectionAssert.IsEmpty(Validator().Validate(Run(first, second, Room("room-3"))));
        }

        [Test]
        public void 선택지_문구에만_나오는_검열_키도_요구할_수_있다()
        {
            // 검열은 대사뿐 아니라 선택지 문구에도 걸린다. 키 목록을 대사에서만
            // 모았다면 정상적인 저작이 오류로 잡혔을 것이다.
            var room = Room(
                "room-1",
                new[] { Clue("clue-1", MemoryColor.Blue, 0.2f) },
                "line-1",
                Line("line-1", "",
                    Choice("choice-1", isCorrect: true, authoredText: "[[B:beach-house:그 집]] 이야기를 꺼낸다"),
                    Choice("choice-2", isCorrect: false,
                        condition: ChoiceCondition.RequiresCensorKeyRevealed(new CensorKey("beach-house")))));

            CollectionAssert.IsEmpty(Validator().Validate(RunWith(room)));
        }

        [Test]
        public void 조건이_없거나_단서_조건인_선택지는_키_검사를_받지_않는다()
        {
            var room = Room(
                "room-1", null, "line-1",
                Line("line-1", "",
                    Choice("choice-1", isCorrect: true),
                    Choice("choice-2", isCorrect: false,
                        condition: ChoiceCondition.ClueUsed(new ClueId("clue-없음")))));

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
        public void 정답_단서가_그_방에_없으면_오류다()
        {
            var room = ClueSelRoom(
                ClueSelLine("q", new[] { "clue-없음" }, "right", "wrong"),
                Line("right"), Line("wrong"));

            Assert.GreaterOrEqual(Errors(Validator().Validate(RunWith(room))).Count, 1);
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
        public void 풀_후보_원문에_걸린_검열도_해금_가능성_검사를_받는다()
        {
            // 방 단서는 Red만 내주는데(RoomWithPools 기본) 후보가 Blue 검열을 쓴다
            // → 그 후보가 뽑히면 그 말은 영영 안 풀린다.
            var pool = new BranchPool(new[]
            {
                Candidate("slot", "안전한 후보", Choice("safe", isCorrect: true)),
                Candidate("slot", "우리가 [[B:beach:그 해변 집]]에 있었지", Choice("bad", isCorrect: true)),
            });
            var room = RoomWithPools(
                new[] { Line("line-1", "", Choice("c", isCorrect: true, next: "slot")) },
                new[] { pool });

            Assert.GreaterOrEqual(Errors(Validator().Validate(RunWith(room))).Count, 1);
        }

        [Test]
        public void 풀_후보_선택지_문구에_걸린_검열도_검사를_받는다()
        {
            var pool = new BranchPool(new[]
            {
                Candidate("slot", "본문", Choice("a", isCorrect: true)),
                Candidate("slot", "본문",
                    Choice("b", isCorrect: true, authoredText: "[[G:that-name:그 이름]]을 말한다")),
            });
            var room = RoomWithPools(
                new[] { Line("line-1", "", Choice("c", isCorrect: true, next: "slot")) },
                new[] { pool });

            Assert.GreaterOrEqual(Errors(Validator().Validate(RunWith(room))).Count, 1);
        }

        [Test]
        public void 풀_후보의_검열이_그_방에서_풀릴_수_있으면_통과한다()
        {
            var pool = new BranchPool(new[]
            {
                Candidate("slot", "우리가 [[R:place:거기]]서 만났지", Choice("a", isCorrect: true)),
                Candidate("slot", "그때 [[R:place:그곳]] 기억나", Choice("b", isCorrect: true)),
            });
            // RoomWithPools 기본 단서가 Red 하나 → R 검열은 풀린다.
            var room = RoomWithPools(
                new[] { Line("line-1", "", Choice("c", isCorrect: true, next: "slot")) },
                new[] { pool });

            CollectionAssert.IsEmpty(Errors(Validator().Validate(RunWith(room))));
        }

        [Test]
        public void 풀_후보로_넣은_ClueSelection_라인도_성립_검사를_받는다()
        {
            // 정답 태그 집합이 비어 있고 분기도 안 지정된 ClueSelection 후보.
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
