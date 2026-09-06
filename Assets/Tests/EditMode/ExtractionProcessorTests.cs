using System;
using System.Collections.Generic;
using GameName.Core.Authoring;
using GameName.Core.Clues;
using GameName.Core.Dialogue;
using GameName.Core.Events;
using GameName.Core.Extraction;
using GameName.Core.Hiromi;
using GameName.Core.Memories;
using GameName.Core.MemoryRooms;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    // 추출은 히로민 9를 쓰고 추출된 기억 하나를 남긴다 — 부분 성공은 없다.
    public class ExtractionProcessorTests
    {
        private static readonly MemoryRoomId TheRoom = new MemoryRoomId("room-1");
        private static readonly ClueId TheClue = new ClueId("clue-1");

        private sealed class Fixture
        {
            public readonly EventBus Bus = new EventBus(new NoOpEventExceptionHandler());
            public readonly HiromiWallet Hiromi;
            public readonly ClueStateStore State;
            public readonly ExtractedMemoryStore Memories = new ExtractedMemoryStore();
            public readonly ExtractionProcessor Processor;
            public readonly List<ClueExtractedEvent> Extracted = new List<ClueExtractedEvent>();
            public readonly List<MemoryColorRevealedEvent> Revealed = new List<MemoryColorRevealedEvent>();

            public Fixture(int startingHiromi, MemoryColor hiddenColor)
            {
                var def = new ClueDefinition(
                    TheClue, ClueKind.Poster, "단서", new CluePositionRatio(0.5f), hiddenColor);
                var room = new RoomDefinition(
                    TheRoom, new[] { def }, null, Array.Empty<DialogueLineDefinition>());

                Hiromi = new HiromiWallet(startingHiromi, Bus);
                State = new ClueStateStore(new[] { room }, Bus);
                State.Seed(0);
                var tracker = new MemoryRoomClueTracker(new[] { new CluePlacement(TheRoom, def) });
                Processor = new ExtractionProcessor(Hiromi, State, Memories, tracker, Bus);

                Bus.Subscribe<ClueExtractedEvent>(Extracted.Add);
                Bus.Subscribe<MemoryColorRevealedEvent>(Revealed.Add);
            }

            public void SetState(ClueState s) => State.SetState(TheClue, s);
            public ExtractionResult Extract() => Processor.Extract(TheClue);
            public bool HasMemoryOfColor(MemoryColor color) =>
                Memories.TryGet(TheClue, out var memory) && memory.Color == color;
        }

        [Test]
        public void 수집한_단서를_추출하면_히로민_9_소모_기억_하나_획득_두_사건()
        {
            var fx = new Fixture(startingHiromi: 18, hiddenColor: MemoryColor.Blue);
            fx.SetState(ClueState.Collected);

            var result = fx.Extract();

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(9, fx.Hiromi.Remaining);
            Assert.AreEqual(ClueState.Extracted, fx.State.GetState(TheClue));
            Assert.IsTrue(fx.HasMemoryOfColor(MemoryColor.Blue));
            Assert.AreEqual(1, fx.Extracted.Count);
            Assert.AreEqual(1, fx.Revealed.Count);
            Assert.AreEqual(MemoryColor.Blue, fx.Revealed[0].Color);
        }

        [Test]
        public void 추출된_기억은_출처_단서의_태그를_그대로_옮겨_담는다()
        {
            var tag = new ClueTag("room1.blanket");
            var def = new ClueDefinition(
                TheClue, ClueKind.Poster, "단서", new CluePositionRatio(0.5f), MemoryColor.Blue, new[] { tag });
            var room = new RoomDefinition(TheRoom, new[] { def }, null, Array.Empty<DialogueLineDefinition>());
            var bus = new EventBus(new NoOpEventExceptionHandler());
            var state = new ClueStateStore(new[] { room }, bus);
            state.Seed(0);
            var tracker = new MemoryRoomClueTracker(new[] { new CluePlacement(TheRoom, def) });
            var memories = new ExtractedMemoryStore();
            var processor = new ExtractionProcessor(new HiromiWallet(9, bus), state, memories, tracker, bus);

            state.SetState(TheClue, ClueState.Collected);
            processor.Extract(TheClue);

            Assert.IsTrue(memories.TryGet(TheClue, out var memory));
            CollectionAssert.AreEqual(new[] { tag }, memory.Tags);
        }

        [Test]
        public void 히로민이_모자라면_상태도_사건도_전혀_바뀌지_않는다()
        {
            var fx = new Fixture(startingHiromi: 0, hiddenColor: MemoryColor.Red);
            fx.SetState(ClueState.Collected);

            var result = fx.Extract();

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ExtractionFailureReason.ResourceExhausted, result.FailureReason);
            Assert.AreEqual(ClueState.Collected, fx.State.GetState(TheClue));
            Assert.IsFalse(fx.Memories.TryGet(TheClue, out _));
            CollectionAssert.IsEmpty(fx.Extracted);
            CollectionAssert.IsEmpty(fx.Revealed);
        }

        [Test]
        public void 아직_수집하지_않은_단서는_추출할_수_없다()
        {
            var fx = new Fixture(startingHiromi: 27, hiddenColor: MemoryColor.Red);

            var result = fx.Extract();

            Assert.AreEqual(ExtractionFailureReason.NotCollected, result.FailureReason);
            Assert.AreEqual(27, fx.Hiromi.Remaining);
        }

        [Test]
        public void 대화에_이미_쓴_단서는_추출할_수_없다()
        {
            var fx = new Fixture(startingHiromi: 27, hiddenColor: MemoryColor.Red);
            fx.SetState(ClueState.Collected);
            fx.SetState(ClueState.UsedInDialogue);

            var result = fx.Extract();

            Assert.AreEqual(ExtractionFailureReason.AlreadyUsedInDialogue, result.FailureReason);
            Assert.AreEqual(27, fx.Hiromi.Remaining);
            CollectionAssert.IsEmpty(fx.Extracted);
        }

        [Test]
        public void 이미_추출한_단서를_또_추출하려_하면_거부된다()
        {
            var fx = new Fixture(startingHiromi: 27, hiddenColor: MemoryColor.Green);
            fx.SetState(ClueState.Collected);
            fx.Extract();

            var again = fx.Extract();

            Assert.AreEqual(ExtractionFailureReason.AlreadyExtracted, again.FailureReason);
            Assert.AreEqual(18, fx.Hiromi.Remaining);
            Assert.IsTrue(fx.HasMemoryOfColor(MemoryColor.Green));
        }
    }
}
