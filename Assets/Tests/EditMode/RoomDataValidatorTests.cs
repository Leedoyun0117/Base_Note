using System.Linq;
using GameName.Core.Clues;
using GameName.Core.Emotions;
using GameName.Core.MemoryRooms;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    public class RoomDataValidatorTests
    {
        // 최소 간격은 규칙이 스스로 정하지 않고 주입받는다 — 얼마나 떨어져야
        // 충분한지는 단서 크기와 방 길이에 달린 표시 쪽 사정이다.
        private const float MinimumSeparation = 0.1f;

        private static RoomDataValidator MakeValidator() =>
            new RoomDataValidator(new IRoomDataConsistencyRule[]
            {
                new AnswerHasNoZeroIntensityRule(),
                new ClueTrueCompositionMatchesAnswerRule(),
                new CluePositionsAreSeparatedRule(MinimumSeparation),
            });

        private static MemoryRoomData MakeRoomData(params ClueDefinition[] clues)
        {
            var answerScent = new Scent(EmotionType.Joy, new EmotionBlend(new[]
            {
                new EmotionBlendEntry(EmotionType.Love, 5),
            }));

            return new MemoryRoomData(new MemoryRoomAnswer(new MemoryRoomId("room-1"), answerScent), clues);
        }

        private static ClueDefinition MakeClue(string id, float position, ClueKind kind) =>
            new ClueDefinition(
                new ClueId(id), kind, new CluePositionRatio(position),
                apparentComposition: new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Love, 5) }),
                trueComposition: new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Love, 5) }));

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
                ClueKind.Poster,
                new CluePositionRatio(0.3f),
                apparentComposition: new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Love, 5) }),
                trueComposition: new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Anger, 3) }));

            var data = new MemoryRoomData(answer, new[] { clue });

            var issues = MakeValidator().Validate(data);

            Assert.IsTrue(issues.Any(i => i.Severity == RoomDataIssueSeverity.Warning));
        }

        // 겹치면 자동으로 밀어내지 않고 저작 시점에 알리기만 한다 — 코드가
        // 자리를 옮기기 시작하면 "기획이 정한 자리에 그대로 놓인다"는 전제가
        // 다시 무너지기 때문이다.
        [Test]
        public void 같은_종류의_단서가_너무_가까우면_경고를_잡아낸다()
        {
            var data = MakeRoomData(
                MakeClue("clue-1", 0.50f, ClueKind.FloorObject),
                MakeClue("clue-2", 0.55f, ClueKind.FloorObject));

            var issues = MakeValidator().Validate(data);

            Assert.IsTrue(issues.Any(i => i.Severity == RoomDataIssueSeverity.Warning));
        }

        [Test]
        public void 종류가_다르면_같은_자리라도_경고하지_않는다()
        {
            // 포스터는 벽 높이, 바닥 물건은 바닥이라 가로가 같아도 겹치지 않는다
            // — 오히려 "창문 아래 떨어진 반지" 같은 배치는 의도해서 만든다.
            var data = MakeRoomData(
                MakeClue("clue-poster", 0.5f, ClueKind.Poster),
                MakeClue("clue-object", 0.5f, ClueKind.FloorObject));

            var issues = MakeValidator().Validate(data);

            Assert.IsEmpty(issues);
        }

        [Test]
        public void 충분히_떨어져_있으면_경고하지_않는다()
        {
            var data = MakeRoomData(
                MakeClue("clue-1", 0.2f, ClueKind.FloorObject),
                MakeClue("clue-2", 0.8f, ClueKind.FloorObject));

            var issues = MakeValidator().Validate(data);

            Assert.IsEmpty(issues);
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
                ClueKind.Poster,
                new CluePositionRatio(0.3f),
                apparentComposition: new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Love, 5) }),
                trueComposition: new EmotionBlend(new[] { new EmotionBlendEntry(EmotionType.Love, 5) }));

            var data = new MemoryRoomData(answer, new[] { clue });

            var issues = MakeValidator().Validate(data);

            Assert.IsEmpty(issues);
        }
    }
}
