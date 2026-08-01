using GameName.Core.Emotions;
using GameName.Core.Validation;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    public class ScentCompositionValidatorTests
    {
        private static IEmotionCompositionPolicy MakePolicy(
            int min = 2, int max = 4, bool allowSameAsBase = false) =>
            new EmotionCompositionPolicy(min, max, allowSameAsBase);

        [Test]
        public void 규칙을_모두_지키면_유효하다()
        {
            var validator = new ScentCompositionValidator(MakePolicy());
            var scent = new Scent(EmotionType.Joy, new EmotionBlend(new[]
            {
                new EmotionBlendEntry(EmotionType.Love, 10),
                new EmotionBlendEntry(EmotionType.Anger, 5),
            }));

            var result = validator.Validate(scent, requiredSupportingIntensityTotal: 15);

            Assert.IsTrue(result.IsValid);
        }

        [Test]
        public void 위반_사유가_여러_개면_전부_함께_반환된다()
        {
            var validator = new ScentCompositionValidator(MakePolicy(min: 2, max: 4, allowSameAsBase: false));

            // 1) 보조 감정 개수 1종 -> 최소 2종 위반
            // 2) 보조 감정에 바탕 감정(Joy)과 같은 감정 포함 -> 중복 금지 위반
            // 3) 세기 총량 5 != 요구 총량 15 -> 총량 불일치 위반
            var scent = new Scent(EmotionType.Joy, new EmotionBlend(new[]
            {
                new EmotionBlendEntry(EmotionType.Joy, 5),
            }));

            var result = validator.Validate(scent, requiredSupportingIntensityTotal: 15);

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(3, result.Violations.Count);
        }

        [Test]
        public void 세기_총량이_요구치보다_많아도_위반이다()
        {
            var validator = new ScentCompositionValidator(MakePolicy());
            var scent = new Scent(EmotionType.Joy, new EmotionBlend(new[]
            {
                new EmotionBlendEntry(EmotionType.Love, 10),
                new EmotionBlendEntry(EmotionType.Anger, 10),
            }));

            // 총량 20은 요구 총량 15보다 크다 -> 상한이 아니라 정확히 일치해야 하므로 위반.
            var result = validator.Validate(scent, requiredSupportingIntensityTotal: 15);

            Assert.IsFalse(result.IsValid);
        }
    }
}
