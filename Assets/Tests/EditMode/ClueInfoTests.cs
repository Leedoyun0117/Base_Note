using System.Linq;
using System.Reflection;
using GameName.Core.Clues;
using GameName.Core.Emotions;
using GameName.Core.MemoryRooms;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    // UI/인벤토리가 실제로 받는 타입(ClueInfo)에 진실 구성을 읽을 방법이 아예
    // 없다는 것을 "타입 수준"에서 증명하기 위한 테스트. 인스턴스를 만들어 어떤
    // 값이 나오는지 확인하는 방식으로는 "TrueComposition에 접근할 방법이
    // 없다"는 것을 증명할 수 없으므로(그 프로퍼티 자체가 없으니 애초에 읽는
    // 코드를 작성할 수 없다), 리플렉션으로 공개 표면 자체를 검사한다. 나중에
    // 누군가 실수로 ClueInfo에 진실 관련 프로퍼티를 추가하면 이 테스트가
    // 즉시 깨진다.
    public class ClueInfoTests
    {
        [Test]
        public void ClueInfo의_공개_프로퍼티에는_진실_구성이_없다()
        {
            var publicPropertyNames = typeof(ClueInfo)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => p.Name)
                .ToArray();

            CollectionAssert.DoesNotContain(publicPropertyNames, "TrueComposition");
            CollectionAssert.AreEquivalent(
                new[]
                {
                    nameof(ClueInfo.Id),
                    nameof(ClueInfo.RoomId),
                    nameof(ClueInfo.ApparentComposition),
                    nameof(ClueInfo.Category),
                },
                publicPropertyNames);
        }

        [Test]
        public void ClueDefinition에서_변환한_ClueInfo는_겉보기_구성만_담는다()
        {
            var apparent = new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Joy, 5) });
            var truth = new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Fear, 9) });
            var definition = new ClueDefinition(new ClueId("clue-1"), new MemoryRoomId("room-1"), apparent, truth);

            // info의 정적 타입(ClueInfo) 자체가 TrueComposition을 노출하지 않으므로,
            // "info.TrueComposition"과 같은 코드는 애초에 컴파일되지 않는다.
            // 컴파일이 안 된다는 사실 자체가 "타입 수준 차단"의 증거이며, 여기서는
            // 변환 결과가 겉보기 구성만 그대로 담고 있음을 확인한다.
            var info = definition.ToInfo();

            Assert.AreEqual(definition.Id, info.Id);
            Assert.AreEqual(definition.RoomId, info.RoomId);
            Assert.AreEqual(apparent, info.ApparentComposition);
        }
    }
}
