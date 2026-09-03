namespace GameName.Core.Clues
{
    // 방 가운데를 기준으로 좌우 대칭으로 열리는 접근 정책.
    //
    // 가시 비율 v면 방 한가운데(0.5)에서 양쪽으로 v/2씩, 곧 [0.5 - v/2, 0.5 + v/2]
    // 구간 안의 단서만 집을 수 있다. v가 1이면 방 전체(0~1), v가 0.5면 [0.25, 0.75]다.
    //
    // 경계에 정확히 걸친 단서는 집을 수 있는 것으로 본다 — "보이긴 하는데 손이
    // 닿지 않는" 자리를 한 픽셀 두께로 만들면 그 자리에 놓인 단서가 버그처럼
    // 보이기 때문이다. 부동소수 계산 오차로 경계가 미세하게 어긋나는 것도 같은
    // 이유로 허용 오차 안에서 흡수한다.
    //
    // 이미 집은 단서를 신뢰도가 깎여 구간 밖으로 밀어냈을 때 인벤토리에서
    // 빼는 일은 이 정책이 관여하지 않는다. 이 정책이 답하는 것은 오직 "지금
    // 집을 수 있는가"이고, 집는 처리기가 아직 Available인 단서에게만 이 질문을 한다.
    public sealed class CenteredClueAccessPolicy : IClueAccessPolicy
    {
        private const float Center = 0.5f;
        private const float BoundaryTolerance = 1e-4f;

        public bool IsAccessible(CluePositionRatio position, float visibleRatio)
        {
            if (visibleRatio <= 0f)
                return false;

            var half = visibleRatio / 2f;
            var lower = Center - half;
            var upper = Center + half;

            return position.Value >= lower - BoundaryTolerance
                && position.Value <= upper + BoundaryTolerance;
        }
    }
}
