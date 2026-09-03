namespace GameName.Core.Clues
{
    // 지금 보이는 범위 안에 있는 단서를 집을 수 있는지 정하는 규칙.
    //
    // "보이는 만큼만 집을 수 있다"가 유일한 답은 아니다 — 경계에 걸친 단서를
    // 허용할지, 여유분을 둘지는 조작감 문제라 바뀔 수 있다. 그래서 비교식을
    // 수집기 안에 박지 않고 정책으로 뺀다. 가시 범위 자체를 어떻게 구했는지
    // (IVisibilityPolicy)는 이쪽의 관심사가 아니라 비율만 받는다.
    public interface IClueAccessPolicy
    {
        bool IsAccessible(CluePositionRatio position, float visibleRatio);
    }
}
