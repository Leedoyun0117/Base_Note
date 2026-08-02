using System.Linq;
using GameName.Core.Clues;
using GameName.Core.Emotions;
using GameName.Core.MemoryRooms;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    public class RoomDataValidatorTests
    {
        private static RoomDataValidator MakeValidator() =>
            new RoomDataValidator(new IRoomDataConsistencyRule[]
            {
                new AnswerHasNoZeroIntensityRule(),
                new ClueTrueCompositionMatchesAnswerRule(),
            });

        [Test]
        public void 정답에_세기_0인_감정이_있으면_오류를_잡아낸다()
        {
            // EmotionBlendEntry 생성자는 세기 0을 막지만, 배열 기본값(default)이
            // 그 검증을 우회하는 상황(entries[1]을 채우지 않고 남겨 둠)을
            // 재현해 실제로 뚫릴 수 있는 구멍을 검증한다.
            var entries = new EmotionBlendEntry[2];
            entries[0] = new EmotionBlendEntry(EmotionType.Love, 5);

            var blend = new EmotionBlend(entries);
            var answerScent = new Scent(EmotionType.Joy, blend);
            var answer = new MemoryRoomAnswer(new MemoryRoomId("room-1"), answerScent);
            var data = new MemoryRoomData(answer, new ClueDefinition[0]);

            var issues = MakeValidator().Validate(data);

            Assert.IsTrue(issues.Any(i => i.Severity == RoomDataIssueSeverity.Error));
        }

        [Test]
        public void 단서의_실제_구성에_정답에_없는_감정이_있으면_경고를_잡아낸다()
        {
            var roomId = new MemoryRoomId("room-1");
            var answerScent = new Scent(EmotionType.Joy, new EmotionBlend(new[]
            {
                new EmotionBlendEntry(EmotionType.Love, 5),
            }));
            var answer = new MemoryRoomAnswer(roomId, answerScent);

            var clue = new ClueDefinition(
                new ClueId("clue-1"),
                roomId,
                apparentComposition: new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Love, 5) }),
                trueComposition: new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Anger, 3) }));

            var data = new MemoryRoomData(answer, new[] { clue });

            var issues = MakeValidator().Validate(data);

            Assert.IsTrue(issues.Any(i => i.Severity == RoomDataIssueSeverity.Warning));
        }

        [Test]
        public void 문제가_없으면_이슈가_없다()
        {
            var roomId = new MemoryRoomId("room-1");
            var answerScent = new Scent(EmotionType.Joy, new EmotionBlend(new[]
            {
                new EmotionBlendEntry(EmotionType.Love, 5),
            }));
            var answer = new MemoryRoomAnswer(roomId, answerScent);

            var clue = new ClueDefinition(
                new ClueId("clue-1"),
                roomId,
                apparentComposition: new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Love, 5) }),
                trueComposition: new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Love, 5) }));

            var data = new MemoryRoomData(answer, new[] { clue });

            var issues = MakeValidator().Validate(data);

            Assert.IsEmpty(issues);
        }
    }
}
