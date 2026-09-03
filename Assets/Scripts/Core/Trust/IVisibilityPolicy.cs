namespace GameName.Core.Trust
{
    // 신뢰도가 방을 얼마나 보여주는지를 정하는 규칙.
    //
    // 신뢰도와 가시 범위의 대응은 밸런싱 대상이며 선형일 필요도 없다(구간별
    // 계단식, 특정 값 이상에서만 열림 등). 그래서 게이지 안에 계산식을 박지
    // 않고 정책으로 떼어 낸다. 반환값은 방 가로 길이에 대한 0~1 비율이라
    // 방 치수(MemoryRoomLayout)가 바뀌어도 이 규칙은 그대로 쓴다.
    public interface IVisibilityPolicy
    {
        float GetVisibleRatio(int trust);
    }
}
