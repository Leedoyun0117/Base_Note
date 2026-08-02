using System;
using System.Linq;
using System.Reflection;
using GameName.Core.Emotions;
using GameName.Core.FinalCrafting;
using GameName.Core.MemoryRooms;
using GameName.Core.Validation;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    // 섹션 1 검증: 최종 조향은 정신력을 전혀 요구/소모하지 않는다. "비용이
    // 0으로 설정되어 있다"가 아니라 "비용 개념 자체가 없다"로 구현했으므로,
    // 타입 계약(생성자가 정신력 관련 타입을 아예 받지 않음)과 실제 동작(여러 번
    // 조향해도 아무 정신력 게이지도 줄지 않음)을 함께 확인한다.
    public class FinalCraftingProcessorTests
    {
        private static readonly MemoryRoomId RoomId = new MemoryRoomId("room-1");

        private static (FinalCraftingProcessor Processor, FinalCraftingBoard Board) MakeFixture(int requiredTotal)
        {
            var board = new FinalCraftingBoard();
            var answerRepository = new MemoryRoomAnswerRepository(new[]
            {
                new MemoryRoomAnswer(
                    RoomId, new Scent(EmotionType.Joy, new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Love, requiredTotal) }))),
            });
            var compositionValidator = new ScentCompositionValidator(
                new EmotionCompositionPolicy(
                    minSupportingEmotionCount: 1, maxSupportingEmotionCount: 4, allowSupportingEmotionSameAsBase: false));

            var processor = new FinalCraftingProcessor(compositionValidator, answerRepository, board);
            return (processor, board);
        }

        [Test]
        public void 생성자는_정신력_관련_타입을_전혀_받지_않는다()
        {
            var parameterTypeNames = typeof(FinalCraftingProcessor)
                .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .SelectMany(c => c.GetParameters())
                .Select(p => p.ParameterType.FullName)
                .ToArray();

            Assert.IsFalse(parameterTypeNames.Any(n => n.Contains("Mentality")));
        }

        [Test]
        public void 여러_번_조향해도_최종_향만_바뀔_뿐_정신력_게이지는_전혀_참조되지_않는다()
        {
            var (processor, board) = MakeFixture(requiredTotal: 5);
            var scent = new Scent(EmotionType.Joy, new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Love, 5) }));

            for (var i = 0; i < 5; i++)
            {
                var result = processor.Craft(RoomId, scent);
                Assert.IsTrue(result.Succeeded);
            }

            Assert.IsTrue(board.TryGet(RoomId, out var confirmed));
            Assert.AreEqual(scent, confirmed);
        }

        [Test]
        public void 다시_조향하면_이전_값을_덮어쓴다()
        {
            var (processor, board) = MakeFixture(requiredTotal: 5);
            var first = new Scent(EmotionType.Joy, new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Love, 5) }));
            var second = new Scent(EmotionType.Joy, new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Fear, 5) }));

            processor.Craft(RoomId, first);
            processor.Craft(RoomId, second);

            board.TryGet(RoomId, out var confirmed);
            Assert.AreEqual(second, confirmed);
        }
    }
}
