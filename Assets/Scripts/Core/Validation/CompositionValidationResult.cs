using System;
using System.Collections.Generic;

namespace GameName.Core.Validation
{
    // 배합 유효성 검증 결과. 위반 사유가 될 구체적 규칙이 아직 미정이므로
    // 문자열 목록으로 남겨 검증기 구현이 자유롭게 사유를 채울 수 있게 한다.
    public sealed class CompositionValidationResult
    {
        public bool IsValid { get; }
        public IReadOnlyList<string> Violations { get; }

        private CompositionValidationResult(bool isValid, IReadOnlyList<string> violations)
        {
            IsValid = isValid;
            Violations = violations;
        }

        public static CompositionValidationResult Valid() =>
            new CompositionValidationResult(true, Array.Empty<string>());

        public static CompositionValidationResult Invalid(IReadOnlyList<string> violations)
        {
            if (violations == null || violations.Count == 0)
                throw new ArgumentException("무효 판정에는 사유가 최소 1개 필요하다.", nameof(violations));

            return new CompositionValidationResult(false, violations);
        }
    }
}
