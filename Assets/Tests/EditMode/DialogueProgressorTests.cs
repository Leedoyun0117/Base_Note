using System;
using System.Collections.Generic;
using GameName.Core.Authoring;
using GameName.Core.Clues;
using GameName.Core.Dialogue;
using GameName.Core.Events;
using GameName.Core.Memories;
using GameName.Core.MemoryRooms;
using GameName.Core.Mind;
using GameName.Core.Trust;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    // 표시 가능한 선택지 필터링은 검열/단서 판정에 위임한다. 신뢰도는 이제 이
    // 처리기가 깎지 않고(안정 축 이탈에 따라 별도 리스너가 깎는다), 읽기만 해서
    // 신뢰 0 뒤로는 입력을 받지 않는다.
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
            public readonly StabilityAxis Stability;
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
                Stability = new StabilityAxis(0, -100, 100, Bus);
                ClueState = new ClueStateStore(rooms, Bus);
                Progressor = new DialogueProgressor(
                    rooms, Censor, ClueState, Trust, new TagMatchGrader(), Stability, Bus);

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
        public void 오답_선택지도_이제_신뢰도를_직접_깎지_않는다()
        {
            var fx = new Fixture("line-1", new[]
            {
                Line("line-1", Choice("wrong", false, next: "line-1"), Choice("right", true)),
            });

            var result = fx.Progressor.Select(new ChoiceId("wrong"));

            // 신뢰는 안정 축 이탈로만 깎인다 — 이 처리기는 손대지 않는다.
            Assert.AreEqual(3, fx.Trust.Current);
            Assert.AreEqual(ChoiceSelectionOutcome.Advanced, result.Outcome);
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

            // 안정 축 이탈로 인내심이 바닥났다고 치고 게이지를 직접 0으로 민다.
            fx.Trust.Decrease(3);

            Assert.AreEqual(0, fx.Trust.Current);
            Assert.AreEqual(ChoiceSelectionOutcome.Ignored, fx.Progressor.Select(new ChoiceId("right")).Outcome);
        }

        // ── ClueSelection: 질문에 단서로 답하기 ─────────────────────────────

        private static ClueDefinition ClueDef(string id, params string[] tags) =>
            new ClueDefinition(new ClueId(id), ClueKind.FloorObject, id, new CluePositionRatio(0.5f), MemoryColor.Red,
                System.Array.ConvertAll(tags, t => new ClueTag(t)));

        private static DialogueLineDefinition ClueLine(
            string id, string[] requiredTags, string correctNext, string incorrectNext) =>
            DialogueLineDefinition.ClueSelection(
                new DialogueLineId(id), "화자", "무엇을 들고 있었어?",
                System.Array.ConvertAll(requiredTags, t => new ClueTag(t)),
                correctNext == null ? (DialogueLineId?)null : new DialogueLineId(correctNext),
                incorrectNext == null ? (DialogueLineId?)null : new DialogueLineId(incorrectNext));

        private static Fixture ClueSelectionFixture()
        {
            // clue-a와 clue-b는 같은 태그("answer")를 가진다 — 둘 중 어느 것을
            // 답으로 내도 정답이 되는 것이 태그 판정의 핵심이다. clue-c는 이
            // 태그가 없어 오답 서브체인으로 간다.
            var fx = new Fixture("q", new[]
            {
                ClueLine("q", new[] { "answer" }, "right", "wrong-1"),
                Line("right", Choice("r", true)),
                Line("wrong-1", Choice("w1", true, next: "wrong-2")),
                Line("wrong-2", Choice("w2", true, next: "merge")),
                Line("merge", Choice("m", true)),
            }, new[] { ClueDef("clue-a", "answer"), ClueDef("clue-b", "answer"), ClueDef("clue-c") });

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
                CollectionAssert.AreEqual(
                    new[] { MatchGrade.Exact }, fx.ClueAnswered.ConvertAll(e => e.Grade));
            }
        }

        [Test]
        public void 오답_단서는_오답_분기로_가고_신뢰는_이_처리기가_건드리지_않는다()
        {
            var fx = ClueSelectionFixture();

            var result = fx.Progressor.SelectClue(new ClueId("clue-c"));

            Assert.AreEqual(ChoiceSelectionOutcome.Advanced, result.Outcome);
            Assert.AreEqual(new DialogueLineId("wrong-1"), fx.Progressor.CurrentLineId);
            Assert.AreEqual(3, fx.Trust.Current, "신뢰는 안정 축 이탈로만 깎인다.");
            Assert.AreEqual(ClueState.UsedInDialogue, fx.ClueState.GetState(new ClueId("clue-c")));
            CollectionAssert.AreEqual(
                new[] { MatchGrade.None }, fx.ClueAnswered.ConvertAll(e => e.Grade));
        }

        [Test]
        public void 중심축이_어긋난_부분적합_답은_오답_분기로_간다()
        {
            // 질문은 중심축 "answer"를 요구한다. clue-side는 그 말을 곁축으로만
            // 가져 스치기는 하지만(부분적합) 중심축이 아니라 오답 분기로 가야 한다.
            var line = DialogueLineDefinition.ClueSelection(
                new DialogueLineId("q"), "화자", "?", new[] { new ClueTag("answer") },
                new DialogueLineId("right"), new DialogueLineId("wrong"));
            var sideClue = new ClueDefinition(
                new ClueId("clue-side"), ClueKind.FloorObject, "곁", new CluePositionRatio(0.5f),
                MemoryColor.Red, new[] { ClueTag.Center("other"), ClueTag.Sub("answer") });
            var fx = new Fixture("q", new[]
            {
                line, Line("right", Choice("r", true)), Line("wrong", Choice("w", true)),
            }, new[] { sideClue });
            fx.ClueState.SetState(new ClueId("clue-side"), ClueState.Collected);

            fx.Progressor.SelectClue(new ClueId("clue-side"));

            Assert.AreEqual(new DialogueLineId("wrong"), fx.Progressor.CurrentLineId);
            CollectionAssert.AreEqual(
                new[] { MatchGrade.Partial }, fx.ClueAnswered.ConvertAll(e => e.Grade));
        }

        // ── 피드백 대사가 안정 축을 민다 ──────────────────────────────────

        private static DialogueLineDefinition Feedback(string id, int stabilityDelta) =>
            new DialogueLineDefinition(
                new DialogueLineId(id), "화자", "", new[] { Choice("c", true) }, stabilityDelta);

        [Test]
        public void 정답_피드백_대사로_들어가면_그_줄의_안정_델타만큼_축이_움직인다()
        {
            var fx = new Fixture("q", new[]
            {
                ClueLine("q", new[] { "answer" }, "warm", "miss"),
                Feedback("warm", 15), Feedback("miss", -20),
            }, new[] { ClueDef("clue-a", "answer") });
            fx.ClueState.SetState(new ClueId("clue-a"), ClueState.Collected);

            Assert.AreEqual(0, fx.Stability.Position, "질문 줄은 축을 건드리지 않는다.");

            fx.Progressor.SelectClue(new ClueId("clue-a")); // 완전적합 → warm

            Assert.AreEqual(new DialogueLineId("warm"), fx.Progressor.CurrentLineId);
            Assert.AreEqual(15, fx.Stability.Position);
        }

        [Test]
        public void 오답_피드백_대사도_그_줄의_안정_델타를_적용한다()
        {
            var fx = new Fixture("q", new[]
            {
                ClueLine("q", new[] { "answer" }, "warm", "miss"),
                Feedback("warm", 15), Feedback("miss", -20),
            }, new[] { ClueDef("clue-c") }); // 태그 없음 → 무관 → miss
            fx.ClueState.SetState(new ClueId("clue-c"), ClueState.Collected);

            fx.Progressor.SelectClue(new ClueId("clue-c"));

            Assert.AreEqual(new DialogueLineId("miss"), fx.Progressor.CurrentLineId);
            Assert.AreEqual(-20, fx.Stability.Position);
        }

        [Test]
        public void 분기가_null이면_그_답으로_대화가_끝난다()
        {
            var fx = new Fixture("q", new[]
            {
                ClueLine("q", new[] { "answer" }, correctNext: null, incorrectNext: null),
            }, new[] { ClueDef("clue-a", "answer") });
            fx.ClueState.SetState(new ClueId("clue-a"), ClueState.Collected);

            var result = fx.Progressor.SelectClue(new ClueId("clue-a"));

            Assert.AreEqual(ChoiceSelectionOutcome.DialogueEnded, result.Outcome);
            Assert.IsNull(fx.Progressor.CurrentLine);
            Assert.AreEqual(1, fx.Ended.Count);
        }

        [Test]
        public void 넘어가기_분기가_null이면_넘어가기로_대화가_끝난다()
        {
            var fx = new Fixture("q", new[]
            {
                ClueLine("q", new[] { "answer" }, correctNext: null, incorrectNext: null),
            }, new[] { ClueDef("clue-a", "answer") });

            var result = fx.Progressor.SkipClueSelection();

            Assert.AreEqual(ChoiceSelectionOutcome.DialogueEnded, result.Outcome);
            Assert.AreEqual(1, fx.Ended.Count);
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
            CollectionAssert.AreEqual(new[] { MatchGrade.None }, fx.ClueAnswered.ConvertAll(e => e.Grade),
                "넘어가기는 답을 안 낸 것이므로 MatchGrade.None이다.");
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

        // ── 단서 누적: 다른 방에서 집은 단서도 지금 방 대화에 쓸 수 있다 ──────

        [Test]
        public void 다른_방에서_손에_든_단서도_지금_방의_답_목록에_나타난다()
        {
            var otherRoomClue = new ClueDefinition(
                new ClueId("clue-other"), ClueKind.FloorObject, "다른 방 단서",
                new CluePositionRatio(0.5f), MemoryColor.Red);
            var otherRoom = new RoomDefinition(
                new MemoryRoomId("room-0"), new[] { otherRoomClue }, null, Array.Empty<DialogueLineDefinition>());

            var line = DialogueLineDefinition.ClueSelection(
                new DialogueLineId("q"), "화자", "?", new[] { new ClueTag("answer") },
                new DialogueLineId("right"), new DialogueLineId("wrong"));
            var currentRoom = new RoomDefinition(
                TheRoom, Array.Empty<ClueDefinition>(), new DialogueLineId("q"),
                new[] { line, Line("right", Choice("r", true)), Line("wrong", Choice("w", true)) });

            var rooms = new[] { otherRoom, currentRoom };
            var bus = new EventBus(new NoOpEventExceptionHandler());
            var trust = new TrustGauge(3, bus);
            var censor = new CensorUnlockLog();
            var clueState = new ClueStateStore(rooms, bus);
            var progressor = new DialogueProgressor(
                rooms, censor, clueState, trust, new TagMatchGrader(),
                new StabilityAxis(0, -100, 100, bus), bus);

            // room-0을 먼저 방문해 단서를 손에 넣는다.
            bus.Publish(new RoomStartedEvent(otherRoom.Id, 0));
            clueState.SetState(new ClueId("clue-other"), ClueState.Collected);

            // 누적이므로 room-1(지금 방)로 넘어와도 그 단서는 손에 그대로 있다.
            bus.Publish(new RoomStartedEvent(currentRoom.Id, 1));

            var ids = new List<string>();
            foreach (var pair in progressor.SelectableClues())
                ids.Add(pair.Key.Value);

            CollectionAssert.Contains(ids, "clue-other");
        }

        [Test]
        public void 다른_방에서_수집한_단서로_지금_방_대화의_정답_분기로_갈_수_있다()
        {
            var otherRoomClue = new ClueDefinition(
                new ClueId("clue-other"), ClueKind.FloorObject, "다른 방 단서",
                new CluePositionRatio(0.5f), MemoryColor.Red, new[] { new ClueTag("answer") });
            var otherRoom = new RoomDefinition(
                new MemoryRoomId("room-0"), new[] { otherRoomClue }, null, Array.Empty<DialogueLineDefinition>());

            var line = DialogueLineDefinition.ClueSelection(
                new DialogueLineId("q"), "화자", "?", new[] { new ClueTag("answer") },
                new DialogueLineId("right"), new DialogueLineId("wrong"));
            var currentRoom = new RoomDefinition(
                TheRoom, Array.Empty<ClueDefinition>(), new DialogueLineId("q"),
                new[] { line, Line("right", Choice("r", true)), Line("wrong", Choice("w", true)) });

            var rooms = new[] { otherRoom, currentRoom };
            var bus = new EventBus(new NoOpEventExceptionHandler());
            var trust = new TrustGauge(3, bus);
            var censor = new CensorUnlockLog();
            var clueState = new ClueStateStore(rooms, bus);
            var progressor = new DialogueProgressor(
                rooms, censor, clueState, trust, new TagMatchGrader(),
                new StabilityAxis(0, -100, 100, bus), bus);

            bus.Publish(new RoomStartedEvent(otherRoom.Id, 0));
            clueState.SetState(new ClueId("clue-other"), ClueState.Collected);
            bus.Publish(new RoomStartedEvent(currentRoom.Id, 1));

            var result = progressor.SelectClue(new ClueId("clue-other"));

            Assert.AreEqual(ChoiceSelectionOutcome.Advanced, result.Outcome);
            Assert.AreEqual(new DialogueLineId("right"), progressor.CurrentLineId);
            Assert.AreEqual(ClueState.UsedInDialogue, clueState.GetState(new ClueId("clue-other")));
        }
    }
}
