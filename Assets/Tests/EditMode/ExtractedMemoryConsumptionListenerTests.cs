using System;
using System.Collections.Generic;
using GameName.Core.Clues;
using GameName.Core.Events;
using GameName.Core.Memories;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    // 대화의 답으로 내민 단서에 딸린 추출 기억을 그 자리에서 소모한다.
    // 추출하지 않은 단서면 저장소에 없어 아무 일도 하지 않는다.
    public class ExtractedMemoryConsumptionListenerTests
    {
        private sealed class Fixture
        {
            public readonly EventBus Bus = new EventBus(new NoOpEventExceptionHandler());
            public readonly ExtractedMemoryStore Memories = new ExtractedMemoryStore();
            public readonly List<ExtractedMemoryConsumedEvent> Consumed =
                new List<ExtractedMemoryConsumedEvent>();

            // ReSharper disable once NotAccessedField.Local — 구독을 살려 두기 위한 보관.
            private readonly ExtractedMemoryConsumptionListener _listener;

            public Fixture()
            {
                _listener = new ExtractedMemoryConsumptionListener(Memories, Bus);
                Bus.Subscribe<ExtractedMemoryConsumedEvent>(Consumed.Add);
            }

            public void AddMemory(string clueId, MemoryColor color) =>
                Memories.Add(new ExtractedMemory(new ClueId(clueId), color, Array.Empty<ClueTag>()));
        }

        [Test]
        public void 답으로_낸_단서의_추출_기억이_소모되고_사건이_난다()
        {
            var fx = new Fixture();
            fx.AddMemory("clue-a", MemoryColor.Green);

            fx.Bus.Publish(new ClueUsedInDialogueEvent(new ClueId("clue-a")));

            Assert.IsFalse(fx.Memories.TryGet(new ClueId("clue-a"), out _));
            Assert.AreEqual(1, fx.Consumed.Count);
            Assert.AreEqual(new ClueId("clue-a"), fx.Consumed[0].Source);
            Assert.AreEqual(MemoryColor.Green, fx.Consumed[0].Color);
        }

        [Test]
        public void 추출하지_않은_단서를_답으로_내면_아무_일도_없다()
        {
            var fx = new Fixture();
            fx.AddMemory("clue-a", MemoryColor.Green);

            fx.Bus.Publish(new ClueUsedInDialogueEvent(new ClueId("clue-b")));

            Assert.IsTrue(fx.Memories.TryGet(new ClueId("clue-a"), out _), "다른 단서의 기억은 그대로다.");
            CollectionAssert.IsEmpty(fx.Consumed);
        }

        [Test]
        public void 답변_사건_자체는_기억을_소모하지_않는다()
        {
            var fx = new Fixture();
            fx.AddMemory("clue-a", MemoryColor.Green);

            // 넘어가기는 ClueAnsweredEvent만 내고 ClueUsedInDialogueEvent는 안 낸다.
            fx.Bus.Publish(new ClueAnsweredEvent(MatchGrade.None));

            Assert.IsTrue(fx.Memories.TryGet(new ClueId("clue-a"), out _));
            CollectionAssert.IsEmpty(fx.Consumed);
        }
    }
}
