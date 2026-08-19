using System.Collections.Generic;
using GameName.Core.Ampoules;
using GameName.Core.Clues;
using GameName.Core.Emotions;
using GameName.Core.Inventory;
using GameName.Core.MemoryRooms;
using GameName.UI.Shared;
using NUnit.Framework;

namespace GameName.UI.Tests.EditMode
{
    public class ClueInventoryFilterTests
    {
        [Test]
        public void 앰플은_분석_대상_목록에_나타나지_않는다()
        {
            var clue = new ClueInfo(
                new ClueId("clue-1"), ClueKind.Poster, new CluePositionRatio(0.5f),
                new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Love, 5) }));
            var ampoule = new Ampoule(
                new AmpouleId("ampoule-1"), new MemoryRoomId("room-1"),
                new Scent(EmotionType.Joy, new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Love, 5) })));

            var items = new List<IInventoryItem> { clue, ampoule };

            var result = ClueInventoryFilter.OnlyClues(items);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(clue.Id, result[0].Id);
        }

        [Test]
        public void 단서가_없으면_빈_목록을_돌려준다()
        {
            var ampoule = new Ampoule(
                new AmpouleId("ampoule-1"), new MemoryRoomId("room-1"),
                new Scent(EmotionType.Joy, new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Love, 5) })));

            var result = ClueInventoryFilter.OnlyClues(new List<IInventoryItem> { ampoule });

            Assert.AreEqual(0, result.Count);
        }
    }
}
