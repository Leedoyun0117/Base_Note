using System;
using System.Collections.Generic;
using GameName.Core.Authoring;
using GameName.Core.Clues;
using GameName.Core.Dialogue;
using GameName.Core.Events;
using GameName.Core.Memories;
using GameName.Core.MemoryRooms;
using GameName.Core.Trust;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    // 표시 가능한 선택지 필터링은 검열/단서 판정에 위임하고, 오답은 게이지에
    // 위임하며, 신뢰 0 뒤로는 입력을 받지 않는다.
    public class DialogueProgressorTests
    {
        private static readonly MemoryRoomId TheRoom = new MemoryRoomId("room-1");

        private static ChoiceDefinition Choice(
            string id, bool isCorrect, string next = null, ChoiceCondition? condition = null) =>
            new ChoiceDefinition(
                new ChoiceId(id), "", isCorrect,
                next == null ? (DialogueLineId?)null : new DialogueLineId(next),
                condition ?? ChoiceCondition.None);

        private static DialogueLineDefinition Line(string id, params ChoiceDefinition[] choices) =>
            new DialogueLineDefinition(new DialogueLineId(id), "화자", "", choices);

        private sealed class Fixture
        {
            public readonly EventBus Bus = new EventBus(new NoOpEventExceptionHandler());
            public readonly TrustGauge Trust;
            public readonly CensorUnlockLog Censor = new CensorUnlockLog();
            public readonly ClueStateStore ClueState;
            public readonly DialogueProgressor Progressor;
            public readonly List<DialogueLineEnteredEvent> Entered = new List<DialogueLineEnteredEvent>();
            public readonly List<ChoiceSelectedEvent> Selected = new List<ChoiceSelectedEvent>();
            public readonly List<DialogueEndedEvent> Ended = new List<DialogueEndedEvent>();
            public readonly List<ClueAnsweredEvent> ClueAnswered = new List<ClueAnsweredEvent>();

            public Fixture(string startLine, IReadOnlyList<DialogueLineDefinition> lines, IReadOnlyList<ClueDefinition> clues = null)
            {
                var room = new RoomDefinition(
                    TheRoom, clues ?? Array.Empty<ClueDefinition>(), new DialogueLineId(startLine), lines);
                var rooms = new[] { room };

                Trust = new TrustGauge(3, Bus);
                ClueState = new ClueStateStore(rooms, Bus);
                Progressor = new DialogueProgressor(rooms, Censor, ClueState, Trust, Bus);

                Bus.Subscribe<DialogueLineEnteredEvent>(Entered.Add);
                Bus.Subscribe<ChoiceSelectedEvent>(Selected.Add);
                Bus.Subscribe<DialogueEndedEvent>(Ended.Add);
                Bus.Subscribe<ClueAnsweredEvent>(ClueAnswered.Add);

                Bus.Publish(new RoomStartedEvent(TheRoom, 0));
            }

            public IReadOnlyList<string> VisibleChoiceIds()
            {
                var ids = new List<string>();
                foreach (var c in Progressor.VisibleChoices())
                    ids.Add(c.Id.Value);
                return ids;
            }
        }

        [Test]
        public void 방이_시작되면_시작_대사로_들어간다()
        {
            var fx = new Fixture("line-1", new[] { Line("line-1", Choice("a", true)) });

            Assert.AreEqual(new DialogueLineId("line-1"), fx.Progressor.CurrentLineId);
            Assert.AreEqual(1, fx.Entered.Count);
        }

        [Test]
        public void 검열_조건_선택지는_해금_전엔_안_보이고_해금_후엔_보인다()
        {
            var fx = new Fixture("line-1", new[]
            {
                Line("line-1",
                    Choice("plain", true),
                    Choice("gated", true, condition: ChoiceCondition.RequiresCensorKeyRevealed(new CensorKey("beach-house")))),
            });

            CollectionAssert.AreEquivalent(new[] { "plain" }, fx.VisibleChoiceIds());

            fx.Censor.Record(new CensorKey("beach-house"));

            CollectionAssert.AreEquivalent(new[] { "plain", "gated" }, fx.VisibleChoiceIds());
        }

        [Test]
        public void 단서_사용_조건_선택지는_그_단서를_대화에_쓴_뒤에만_보인다()
        {
            var clue = new ClueDefinition(
                new ClueId("clue-1"), ClueKind.Poster, "단서", new CluePositionRatio(0.5f), MemoryColor.Red);
            var fx = new Fixture("line-1", new[]
            {
                Line("line-1",
                    Choice("plain", true),
                    Choice("gated", true, condition: ChoiceCondition.ClueUsed(new ClueId("clue-1")))),
            }, new[] { clue });

            CollectionAssert.AreEquivalent(new[] { "plain" }, fx.VisibleChoiceIds());

            fx.ClueState.SetState(new ClueId("clue-1"), ClueState.Collected);
            fx.ClueState.SetState(new ClueId("clue-1"), ClueState.UsedInDialogue);

            CollectionAssert.AreEquivalent(new[] { "plain", "gated" }, fx.VisibleChoiceIds());
        }

        [Test]
        public void 다른_방_단서를_가리키는_조건은_조용히_안_보이는_것으로_친다()
        {
            var fx = new Fixture("line-1", new[]
            {
                Line("line-1",
                    Choice("plain", true),
                    Choice("gated", true, condition: ChoiceCondition.ClueUsed(new ClueId("clue-다른방")))),
            });

            Assert.DoesNotThrow(() => fx.VisibleChoiceIds());
            CollectionAssert.AreEquivalent(new[] { "plain" }, fx.VisibleChoiceIds());
        }

        [Test]
        public void 정답_선택지는_신뢰도를_깎지_않고_다음_대사로_넘어간다()
        {
            var fx = new Fixture("line-1", new[]
            {
                Line("line-1", Choice("a", true, next: "line-2")),
                Line("line-2", Choice("b", true)),
            });

            var result = fx.Progressor.Select(new ChoiceId("a"));

            Assert.AreEqual(ChoiceSelectionOutcome.Advanced, result.Outcome);
            Assert.AreEqual(3, fx.Trust.Current);
            Assert.AreEqual(new DialogueLineId("line-2"), fx.Progressor.CurrentLineId);
            Assert.AreEqual(1, fx.Selected.Count);
        }

        [Test]
        public void 오답_선택지는_게이지에_위임해_신뢰도를_1_깎는다()
        {
            var fx = new Fixture("line-1", new[]
            {
                Line("line-1", Choice("wrong", false, next: "line-1"), Choice("right", true)),
            });

            fx.Progressor.Select(new ChoiceId("wrong"));

            Assert.AreEqual(2, fx.Trust.Current);
        }

        [Test]
        public void 다음_대사가_없는_선택지는_대화_종료_사건을_낸다()
        {
            var fx = new Fixture("line-1", new[] { Line("line-1", Choice("end", true)) });

            var result = fx.Progressor.Select(new ChoiceId("end"));

            Assert.AreEqual(ChoiceSelectionOutcome.DialogueEnded, result.Outcome);
            Assert.AreEqual(1, fx.Ended.Count);
            Assert.AreEqual(TheRoom, fx.Ended[0].RoomId);
        }

        [Test]
        public void 표시_중이_아닌_선택지_선택은_거부된다()
        {
            var fx = new Fixture("line-1", new[]
            {
                Line("line-1",
                    Choice("plain", true),
                    Choice("gated", true, condition: ChoiceCondition.RequiresCensorKeyRevealed(new CensorKey("k")))),
            });

            Assert.AreEqual(ChoiceSelectionOutcome.Rejected, fx.Progressor.Select(new ChoiceId("gated")).Outcome);
        }

        [Test]
        public void 신뢰도가_0이_되면_그_뒤_선택_입력은_무시된다()
        {
            var fx = new Fixture("line-1", new[]
            {
                Line("line-1", Choice("wrong", false, next: "line-1"), Choice("right", true)),
            });

            fx.Progressor.Select(new ChoiceId("wrong")); // 3 → 2
            fx.Progressor.Select(new ChoiceId("wrong")); // 2 → 1
            fx.Progressor.Select(new ChoiceId("wrong")); // 1 → 0

            Assert.AreEqual(0, fx.Trust.Current);
            Assert.AreEqual(ChoiceSelectionOutcome.Ignored, fx.Progressor.Select(new ChoiceId("right")).Outcome);
        }

        // ── ClueSelection: 질문에 단서로 답하기 ─────────────────────────────

        private static ClueDefinition ClueDef(string id) =>
            new ClueDefinition(new ClueId(id), ClueKind.FloorObject, id, new CluePositionRatio(0.5f), MemoryColor.Red);

        private static DialogueLineDefinition ClueLine(
            string id, string[] required, string correctNext, string incorrectNext) =>
            DialogueLineDefinition.ClueSelection(
                new DialogueLineId(id), "화자", "무엇을 들고 있었어?",
                System.Array.ConvertAll(required, r => new ClueId(r)),
                new DialogueLineId(correctNext), new DialogueLineId(incorrectNext));

        private static Fixture ClueSelectionFixture()
        {
            var fx = new Fixture("q", new[]
            {
                ClueLine("q", new[] { "clue-a", "clue-b" }, "right", "wrong-1"),
                Line("right", Choice("r", true)),
                Line("wrong-1", Choice("w1", true, next: "wrong-2")),
                Line("wrong-2", Choice("w2", true, next: "merge")),
                Line("merge", Choice("m", true)),
            }, new[] { ClueDef("clue-a"), ClueDef("clue-b"), ClueDef("clue-c") });

            // 세 단서를 손에 든 상태로 만든다.
            foreach (var id in new[] { "clue-a", "clue-b", "clue-c" })
                fx.ClueState.SetState(new ClueId(id), ClueState.Collected);
            return fx;
        }

        [Test]
        public void ClueSelection_줄은_텍스트_선택지_대신_들고_있는_단서만_노출한다()
        {
            var fx = ClueSelectionFixture();
            fx.ClueState.SetState(new ClueId("clue-c"), ClueState.Extracted); // 손에서 사라짐

            CollectionAssert.IsEmpty(fx.Progressor.VisibleChoices());

            var ids = new List<string>();
            foreach (var pair in fx.Progressor.SelectableClues())
                ids.Add(pair.Key.Value);

            CollectionAssert.AreEquivalent(new[] { "clue-a", "clue-b" }, ids); // clue-c는 제외
        }

        [Test]
        public void 정답_단서_아무거나_골라도_정답_분기로_간다()
        {
            foreach (var answer in new[] { "clue-a", "clue-b" })
            {
                var fx = ClueSelectionFixture();
                var result = fx.Progressor.SelectClue(new ClueId(answer));

                Assert.AreEqual(ChoiceSelectionOutcome.Advanced, result.Outcome);
                Assert.AreEqual(new DialogueLineId("right"), fx.Progressor.CurrentLineId);
                Assert.AreEqual(ClueState.UsedInDialogue, fx.ClueState.GetState(new ClueId(answer)));
                CollectionAssert.AreEqual(new[] { true }, fx.ClueAnswered.ConvertAll(e => e.WasCorrect));
            }
        }

        [Test]
        public void 오답_단서는_오답_분기로_가고_신뢰는_깎지_않는다()
        {
            var fx = ClueSelectionFixture();

            var result = fx.Progressor.SelectClue(new ClueId("clue-c"));

            Assert.AreEqual(ChoiceSelectionOutcome.Advanced, result.Outcome);
            Assert.AreEqual(new DialogueLineId("wrong-1"), fx.Progressor.CurrentLineId);
            Assert.AreEqual(3, fx.Trust.Current, "단서 오답은 신뢰를 깎지 않는다.");
            Assert.AreEqual(ClueState.UsedInDialogue, fx.ClueState.GetState(new ClueId("clue-c")));
            CollectionAssert.AreEqual(new[] { false }, fx.ClueAnswered.ConvertAll(e => e.WasCorrect),
                "오답은 신뢰를 안 깎는 대신 ClueAnsweredEvent(false)로만 흔적을 남긴다.");
        }

        [Test]
        public void 오답_서브체인은_기존_Next_체인으로_메인_줄기에_합류한다()
        {
            var fx = ClueSelectionFixture();

            fx.Progressor.SelectClue(new ClueId("clue-c"));      // q → wrong-1
            fx.Progressor.Select(new ChoiceId("w1"));            // wrong-1 → wrong-2
            fx.Progressor.Select(new ChoiceId("w2"));            // wrong-2 → merge

            Assert.AreEqual(new DialogueLineId("merge"), fx.Progressor.CurrentLineId);
        }

        [Test]
        public void 들고_있지_않은_단서로는_답할_수_없다()
        {
            var fx = ClueSelectionFixture();
            fx.ClueState.SetState(new ClueId("clue-a"), ClueState.UsedInDialogue);

            Assert.AreEqual(
                ChoiceSelectionOutcome.Rejected,
                fx.Progressor.SelectClue(new ClueId("clue-a")).Outcome);
        }

        [Test]
        public void 단서로_답하지_않고_넘어가면_오답_분기로_간다()
        {
            var fx = ClueSelectionFixture();

            var result = fx.Progressor.SkipClueSelection();

            Assert.AreEqual(ChoiceSelectionOutcome.Advanced, result.Outcome);
            Assert.AreEqual(new DialogueLineId("wrong-1"), fx.Progressor.CurrentLineId);
            Assert.AreEqual(ClueState.Collected, fx.ClueState.GetState(new ClueId("clue-a")), "넘어가면 단서를 소모하지 않는다.");
            CollectionAssert.AreEqual(new[] { false }, fx.ClueAnswered.ConvertAll(e => e.WasCorrect),
                "넘어가기도 정답이 아니므로 ClueAnsweredEvent(false)다.");
        }

        [Test]
        public void TextChoice_줄에서는_SelectClue가_거부된다()
        {
            var fx = new Fixture("line-1", new[] { Line("line-1", Choice("a", true)) },
                new[] { ClueDef("clue-a") });
            fx.ClueState.SetState(new ClueId("clue-a"), ClueState.Collected);

            Assert.AreEqual(
                ChoiceSelectionOutcome.Rejected, fx.Progressor.SelectClue(new ClueId("clue-a")).Outcome);
            CollectionAssert.IsEmpty(fx.Progressor.SelectableClues());
        }
    }
}
