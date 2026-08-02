using System.Collections.Generic;

namespace GameName.Core.Ampoules
{
    // 조향(앰플 제작) 시도의 결과.
    public sealed class AmpouleCraftingResult
    {
        public bool Succeeded { get; }
        public AmpouleCraftingFailureReason? FailureReason { get; }
        public IReadOnlyList<Ampoule> CraftedAmpoules { get; }

        private AmpouleCraftingResult(
            bool succeeded, AmpouleCraftingFailureReason? failureReason, IReadOnlyList<Ampoule> craftedAmpoules)
        {
            Succeeded = succeeded;
            FailureReason = failureReason;
            CraftedAmpoules = craftedAmpoules;
        }

        public static AmpouleCraftingResult Success(IReadOnlyList<Ampoule> craftedAmpoules) =>
            new AmpouleCraftingResult(true, null, craftedAmpoules);

        public static AmpouleCraftingResult Failure(AmpouleCraftingFailureReason reason) =>
            new AmpouleCraftingResult(false, reason, null);
    }
}
