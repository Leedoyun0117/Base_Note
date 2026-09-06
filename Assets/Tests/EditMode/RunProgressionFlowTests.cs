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

        private sealed class FakeMask : ICensorMaskFormatter
        {
            public string FormatMask(MemoryColor color) => $"<{color}>";
        }

        private sealed class World
        {
            public readonly EventBus Bus = new EventBus(new NoOpEventExceptionHandler());
            public readonly TrustGauge Trust;
            public readonly StepVisibilityPolicy Visibility = new StepVisibilityPolicy(VisibilityTable);
            public readonly CenteredClueAccessPolicy Access = new CenteredClueAccessPolicy();
            public readonly ExtractedMemoryStore Memories = new ExtractedMemoryStore();
            public readonly PlayerInventory Inventory =
                new PlayerInventory(new InventorySettings(16), new SharedSlotInventoryPolicy());
            public readonly HiromiWallet Hiromi;
            public readonly CensorUnlockLog CensorLog = new CensorUnlockLog();
            public readonly ClueStateStore ClueState;
            public readonly ClueCollectionProcessor Collection;
            public readonly ExtractionProcessor Extraction;
            public readonly CensorUnlockProcessor CensorUnlock;
            public readonly DialogueProgressor Dialogue;
            public readonly RunProgressor Run;
            public readonly List<RoomFailedEvent> Failed = new List<RoomFailedEvent>();
            public readonly List<RoomClearedEvent> Cleared = new List<RoomClearedEvent>();

            public World(RunDefinition run, IReadOnlyList<CluePlacement> placements)
            {
                Trust = new TrustGauge(run.StartingTrust, Bus);
                Hiromi = new HiromiWallet(run.StartingHiromi, Bus);
                ClueState = new ClueStateStore(run.Rooms, Bus);
                var tracker = new MemoryRoomClueTracker(placements);
                var requiredTags = new CensorKeyRequiredTagMap(run);

                Collection = new ClueCollectionProcessor(
                    ClueState, Access, Trust, Visibility, tracker, Inventory, Bus);
                Extraction = new ExtractionProcessor(Hiromi, ClueState, Memories, tracker, Bus);
                CensorUnlock = new CensorUnlockProcessor(Memories, CensorLog, requiredTags, Bus);
                Dialogue = new DialogueProgressor(run.Rooms, CensorLog, ClueState, Trust, Bus);
                var _ = new RoomCompletionArbiter(Trust, Bus);
                Run = new RunProgressor(run.Rooms, Bus);

                Bus.Subscribe<RoomFailedEvent>(Failed.Add);
                Bus.Subscribe<RoomClearedEvent>(Cleared.Add);
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

        // ── 시나리오 2: 방을 실패해도 다음 방으로 넘어가고, 런 자원은 유지된다 ──

        [Test]
        public void 방_실패로_넘어가도_추출된_기억_자원_검열해금은_유지되고_신뢰만_리셋된다()
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
                new[] { Line("r2-line", "우리가 [[B:k1:해변의 집]]에서.", Choice("done", true)) });

            var run = new RunDefinition(
                new[] { room1, room2, Room("room-3") }, startingTrust: 3, startingHiromi: 18,
                censorKeyTagRequirements: new[]
                {
                    new CensorKeyTagRequirement(new CensorKey("k1"), new[] { new ClueTag("beachHouse") }),
                });
            var world = new World(run, new[]
            {
                new CluePlacement(new MemoryRoomId("room-1"), r1Clue),
                new CluePlacement(new MemoryRoomId("room-2"), r2Clue),
            });
            world.Run.Start();

            // room-1에서 단서를 수집·추출해 기억 하나를 얻고, 히로민 9를 쓴다.
            Assert.IsTrue(world.Collection.Collect(new ClueId("r1-blue")).Succeeded);
            Assert.IsTrue(world.Extraction.Extract(new ClueId("r1-blue")).Succeeded);
            Assert.IsTrue(world.Memories.TryGet(new ClueId("r1-blue"), out _));
            Assert.AreEqual(9, world.Hiromi.Remaining);

            // 그 기억을 제시해 room-2 대사의 검열 k1을 미리 푼다.
            Assert.IsTrue(world.CensorUnlock.Unlock(new CensorKey("k1"), new ClueId("r1-blue")).Succeeded);
            Assert.IsFalse(world.Memories.TryGet(new ClueId("r1-blue"), out _), "제시한 기억은 소모된다.");
            Assert.IsTrue(world.CensorLog.IsRevealed(new CensorKey("k1")));

            // room-1을 오답 3번으로 실패시킨다.
            world.Dialogue.Select(new ChoiceId("wrong"));
            world.Dialogue.Select(new ChoiceId("wrong"));
            world.Dialogue.Select(new ChoiceId("wrong"));

            Assert.AreEqual(1, world.Failed.Count);
            Assert.AreEqual(new MemoryRoomId("room-1"), world.Failed[0].RoomId);

            // room-2로 넘어왔다. 신뢰는 3으로 리셋, 히로민·해금 기록은 그대로.
            Assert.AreEqual(new DialogueLineId("r2-line"), world.Dialogue.CurrentLineId);
            Assert.AreEqual(3, world.Trust.Current);
            Assert.AreEqual(9, world.Hiromi.Remaining);
            Assert.IsTrue(world.CensorLog.IsRevealed(new CensorKey("k1")));
            Assert.AreEqual(ClueState.Available, world.ClueState.GetState(new ClueId("r2-clue")));

            // 실패한 방에서 못 얻은 빨간 단서의 기억은 그대로 없다.
            Assert.IsFalse(world.Memories.TryGet(new ClueId("r2-clue"), out _));
        }
    }
}
