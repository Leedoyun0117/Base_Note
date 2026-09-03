using System.Collections.Generic;
using GameName.Core.Events;
using GameName.Core.MemoryRooms;
using GameName.Core.Progression;
using GameName.Core.Trust;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    // 방은 대화 종료로 클리어되거나 신뢰 0으로 실패한다. 둘이 겹치면 실패가 이긴다.
    public class RoomCompletionArbiterTests
    {
        private sealed class MutableTrust : ITrustReader
        {
            public int Current { get; set; }
        }

        private static readonly MemoryRoomId TheRoom = new MemoryRoomId("room-1");

        private sealed class Fixture
        {
            public readonly EventBus Bus = new EventBus(new NoOpEventExceptionHandler());
            public readonly MutableTrust Trust = new MutableTrust { Current = 3 };
            public readonly List<RoomClearedEvent> Cleared = new List<RoomClearedEvent>();
            public readonly List<RoomFailedEvent> Failed = new List<RoomFailedEvent>();

            public Fixture()
            {
                var _ = new RoomCompletionArbiter(Trust, Bus);
                Bus.Subscribe<RoomClearedEvent>(Cleared.Add);
                Bus.Subscribe<RoomFailedEvent>(Failed.Add);
                Bus.Publish(new RoomStartedEvent(TheRoom, 0));
            }
        }

        [Test]
        public void 신뢰가_남은_채_대화가_끝나면_클리어다()
        {
            var fx = new Fixture();

            fx.Bus.Publish(new DialogueEndedEvent(TheRoom));

            Assert.AreEqual(1, fx.Cleared.Count);
            Assert.AreEqual(TheRoom, fx.Cleared[0].RoomId);
            CollectionAssert.IsEmpty(fx.Failed);
        }

        [Test]
        public void 신뢰가_0에_닿으면_즉시_실패다()
        {
            var fx = new Fixture();
            fx.Trust.Current = 0;

            fx.Bus.Publish(new TrustChangedEvent(1, 0));

            Assert.AreEqual(1, fx.Failed.Count);
            CollectionAssert.IsEmpty(fx.Cleared);
        }

        [Test]
        public void 신뢰_0과_대화_종료가_겹치면_실패가_이긴다()
        {
            var fx = new Fixture();
            fx.Trust.Current = 0;

            fx.Bus.Publish(new TrustChangedEvent(1, 0)); // 먼저 동기 발행되는 쪽
            fx.Bus.Publish(new DialogueEndedEvent(TheRoom));

            Assert.AreEqual(1, fx.Failed.Count);
            CollectionAssert.IsEmpty(fx.Cleared);
        }

        [Test]
        public void 대화_종료_시점에_신뢰가_0이면_클리어가_아니라_실패다()
        {
            var fx = new Fixture();
            fx.Trust.Current = 0;

            // 신뢰가 다른 경로로 0이 되어 TrustChangedEvent를 못 봤더라도,
            // 대화 종료 시점에 신뢰를 다시 확인한다.
            fx.Bus.Publish(new DialogueEndedEvent(TheRoom));

            Assert.AreEqual(1, fx.Failed.Count);
            CollectionAssert.IsEmpty(fx.Cleared);
        }

        [Test]
        public void 한_방은_정확히_한_번만_닫힌다()
        {
            var fx = new Fixture();

            fx.Bus.Publish(new DialogueEndedEvent(TheRoom));
            fx.Bus.Publish(new DialogueEndedEvent(TheRoom));
            fx.Trust.Current = 0;
            fx.Bus.Publish(new TrustChangedEvent(1, 0));

            Assert.AreEqual(1, fx.Cleared.Count);
            CollectionAssert.IsEmpty(fx.Failed);
        }

        [Test]
        public void 방이_다시_시작되면_래치가_풀려_다음_판정을_받는다()
        {
            var fx = new Fixture();
            fx.Bus.Publish(new DialogueEndedEvent(TheRoom));

            var room2 = new MemoryRoomId("room-2");
            fx.Bus.Publish(new RoomStartedEvent(room2, 1));
            fx.Bus.Publish(new DialogueEndedEvent(room2));

            Assert.AreEqual(2, fx.Cleared.Count);
            Assert.AreEqual(room2, fx.Cleared[1].RoomId);
        }
    }
}
