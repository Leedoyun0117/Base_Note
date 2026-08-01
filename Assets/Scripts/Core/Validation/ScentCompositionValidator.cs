using System;
using System.Collections.Generic;
using GameName.Core.Emotions;

namespace GameName.Core.Validation
{
    // IScentCompositionValidator 기본 구현.
    // 정책(IEmotionCompositionPolicy)은 생성자로 주입받아 감춰두고, 호출부는
    // 정책의 존재 자체를 몰라도 된다. 위반 사유는 하나만 찾고 멈추지 않고 전부
    // 모아서 반환한다 — 플레이어가 한 번에 여러 문제를 확인할 수 있어야 하기 때문이다.
    public sealed class ScentCompositionValidator : IScentCompositionValidator
    {
        private readonly IEmotionCompositionPolicy _policy;

        public ScentCompositionValidator(IEmotionCompositionPolicy policy)
        {
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        }

        public CompositionValidationResult Validate(Scent scent, int requiredSupportingIntensityTotal)
        {
            if (scent == null) throw new ArgumentNullException(nameof(scent));

            var blend = scent.SupportingBlend;
            var violations = new List<string>();

            if (blend.Count < _policy.MinSupportingEmotionCount || blend.Count > _policy.MaxSupportingEmotionCount)
            {
                violations.Add(
                    $"보조 감정 개수는 {_policy.MinSupportingEmotionCount}~{_policy.MaxSupportingEmotionCount}종이어야 하는데 {blend.Count}종이다.");
            }

            // EmotionBlendEntry는 구조체라 default 값(세기 0)이 검증을 우회해 배열/
            // 리스트에 섞여 들어올 수 있다. EmotionBlend 생성자는 감정 중복 여부만
            // 확인하므로, 여기서 세기가 실제로 양수인지 다시 한 번 확인한다.
            foreach (var entry in blend.Entries)
            {
                if (entry.Intensity <= 0)
                    violations.Add($"{entry.Emotion}의 세기는 양수여야 하는데 {entry.Intensity}이다.");
            }

            if (blend.Total != requiredSupportingIntensityTotal)
            {
                violations.Add(
                    $"세기 총량은 {requiredSupportingIntensityTotal}이어야 하는데 {blend.Total}이다.");
            }

            if (!_policy.AllowSupportingEmotionSameAsBase && blend.Contains(scent.BaseEmotion))
            {
                violations.Add($"보조 감정에 바탕 감정({scent.BaseEmotion})과 같은 감정을 넣을 수 없다.");
            }

            return violations.Count == 0
                ? CompositionValidationResult.Valid()
                : CompositionValidationResult.Invalid(violations);
        }
    }
}
