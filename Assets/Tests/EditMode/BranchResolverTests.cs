using System;
using System.Collections.Generic;
using System.Linq;
using GameName.Core.Authoring;
using GameName.Core.Clues;
using GameName.Core.Dialogue;
using GameName.Core.Events;
using GameName.Core.MemoryRooms;
using GameName.Core.Mind;
using GameName.Core.Trust;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    // 랜덤 분기 풀은 런 시작 시 시드로 한 번만 확정되고, 그 확정은 풀별로
    // 독립적이며(순서·개수에 안 딸려감), 확정본은 고정 라인과 뽑힌 라인이 섞인
    // 하나의 대화 그래프로 끊김 없이 진행된다.
    public class BranchResolverTests
    {
        private static ChoiceDefinition Choice(string id, bool isCorrect, string next = null) =>
            new ChoiceDefinition(
                new ChoiceId(id), "", isCorrect,
                next == null ? (DialogueLineId?)null : new DialogueLineId(next), ChoiceCondition.None);

        private static DialogueLineDefinition Line(
            string id, string text, params ChoiceDefinition[] choices) =>
            new DialogueLineDefinition(new DialogueLineId(id), "화자", text, choices);

        // 후보들은 슬롯 id를 공유하므로, 어느 것이 뽑혔는지는 AuthoredText로 구분한다.
        private static BranchPool Pool(string slotId, params string[] candidateTexts) =>
            new BranchPool(candidateTexts
                .Select(t => Line(slotId, t, Choice($"{slotId}-{t}", true)))
                .ToArray());

        private static RoomDefinition Room(
            string id,
            IReadOnlyList<DialogueLineDefinition> lines,
            IReadOnlyList<BranchPool> pools,
            string startLineId = null) =>
            new RoomDefinition(
                new MemoryRoomId(id),
                Array.Empty<ClueDefinition>(),
                startLineId == null ? (DialogueLineId?)null : new DialogueLineId(startLineId),
                lines,
                pools);

        private static RunDefinition Run(int seed, params RoomDefinition[] rooms) =>
            new RunDefinition(rooms, startingTrust: 3, startingHiromi: 5, seed: seed);

        private static string PickedText(RunDefinition resolved, string roomId, string slotId) =>
            resolved.Rooms
                .Single(r => r.Id.Equals(new MemoryRoomId(roomId)))
                .DialogueLines
                .Single(l => l.Id.Equals(new DialogueLineId(slotId)))
                .AuthoredText;

        [Test]
        public void 같은_시드는_항상_같은_후보를_고른다()
        {
            var raw = Run(1234, Room(
                "room-1",
                new[] { Line("b1", "백본") },
                new[] { Pool("slot", "a", "b", "c", "d", "e") }));

            var first = PickedText(BranchResolver.Resolve(raw, raw.Seed), "room-1", "slot");
            for (var i = 0; i < 20; i++)
                Assert.AreEqual(first, PickedText(BranchResolver.Resolve(raw, raw.Seed), "room-1", "slot"));
        }

        [Test]
        public void 시드가_다르면_대체로_다른_후보가_나온다()
        {
            var picks = new HashSet<string>();
            for (var seed = 0; seed < 50; seed++)
            {
                var raw = Run(seed, Room(
                    "room-1",
                    new[] { Line("b1", "백본") },
                    new[] { Pool("slot", "a", "b", "c", "d", "e") }));
                picks.Add(PickedText(BranchResolver.Resolve(raw, raw.Seed), "room-1", "slot"));
            }

            // 5개 후보 전부는 아니어도, 시드에 따라 갈리기는 해야 한다.
            Assert.GreaterOrEqual(picks.Count, 3);
        }

        [Test]
        public void 풀을_추가하거나_재배치해도_기존_풀의_선정은_안_바뀐다()
        {
            var backbone = new[] { Line("b1", "백본") };

            var before = BranchResolver.Resolve(
                Run(77, Room("room-1", backbone, new[]
                {
                    Pool("slot-a", "a1", "a2", "a3"),
                    Pool("slot-b", "b1", "b2", "b3"),
                })), 77);

            var after = BranchResolver.Resolve(
                Run(77, Room("room-1", backbone, new[]
                {
                    Pool("slot-b", "b1", "b2", "b3"),
                    Pool("slot-c", "c1", "c2", "c3"),
                    Pool("slot-a", "a1", "a2", "a3"),
                })), 77);

            Assert.AreEqual(
                PickedText(before, "room-1", "slot-a"), PickedText(after, "room-1", "slot-a"),
                "slot-a의 선정은 다른 풀이 추가·재배치돼도 그대로여야 한다.");
            Assert.AreEqual(
                PickedText(before, "room-1", "slot-b"), PickedText(after, "room-1", "slot-b"),
                "slot-b의 선정도 마찬가지.");
        }

        [Test]
        public void 확정본에는_풀이_남지_않고_뽑힌_라인이_대사_목록에_들어간다()
        {
            var raw = Run(5, Room(
                "room-1",
                new[] { Line("b1", "백본") },
                new[] { Pool("slot", "x", "y") }));

            var resolved = BranchResolver.Resolve(raw, raw.Seed).Rooms[0];

            CollectionAssert.IsEmpty(resolved.BranchPools);
            Assert.AreEqual(2, resolved.DialogueLines.Count);
            Assert.IsTrue(resolved.DialogueLines.Any(l => l.Id.Equals(new DialogueLineId("slot"))));
        }

        [Test]
        public void 후보가_없는_풀은_터뜨리지_않고_건너뛴다()
        {
            var raw = Run(5, Room(
                "room-1",
                new[] { Line("b1", "백본") },
                new[] { new BranchPool(Array.Empty<DialogueLineDefinition>()) }));

            RunDefinition resolved = null;
            Assert.DoesNotThrow(() => resolved = BranchResolver.Resolve(raw, raw.Seed));
            Assert.AreEqual(1, resolved.Rooms[0].DialogueLines.Count);
        }

        [Test]
        public void 풀이_없는_방은_손대지_않고_그대로_돌려준다()
        {
            var room = Room("room-1", new[] { Line("b1", "백본") }, Array.Empty<BranchPool>());
            var resolved = BranchResolver.Resolve(Run(1, room), 1);

            Assert.AreSame(room, resolved.Rooms[0]);
        }

        [Test]
        public void 시작_조건과_시드는_확정본에_그대로_실려_나온다()
        {
            var raw = Run(999, Room(
                "room-1", new[] { Line("b1", "백본") }, new[] { Pool("slot", "x", "y") }));

            var resolved = BranchResolver.Resolve(raw, raw.Seed);

            Assert.AreEqual(3, resolved.StartingTrust);
            Assert.AreEqual(5, resolved.StartingHiromi);
            Assert.AreEqual(999, resolved.Seed);
        }

        // ── 백본 → 분기 풀 → 백본이 하나의 DialogueProgressor 흐름으로 이어진다 ──

        private sealed class Fixture
        {
            public readonly EventBus Bus = new EventBus(new NoOpEventExceptionHandler());
            public readonly DialogueProgressor Progressor;

            public Fixture(RunDefinition resolvedRun)
            {
                var trust = new TrustGauge(resolvedRun.StartingTrust, Bus);
                var clueState = new ClueStateStore(resolvedRun.Rooms, Bus);
                var censor = new CensorUnlockLog();
                Progressor = new DialogueProgressor(
                    resolvedRun.Rooms, censor, clueState, trust, new TagMatchGrader(),
                    new StabilityAxis(0, -100, 100, Bus), Bus);
                Bus.Publish(new RoomStartedEvent(resolvedRun.Rooms[0].Id, 0));
            }
        }

        [Test]
        public void 백본에서_뽑힌_분기를_거쳐_다시_백본으로_끊김없이_이어진다()
        {
            // b-start → (slot) → b-end. 두 후보 다 b-end로 합류한다.
            var pool = new BranchPool(new[]
            {
                Line("slot", "후보1", Choice("slot-1-go", true, next: "b-end")),
                Line("slot", "후보2", Choice("slot-2-go", true, next: "b-end")),
            });

            var room = Room(
                "room-1",
                new[]
                {
                    Line("b-start", "시작", Choice("to-slot", true, next: "slot")),
                    Line("b-end", "끝", Choice("finish", true)),
                },
                new[] { pool },
                startLineId: "b-start");

            var resolved = BranchResolver.Resolve(Run(3, room), 3);
            var fx = new Fixture(resolved);

            Assert.AreEqual(new DialogueLineId("b-start"), fx.Progressor.CurrentLineId);

            fx.Progressor.Select(new ChoiceId("to-slot"));
            Assert.AreEqual(new DialogueLineId("slot"), fx.Progressor.CurrentLineId,
                "백본에서 슬롯 id로 Next를 걸면 뽑힌 후보 라인으로 들어간다.");

            var branchChoice = fx.Progressor.VisibleChoices().Single();
            var result = fx.Progressor.Select(branchChoice.Id);

            Assert.AreEqual(ChoiceSelectionOutcome.Advanced, result.Outcome);
            Assert.AreEqual(new DialogueLineId("b-end"), fx.Progressor.CurrentLineId,
                "뽑힌 후보의 선택지가 백본으로 합류한다.");

            Assert.AreEqual(
                ChoiceSelectionOutcome.DialogueEnded,
                fx.Progressor.Select(new ChoiceId("finish")).Outcome);
        }

        // ── ClueSelection 라인을 분기 풀 후보로 ──────────────────────────────

        private static DialogueLineDefinition ClueCandidate(string slotId, string requiredTag, string text) =>
            DialogueLineDefinition.ClueSelection(
                new DialogueLineId(slotId), "화자", text,
                new[] { new ClueTag(requiredTag) },
                new DialogueLineId("b-right"), new DialogueLineId("b-wrong"));

        [Test]
        public void ClueSelection_후보도_확정본에_그대로_병합된다()
        {
            var pool = new BranchPool(new[]
            {
                ClueCandidate("slot", "clue-a", "무엇을 쥐고 있었어?"),
                ClueCandidate("slot", "clue-b", "그때 손에 뭐가 있었지?"),
            });

            var room = Room(
                "room-1",
                new[]
                {
                    Line("b-start", "시작", Choice("go", true, next: "slot")),
                    Line("b-right", "정답", Choice("r", true)),
                    Line("b-wrong", "오답", Choice("w", true)),
                },
                new[] { pool },
                startLineId: "b-start");

            var merged = BranchResolver.Resolve(Run(42, room), 42).Rooms[0]
                .DialogueLines.Single(l => l.Id.Equals(new DialogueLineId("slot")));

            Assert.AreEqual(DialoguePromptKind.ClueSelection, merged.PromptKind);
            Assert.AreEqual(new DialogueLineId("b-right"), merged.CorrectNext);
            Assert.AreEqual(new DialogueLineId("b-wrong"), merged.IncorrectNext);
            Assert.AreEqual(1, merged.RequiredTags.Count);
        }

        [Test]
        public void 뽑힌_ClueSelection_후보를_거쳐_오답_분기로_백본에_합류한다()
        {
            var pool = new BranchPool(new[]
            {
                ClueCandidate("slot", "clue-a", "무엇을 쥐고 있었어?"),
                ClueCandidate("slot", "clue-b", "그때 손에 뭐가 있었지?"),
            });

            var room = Room(
                "room-1",
                new[]
                {
                    Line("b-start", "시작", Choice("go", true, next: "slot")),
                    Line("b-right", "정답", Choice("r", true)),
                    Line("b-wrong", "오답", Choice("w", true, next: "b-end")),
                    Line("b-end", "끝", Choice("finish", true)),
                },
                new[] { pool },
                startLineId: "b-start");

            var fx = new Fixture(BranchResolver.Resolve(Run(7, room), 7));

            fx.Progressor.Select(new ChoiceId("go"));
            Assert.AreEqual(new DialogueLineId("slot"), fx.Progressor.CurrentLineId);
            Assert.AreEqual(DialoguePromptKind.ClueSelection, fx.Progressor.CurrentLine.PromptKind);

            var result = fx.Progressor.SkipClueSelection();
            Assert.AreEqual(ChoiceSelectionOutcome.Advanced, result.Outcome);
            Assert.AreEqual(new DialogueLineId("b-wrong"), fx.Progressor.CurrentLineId);

            fx.Progressor.Select(new ChoiceId("w"));
            Assert.AreEqual(new DialogueLineId("b-end"), fx.Progressor.CurrentLineId);
        }
    }
}
