using GameName.Core.Emotions;
using GameName.Core.Judging;
using GameName.Core.MemoryRooms;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    public class ScentJudgeTests
    {
        private static Scent MakeScent(EmotionType baseEmotion, params EmotionBlendEntry[] entries) =>
            new Scent(baseEmotion, new EmotionBlend(entries));

        private static MemoryRoomAnswer MakeAnswer(Scent correctScent) =>
            new MemoryRoomAnswer(new MemoryRoomId("test-room"), correctScent);

        private static ScentJudge MakeJudge(double highAccuracyThreshold = 0.8) =>
            new ScentJudge(new ScentJudgementSettings(highAccuracyThreshold));

        [Test]
        public void 완전히_일치하면_최상위_피드백_단계가_나온다()
        {
            var correct = MakeScent(EmotionType.Joy,
                new EmotionBlendEntry(EmotionType.Love, 10),
                new EmotionBlendEntry(EmotionType.Sadness, 5));
            var candidate = MakeScent(EmotionType.Joy,
                new EmotionBlendEntry(EmotionType.Love, 10),
                new EmotionBlendEntry(EmotionType.Sadness, 5));

            var result = MakeJudge().Judge(candidate, MakeAnswer(correct));

            Assert.IsTrue(result.IsBaseEmotionCorrect);
            Assert.AreEqual(FeedbackStage.PianoAndViolinAndDrum, result.Stage);
            Assert.AreEqual(1.0, result.Accuracy, 1e-9);
        }

        [Test]
        public void 바탕_감정만_틀리면_배분이_같아도_무반응이다()
        {
            var correct = MakeScent(EmotionType.Joy,
                new EmotionBlendEntry(EmotionType.Love, 10),
                new EmotionBlendEntry(EmotionType.Sadness, 5));
            var candidate = MakeScent(EmotionType.Fear,
                new EmotionBlendEntry(EmotionType.Love, 10),
                new EmotionBlendEntry(EmotionType.Sadness, 5));

            var result = MakeJudge().Judge(candidate, MakeAnswer(correct));

            Assert.IsFalse(result.IsBaseEmotionCorrect);
            Assert.AreEqual(FeedbackStage.Silence, result.Stage);
            // 바탕 감정이 틀려도 정확도 자체는 계산되어 결과에 남아야 한다.
            Assert.AreEqual(1.0, result.Accuracy, 1e-9);
        }

        [Test]
        public void 같은_총량_안에서_정답에_가까울수록_정확도가_높다()
        {
            var correct = MakeScent(EmotionType.Joy,
                new EmotionBlendEntry(EmotionType.Love, 10),
                new EmotionBlendEntry(EmotionType.Anger, 10));

            var closeCandidate = MakeScent(EmotionType.Joy,
                new EmotionBlendEntry(EmotionType.Love, 9),
                new EmotionBlendEntry(EmotionType.Anger, 11));
            var farCandidate = MakeScent(EmotionType.Joy,
                new EmotionBlendEntry(EmotionType.Love, 2),
                new EmotionBlendEntry(EmotionType.Anger, 18));

            var judge = MakeJudge();
            var closeResult = judge.Judge(closeCandidate, MakeAnswer(correct));
            var farResult = judge.Judge(farCandidate, MakeAnswer(correct));

            Assert.Greater(closeResult.Accuracy, farResult.Accuracy);
        }

        [Test]
        public void 총_편차가_같으면_한_감정만_크게_틀리든_나눠_틀리든_정확도는_같다()
        {
            var correct = MakeScent(EmotionType.Joy,
                new EmotionBlendEntry(EmotionType.Love, 10),
                new EmotionBlendEntry(EmotionType.Anger, 10));

            // 두 후보 모두 총 편차(diffSum)는 8, 후보 자신의 총량도 12로 같다.
            var concentrated = MakeScent(EmotionType.Joy,
                new EmotionBlendEntry(EmotionType.Love, 2),
                new EmotionBlendEntry(EmotionType.Anger, 10));
            var spread = MakeScent(EmotionType.Joy,
                new EmotionBlendEntry(EmotionType.Love, 6),
                new EmotionBlendEntry(EmotionType.Anger, 6));

            var judge = MakeJudge();
            var concentratedResult = judge.Judge(concentrated, MakeAnswer(correct));
            var spreadResult = judge.Judge(spread, MakeAnswer(correct));

            Assert.AreEqual(concentratedResult.Accuracy, spreadResult.Accuracy, 1e-9);
        }

        [Test]
        public void 양쪽_배분이_모두_비어있으면_정확도는_1이다()
        {
            var correct = MakeScent(EmotionType.Joy);
            var candidate = MakeScent(EmotionType.Joy);

            var result = MakeJudge().Judge(candidate, MakeAnswer(correct));

            Assert.AreEqual(1.0, result.Accuracy, 1e-9);
        }

        [Test]
        public void 총량이_크게_어긋나도_정확도는_0에서_1_사이다()
        {
            var correct = MakeScent(EmotionType.Joy,
                new EmotionBlendEntry(EmotionType.Love, 3));
            var candidate = MakeScent(EmotionType.Joy,
                new EmotionBlendEntry(EmotionType.Fear, 500),
                new EmotionBlendEntry(EmotionType.Sadness, 500));

            var result = MakeJudge().Judge(candidate, MakeAnswer(correct));

            Assert.GreaterOrEqual(result.Accuracy, 0.0);
            Assert.LessOrEqual(result.Accuracy, 1.0);
        }

        [Test]
        public void 정확도가_임계값_미만이면_피아노_단계에_머문다()
        {
            var correct = MakeScent(EmotionType.Joy,
                new EmotionBlendEntry(EmotionType.Love, 10),
                new EmotionBlendEntry(EmotionType.Anger, 10));
            var farCandidate = MakeScent(EmotionType.Joy,
                new EmotionBlendEntry(EmotionType.Love, 1),
                new EmotionBlendEntry(EmotionType.Anger, 1));

            var result = MakeJudge(highAccuracyThreshold: 0.99).Judge(farCandidate, MakeAnswer(correct));

            Assert.AreEqual(FeedbackStage.Piano, result.Stage);
        }
    }
}
