using System;
using System.Collections.Generic;
using GameName.Core.Authoring;
using GameName.Core.Clues;
using GameName.Core.Dialogue;
using GameName.Core.Events;
using GameName.Core.Extraction;
using GameName.Core.Hiromi;
using GameName.Core.Inventory;
using GameName.Core.Memories;
using GameName.Core.MemoryRooms;
using GameName.Core.Mind;
using GameName.Core.Progression;
using GameName.Core.Trust;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    // 전체 그래프를 엮어, 2단계 규칙들이 한 판 안에서 서로 맞물려 도는지 본다.
    public class RunProgressionFlowTests
    {
        private static readonly Dictionary<int, float> VisibilityTable = new Dictionary<int, float>
        {
            { 3, 1.0f }, { 2, 0.75f }, { 1, 0.5f }, { 0, 0.5f },
        };

        private static ChoiceDefinition Choice(string id, bool correct, string next = null) =>
            new ChoiceDefinition(
                new ChoiceId(id), "", correct,
                next == null ? (DialogueLineId?)null : new DialogueLineId(next), ChoiceCondition.None);

        private static DialogueLineDefinition Line(string id, string text, params ChoiceDefinition[] choices) =>
            new DialogueLineDefinition(new DialogueLineId(id), "화자", text, choices);

        private sealed class World
        {
            public readonly EventBus Bus = new EventBus(new NoOpEventExceptionHandler());
            public readonly TrustGauge Trust;
            public readonly StabilityAxis Stability;
            public readonly StepVisibilityPolicy Visibility = new StepVisibilityPolicy(VisibilityTable);
            public readonly CenteredClueAccessPolicy Access = new CenteredClueAccessPolicy();
            public readonly ExtractedMemoryStore Memories = new ExtractedMemoryStore();
            public readonly PlayerInventory Inventory =
                new PlayerInventory(new InventorySettings(16), new SharedSlotInventoryPolicy());
            public readonly HiromiWallet Hiromi;
            public readonly ChanceTracker Chance;
            public readonly ClueStateStore ClueState;
            public readonly ClueCollectionProcessor Collection;
            public readonly ClueDiscardProcessor Discard;
            public readonly ExtractionProcessor Extraction;
            public readonly MemoryMoveProcessor MemoryMove;
            public readonly DialogueProgressor Dialogue;
            public readonly RunProgressor Run;
            public readonly List<RoomFailedEvent> Failed = new List<RoomFailedEvent>();
            public readonly List<RoomClearedEvent> Cleared = new List<RoomClearedEvent>();
            public readonly List<RunCompletedEvent> Completed = new List<RunCompletedEvent>();

            public World(RunDefinition run, IReadOnlyList<CluePlacement> placements)
            {
                Trust = new TrustGauge(run.StartingTrust, Bus);
                Stability = new StabilityAxis(
                    run.StartingStability, run.StabilityMin, run.StabilityMax, Bus);
                _ = new StabilityTrustErosionListener(
                    Stability, Trust, run.TrustErosionFreeBand, run.TrustErosionDivisor, Bus);
                Hiromi = new HiromiWallet(run.StartingHiromi, Bus);
                Chance = new ChanceTracker(run.StartingChance, Bus);
                _ = new ChanceExhaustionListener(Bus);
                ClueState = new ClueStateStore(run.Rooms, Bus);
                var tracker = new MemoryRoomClueTracker(placements);

                Collection = new ClueCollectionProcessor(
                    ClueState, Access, Trust, Visibility, tracker, Inventory, Bus);
                Discard = new ClueDiscardProcessor(ClueState, Bus);
                _ = new InventoryProjection(Inventory, tracker, Bus);
                _ = new RoomEntryInventoryClear(Inventory, Discard, Bus);
                Extraction = new ExtractionProcessor(Hiromi, ClueState, Memories, tracker, Bus);
                _ = new ExtractedMemoryConsumptionListener(Memories, Bus);
                Dialogue = new DialogueProgressor(
                    run.Rooms, ClueState, Trust,
                    new MemoryEffectResolver(new TagMatchGrader(), 20, 30),
                    new PsychologyTracker(run.StartingPsychology, Bus), Stability, Bus);
                // 조사 한도 0 = 방이 시작되면 곧장 대화 국면으로.
                _ = new RoomInvestigationCounter(new RoomPhaseCoordinator(Bus), 0, Bus);
                _ = new RoomCompletionArbiter(Trust, Bus);
                Run = new RunProgressor(run.Rooms, Bus);
                MemoryMove = new MemoryMoveProcessor(run.MoveHiromiCost, Hiromi, Chance, Run);
                // "다음으로" 버튼(= RoomClearedEvent)은 이동 비용을 치른다.
                _ = new RoomClearanceMoveListener(MemoryMove, Bus);

                Bus.Subscribe<RoomFailedEvent>(Failed.Add);
                Bus.Subscribe<RoomClearedEvent>(Cleared.Add);
                Bus.Subscribe<RunCompletedEvent>(Completed.Add);
            }
        }

        // ── 시나리오 1: 신뢰가 깎일수록 방이 좁아져 바깥 단서가 접근 불가해진다 ──

        [Test]
        public void 신뢰가_깎이는_각_단계에서_가시_비율과_단서_접근성이_함께_좁아진다()
        {
            var room1 = new RoomDefinition(
                new MemoryRoomId("room-1"), Array.Empty<ClueDefinition>(),
                null, Array.Empty<DialogueLineDefinition>());
            var run = new RunDefinition(
                new[] { room1, Room("room-2"), Room("room-3") }, startingTrust: 3, startingHiromi: 3);
            var world = new World(run, Array.Empty<CluePlacement>());
            world.Run.Start();

            // 경계에 정확히 걸친 자리(트러스트 2의 창은 [0.125, 0.875]).
            var onTrust2Edge = new CluePositionRatio(0.125f);

            Assert.AreEqual(1.0f, world.Visibility.GetVisibleRatio(world.Trust.Current));
            Assert.IsTrue(world.Access.IsAccessible(onTrust2Edge, world.Visibility.GetVisibleRatio(world.Trust.Current)));

            world.Trust.Decrease(1); // 3 → 2
            Assert.AreEqual(0.75f, world.Visibility.GetVisibleRatio(world.Trust.Current));
            Assert.IsTrue(world.Access.IsAccessible(onTrust2Edge, world.Visibility.GetVisibleRatio(world.Trust.Current)));

            world.Trust.Decrease(1); // 2 → 1
            Assert.AreEqual(0.5f, world.Visibility.GetVisibleRatio(world.Trust.Current));
            Assert.IsFalse(world.Access.IsAccessible(onTrust2Edge, world.Visibility.GetVisibleRatio(world.Trust.Current)));
        }

        private static RoomDefinition Room(string id) =>
            new RoomDefinition(
                new MemoryRoomId(id), Array.Empty<ClueDefinition>(), null,
                Array.Empty<DialogueLineDefinition>());

        // ── 시나리오 2: 방을 클리어해 넘어가면 런 자원(신뢰·기억 포함)이 전부 유지된다 ──

        [Test]
        public void 방을_클리어해_넘어가면_추출된_기억_자원_신뢰가_전부_유지된다()
        {
            var r1Clue = new ClueDefinition(
                new ClueId("r1-blue"), ClueKind.Poster, "r1 단서", new CluePositionRatio(0.5f), MemoryColor.Blue,
                new[] { new ClueTag("beachHouse") });
            var r2Clue = new ClueDefinition(
                new ClueId("r2-clue"), ClueKind.Poster, "r2 단서", new CluePositionRatio(0.5f), MemoryColor.Red);

            var room1 = new RoomDefinition(
                new MemoryRoomId("room-1"), new[] { r1Clue }, new DialogueLineId("r1-line"),
                new[]
                {
                    Line("r1-line", "",
                        Choice("wrong", false, next: "r1-line"),
                        Choice("done", true)),
                });
            var room2 = new RoomDefinition(
                new MemoryRoomId("room-2"), new[] { r2Clue }, new DialogueLineId("r2-line"),
                new[] { Line("r2-line", "우리가 그 해변의 집에서.", Choice("done", true)) });

            var run = new RunDefinition(
                new[] { room1, room2, Room("room-3") }, startingTrust: 3, startingHiromi: 30);
            var world = new World(run, new[]
            {
                new CluePlacement(new MemoryRoomId("room-1"), r1Clue),
                new CluePlacement(new MemoryRoomId("room-2"), r2Clue),
            });
            world.Run.Start();

            // room-1에서 단서를 수집·추출해 기억 하나를 얻는다(히로민 -9 → 21).
            Assert.IsTrue(world.Collection.Collect(new ClueId("r1-blue")).Succeeded);
            Assert.IsTrue(world.Extraction.Extract(new ClueId("r1-blue")).Succeeded);
            Assert.IsTrue(world.Memories.TryGet(new ClueId("r1-blue"), out _));
            Assert.AreEqual(21, world.Hiromi.Remaining);

            // room-1 대화를 완주한다 — 이제 방은 자동으로 닫히지 않는다.
            world.Dialogue.Select(new ChoiceId("done"));
            Assert.IsNull(world.Dialogue.CurrentLine, "대화는 끝났다.");
            CollectionAssert.IsEmpty(world.Cleared, "대화 완주만으로는 방이 닫히지 않는다.");

            // 화면의 "다음으로" 버튼: RoomClearedEvent → 이동 비용 15를 치르고 room-2로.
            world.Bus.Publish(new RoomClearedEvent(new MemoryRoomId("room-1")));

            Assert.AreEqual(1, world.Cleared.Count);
            CollectionAssert.IsEmpty(world.Completed, "아직 마지막 방이 아니다.");

            // room-2로 넘어왔다. 신뢰는 런 스코프라 그대로 3, 히로민은 이동
            // 비용만큼 빠지고(21 → 6), 추출한 기억도 그대로.
            Assert.AreEqual(new DialogueLineId("r2-line"), world.Dialogue.CurrentLineId);
            Assert.AreEqual(3, world.Trust.Current);
            Assert.AreEqual(6, world.Hiromi.Remaining);
            Assert.AreEqual(2, world.Chance.Remaining, "히로민이 넉넉하면 기회는 안 줄어든다.");
            Assert.IsTrue(world.Memories.TryGet(new ClueId("r1-blue"), out _), "추출한 기억은 런 전체에 걸쳐 남는다.");
            Assert.AreEqual(ClueState.Available, world.ClueState.GetState(new ClueId("r2-clue")));

            // 아직 못 얻은 빨간 단서의 기억은 그대로 없다.
            Assert.IsFalse(world.Memories.TryGet(new ClueId("r2-clue"), out _));
        }

        // ── 시나리오 3: 신뢰가 0에 닿으면 다음 방으로 가지 않고 런이 끝난다 ──

        [Test]
        public void 신뢰가_0에_닿으면_다음_방으로_가지_않고_런이_끝난다()
        {
            var room1 = new RoomDefinition(
                new MemoryRoomId("room-1"), Array.Empty<ClueDefinition>(), new DialogueLineId("r1-line"),
                new[]
                {
                    Line("r1-line", "",
                        Choice("wrong", false, next: "r1-line"),
                        Choice("done", true)),
                });
            var run = new RunDefinition(
                new[] { room1, Room("room-2"), Room("room-3") }, startingTrust: 3, startingHiromi: 3);
            var world = new World(run, Array.Empty<CluePlacement>());
            world.Run.Start();

            // 안정 축 이탈로 인내심이 바닥났다고 치고 게이지를 직접 0으로 민다
            // (이 World는 StabilityTrustErosionListener를 엮지 않는다 — 그 계산은
            //  StabilityTrustErosionListenerTests가 따로 커버한다).
            world.Trust.Decrease(3);

            Assert.AreEqual(0, world.Trust.Current);
            Assert.AreEqual(1, world.Failed.Count);
            Assert.AreEqual(1, world.Completed.Count, "신뢰 0이면 런이 끝나야 한다.");
            CollectionAssert.IsEmpty(world.Cleared);

            // room-2 대화는 시작되지 않았다.
            Assert.AreNotEqual(new DialogueLineId("r2-line"), world.Dialogue.CurrentLineId);
        }

        // ── 시나리오 4: 다음 방으로 넘어가면 손에 든 단서를 전부 두고 나온다 ──

        [Test]
        public void 다음_방으로_넘어가면_손에_든_단서가_전부_버려진다()
        {
            var r1Clue = new ClueDefinition(
                new ClueId("r1-clue"), ClueKind.Poster, "r1 단서", new CluePositionRatio(0.5f), MemoryColor.Blue,
                System.Array.Empty<ClueTag>());

            var room1 = new RoomDefinition(
                new MemoryRoomId("room-1"), new[] { r1Clue }, new DialogueLineId("r1-line"),
                new[] { Line("r1-line", "", Choice("done", true)) });
            var run = new RunDefinition(
                new[] { room1, Room("room-2"), Room("room-3") }, startingTrust: 3, startingHiromi: 3);
            var world = new World(run, new[] { new CluePlacement(new MemoryRoomId("room-1"), r1Clue) });
            world.Run.Start();

            Assert.IsTrue(world.Collection.Collect(new ClueId("r1-clue")).Succeeded);
            Assert.AreEqual(1, world.Inventory.Items.Count, "수집한 단서가 손에 들어왔다.");

            // 대화 완주 → "다음으로" 버튼(= RoomClearedEvent) → 방 2.
            world.Dialogue.Select(new ChoiceId("done"));
            world.Bus.Publish(new RoomClearedEvent(new MemoryRoomId("room-1")));

            Assert.IsEmpty(world.Inventory.Items, "방을 넘어오면 손에 든 단서는 다 두고 나온다.");
            Assert.AreEqual(ClueState.Discarded, world.ClueState.GetState(new ClueId("r1-clue")));
        }

        [Test]
        public void 대화_답으로_쓴_단서는_즉시_가방에서_빠진다()
        {
            var clue = new ClueDefinition(
                new ClueId("r1-clue"), ClueKind.Poster, "r1 단서", new CluePositionRatio(0.5f), MemoryColor.Blue,
                new[] { new ClueTag("answer") });

            var room1 = new RoomDefinition(
                new MemoryRoomId("room-1"), new[] { clue }, new DialogueLineId("q"),
                new[]
                {
                    DialogueLineDefinition.ClueSelection(
                        new DialogueLineId("q"), "화자", "뭘 쥐고 있었어?",
                        new[] { new ClueTag("answer") }, null, null),
                });
            var run = new RunDefinition(
                new[] { room1, Room("room-2"), Room("room-3") }, startingTrust: 3, startingHiromi: 3);
            var world = new World(run, new[] { new CluePlacement(new MemoryRoomId("room-1"), clue) });
            world.Run.Start();

            Assert.IsTrue(world.Collection.Collect(new ClueId("r1-clue")).Succeeded);
            Assert.AreEqual(1, world.Inventory.Items.Count);

            world.Dialogue.SelectClue(new ClueId("r1-clue"));

            Assert.IsEmpty(world.Inventory.Items, "대화에 답으로 낸 단서는 그 자리에서 손에서 나간다.");
            Assert.AreEqual(ClueState.UsedInDialogue, world.ClueState.GetState(new ClueId("r1-clue")));
        }

        // ── 시나리오: 피드백 대사가 안정 축을 밀고, 그게 다음 답변의 신뢰 침식으로 이어진다 ──

        [Test]
        public void 피드백_대사로_안정_축이_자유_폭을_넘으면_다음_답변부터_신뢰가_깎인다()
        {
            // q1을 넘기면 miss로 가고(안정 -25), q2를 넘길 땐 |위치| 25 > 자유 폭 20
            // 이라 그 답변이 신뢰를 1 깎는다. q1을 넘길 때는 아직 위치가 0이라 안 깎였다.
            var room1 = new RoomDefinition(
                new MemoryRoomId("room-1"), Array.Empty<ClueDefinition>(), new DialogueLineId("q1"),
                new[]
                {
                    DialogueLineDefinition.ClueSelection(
                        new DialogueLineId("q1"), "화자", "?", new[] { new ClueTag("x") },
                        null, new DialogueLineId("miss")),
                    new DialogueLineDefinition(
                        new DialogueLineId("miss"), "화자", "",
                        new[] { Choice("go", false, next: "q2") }, stabilityDelta: -25),
                    DialogueLineDefinition.ClueSelection(
                        new DialogueLineId("q2"), "화자", "?", new[] { new ClueTag("x") }, null, null),
                });
            var run = new RunDefinition(
                new[] { room1, Room("room-2"), Room("room-3") }, startingTrust: 3, startingHiromi: 3);
            var world = new World(run, Array.Empty<CluePlacement>());
            world.Run.Start();

            world.Dialogue.SkipClueSelection();       // q1 → miss
            Assert.AreEqual(-25, world.Stability.Position);
            Assert.AreEqual(3, world.Trust.Current, "q1을 넘길 땐 안정 축이 0이라 침식이 없다.");

            world.Dialogue.Select(new ChoiceId("go")); // miss → q2
            world.Dialogue.SkipClueSelection();        // q2 답변 — 이제 |위치| 25 > 20

            Assert.AreEqual(2, world.Trust.Current, "자유 폭을 넘긴 상태의 답변이 신뢰를 깎는다.");
        }
    }
}
