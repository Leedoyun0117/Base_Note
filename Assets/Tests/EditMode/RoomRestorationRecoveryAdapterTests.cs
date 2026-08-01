using GameName.Core.Events;
using GameName.Core.Mentality;
using GameName.Core.MemoryRooms;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    public class RoomRestorationRecoveryAdapterTests
    {
        [Test]
        public void 방_복원_이벤트가_발행되면_설정값만큼_정신력이_회복된다()
        {
            var eventBus = new EventBus(new NoOpEventExceptionHandler());
            var settings = new MentalityCostSettings(
                initialMentality: 50, maxMentality: 100,
                memoryRoomMoveCost: 1, basicAnalysisCost: 20, advancedAnalysisCost: 30,
                ampouleCraftingCost: 8, memoryRoomFullRestorationRecovery: 20);
            var gauge = new MentalityGauge(settings, eventBus);

            using (new RoomRestorationRecoveryAdapter(eventBus, gauge, settings))
            {
                eventBus.Publish(new MemoryRoomRestoredEvent(new MemoryRoomId("room-1")));

                Assert.AreEqual(70, gauge.CurrentValue);
            }
        }
    }
}
