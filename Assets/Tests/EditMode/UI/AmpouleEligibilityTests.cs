using GameName.Core.MemoryRooms;
using GameName.UI.Shared;
using NUnit.Framework;

namespace GameName.UI.Tests.EditMode
{
    public class AmpouleEligibilityTests
    {
        private static readonly MemoryRoomId RoomA = new MemoryRoomId("room-a");
        private static readonly MemoryRoomId RoomB = new MemoryRoomId("room-b");

        [Test]
        public void 목표_방이_다른_앰플은_시향_불가로_판정된다()
        {
            Assert.IsFalse(AmpouleEligibility.CanTestInCurrentRoom(RoomA, RoomB));
        }

        [Test]
        public void 목표_방이_같은_앰플은_시향_가능으로_판정된다()
        {
            Assert.IsTrue(AmpouleEligibility.CanTestInCurrentRoom(RoomA, RoomA));
        }

        [Test]
        public void 지금_있는_곳이_알_수_없는_방이면_시향_불가로_판정된다()
        {
            Assert.IsFalse(AmpouleEligibility.CanTestInCurrentRoom(RoomA, null));
        }
    }
}
