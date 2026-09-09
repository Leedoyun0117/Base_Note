using System.Collections.Generic;
using GameName.Core.Events;
using GameName.Core.MemoryRooms;
using GameName.Core.Progression;
using GameName.Core.Trust;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    // 방은 신뢰 0으로만 무너진다(RoomFailedEvent). 대화 완주로 떠나는 것은
    // 화면의 "다음으로" 버튼이 RoomClearedEvent를 내므로 이 심판은 관여하지 않는다.
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
        public void 신뢰가_0에_닿으면_실패다()
        {
            var fx = new Fixture();
            fx.Trust.Current = 0;

            fx.Bus.Publish(new TrustChangedEvent(1, 0));

            Assert.AreEqual(1, fx.Failed.Count);
            Assert.AreEqual(TheRoom, fx.Failed[0].RoomId);
            CollectionAssert.IsEmpty(fx.Cleared);
        }

        [Test]
        public void 대화_종료는_이_심판을_통하지_않는다()
        {
            var fx = new Fixture();

            fx.Bus.Publish(new DialogueEndedEvent(TheRoom));

            CollectionAssert.IsEmpty(fx.Cleared, "대화 완주는 '다음으로' 버튼이 처리한다.");
            CollectionAssert.IsEmpty(fx.Failed);
        }

        [Test]
        public void 신뢰_0_사건이_겹쳐_와도_한_번만_실패한다()
        {
            var fx = new Fixture();
            fx.Trust.Current = 0;

            fx.Bus.Publish(new TrustChangedEvent(1, 0));
            fx.Bus.Publish(new TrustChangedEvent(0, 0));

            Assert.AreEqual(1, fx.Failed.Count);
        }

        [Test]
        public void 방이_다시_시작되면_래치가_풀려_다음_판정을_받는다()
        {
            var fx = new Fixture();
            fx.Trust.Current = 0;
            fx.Bus.Publish(new TrustChangedEvent(1, 0)); // room-1 실패

            var room2 = new MemoryRoomId("room-2");
            fx.Trust.Current = 3;
            fx.Bus.Publish(new RoomStartedEvent(room2, 1));
            fx.Trust.Current = 0;
            fx.Bus.Publish(new TrustChangedEvent(1, 0)); // room-2 실패

            Assert.AreEqual(2, fx.Failed.Count);
            Assert.AreEqual(room2, fx.Failed[1].RoomId);
        }
    }
}
