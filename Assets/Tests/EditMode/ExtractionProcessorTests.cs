using System;
using System.Collections.Generic;
using GameName.Core.Authoring;
using GameName.Core.Clues;
using GameName.Core.Dialogue;
using GameName.Core.Events;
using GameName.Core.Extraction;
using GameName.Core.Memories;
using GameName.Core.MemoryRooms;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    // 추출은 자원 1을 쓰고 색을 꺼낸다 — 부분 성공은 없다.
    public class ExtractionProcessorTests
    {
        private static readonly MemoryRoomId TheRoom = new MemoryRoomId("room-1");

        private sealed class Fixture
        {
            public readonly EventBus Bus = new EventBus(new NoOpEventExceptionHandler());
            public readonly ExtractionBudget Budget;
            public readonly ClueStateStore State;
            public readonly MemoryColorWallet Wallet = new MemoryColorWallet();
            public readonly ExtractionProcessor Processor;
            public readonly List<ClueExtractedEvent> Extracted = new List<ClueExtractedEvent>();
            public readonly List<MemoryColorRevealedEvent> Revealed = new List<MemoryColorRevealedEvent>();

            public Fixture(int budget, MemoryColor hiddenColor)
            {
                var def = new ClueDefinition(
                    new ClueId("clue-1"), ClueKind.Poster, "단서", new CluePositionRatio(0.5f), hiddenColor);
                var room = new RoomDefinition(
                    TheRoom, new[] { def }, null, Array.Empty<DialogueLineDefinition>());

                Budget = new ExtractionBudget(budget);
                State = new ClueStateStore(new[] { room }, Bus);
                State.Seed(0);
                var tracker = new MemoryRoomClueTracker(new[] { new CluePlacement(TheRoom, def) });
                Processor = new ExtractionProcessor(Budget, State, Wallet, tracker, Bus);

                Bus.Subscribe<ClueExtractedEvent>(Extracted.Add);
                Bus.Subscribe<MemoryColorRevealedEvent>(Revealed.Add);
            }

            public void SetState(ClueState s) => State.SetState(new ClueId("clue-1"), s);
            public ExtractionResult Extract() => Processor.Extract(new ClueId("clue-1"));
        }

        [Test]
        public void 수집한_단서를_추출하면_자원_1_소모_색_1_획득_두_사건()
        {
            var fx = new Fixture(budget: 2, hiddenColor: MemoryColor.Blue);
            fx.SetState(ClueState.Collected);

            var result = fx.Extract();

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(1, fx.Budget.Remaining);
            Assert.AreEqual(ClueState.Extracted, fx.State.GetState(new ClueId("clue-1")));
            Assert.AreEqual(1, fx.Wallet.GetCount(MemoryColor.Blue));
            Assert.AreEqual(1, fx.Extracted.Count);
            Assert.AreEqual(1, fx.Extracted[0].RemainingExtractions);
            Assert.AreEqual(1, fx.Revealed.Count);
            Assert.AreEqual(MemoryColor.Blue, fx.Revealed[0].Color);
        }

        [Test]
        public void 자원이_0이면_상태도_사건도_전혀_바뀌지_않는다()
        {
            var fx = new Fixture(budget: 0, hiddenColor: MemoryColor.Red);
            fx.SetState(ClueState.Collected);

            var result = fx.Extract();

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(ExtractionFailureReason.ResourceExhausted, result.FailureReason);
            Assert.AreEqual(ClueState.Collected, fx.State.GetState(new ClueId("clue-1")));
            Assert.AreEqual(0, fx.Wallet.GetCount(MemoryColor.Red));
            CollectionAssert.IsEmpty(fx.Extracted);
            CollectionAssert.IsEmpty(fx.Revealed);
        }

        [Test]
        public void 아직_수집하지_않은_단서는_추출할_수_없다()
        {
            var fx = new Fixture(budget: 3, hiddenColor: MemoryColor.Red);

            var result = fx.Extract();

            Assert.AreEqual(ExtractionFailureReason.NotCollected, result.FailureReason);
            Assert.AreEqual(3, fx.Budget.Remaining);
        }

        [Test]
        public void 대화에_이미_쓴_단서는_추출할_수_없다()
        {
            var fx = new Fixture(budget: 3, hiddenColor: MemoryColor.Red);
            fx.SetState(ClueState.Collected);
            fx.SetState(ClueState.UsedInDialogue);

            var result = fx.Extract();

            Assert.AreEqual(ExtractionFailureReason.AlreadyUsedInDialogue, result.FailureReason);
            Assert.AreEqual(3, fx.Budget.Remaining);
            CollectionAssert.IsEmpty(fx.Extracted);
        }

        [Test]
        public void 이미_추출한_단서를_또_추출하려_하면_거부된다()
        {
            var fx = new Fixture(budget: 3, hiddenColor: MemoryColor.Green);
            fx.SetState(ClueState.Collected);
            fx.Extract();

            var again = fx.Extract();

            Assert.AreEqual(ExtractionFailureReason.AlreadyExtracted, again.FailureReason);
            Assert.AreEqual(2, fx.Budget.Remaining);
            Assert.AreEqual(1, fx.Wallet.GetCount(MemoryColor.Green));
        }
    }
}
