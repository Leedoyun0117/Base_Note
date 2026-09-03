using System.Linq;
using System.Reflection;
using GameName.Core.Clues;
using GameName.Core.Memories;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    // UI/인벤토리가 실제로 받는 타입(ClueInfo)의 공개 표면이 저작 데이터
    // 전체가 아니라 "공개해도 되는 것"만으로 이뤄져 있다는 것을 타입 수준에서
    // 고정하는 테스트. 나중에 ClueDefinition에 감춰야 할 값이 추가되었을 때
    // 그것이 ClueInfo로 새어 나오면 이 테스트가 즉시 깨진다.
    public class ClueInfoTests
    {
        [Test]
        public void ClueInfo의_공개_프로퍼티는_공개해도_되는_것뿐이다()
        {
            var publicPropertyNames = typeof(ClueInfo)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => p.Name)
                .ToArray();

            CollectionAssert.AreEquivalent(
                new[]
                {
                    nameof(ClueInfo.Id),
                    nameof(ClueInfo.Kind),
                    nameof(ClueInfo.DisplayName),
                    nameof(ClueInfo.AuthoredPosition),
                    nameof(ClueInfo.Category),
                },
                publicPropertyNames);
        }

        [Test]
        public void ClueDefinition에서_변환한_ClueInfo는_같은_값을_그대로_담는다()
        {
            var definition = new ClueDefinition(
                new ClueId("clue-1"), ClueKind.Poster, "낡은 모포", new CluePositionRatio(0.25f), MemoryColor.Blue);

            var info = definition.ToInfo();

            Assert.AreEqual(definition.Id, info.Id);
            Assert.AreEqual(definition.Kind, info.Kind);
            Assert.AreEqual(definition.DisplayName, info.DisplayName);
            Assert.AreEqual(definition.AuthoredPosition, info.AuthoredPosition);
        }

        [Test]
        public void ClueInfo는_값이_아니라_식별자로_비교된다()
        {
            var a = new ClueInfo(new ClueId("clue-1"), ClueKind.Poster, "가", new CluePositionRatio(0.1f));
            var b = new ClueInfo(new ClueId("clue-1"), ClueKind.FloorObject, "나", new CluePositionRatio(0.9f));
            var other = new ClueInfo(new ClueId("clue-2"), ClueKind.Poster, "가", new CluePositionRatio(0.1f));

            Assert.AreEqual(a, b);
            Assert.AreNotEqual(a, other);
        }
    }
}
