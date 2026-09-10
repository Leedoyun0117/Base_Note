namespace GameName.Core.Complexes
{
    // 안정 축 위치 하나를 받아 "이번 턴에 새 컴플렉스가 생길 확률"을 내는 규칙.
    //
    // 안정 축이 극단(침체 끝·흥분 끝)에 가까울수록 확률이 오른다. 그 대응이
    // 선형일 필요는 없어(구간별 계단식 등) 게이지 안에 식을 박지 않고 정책으로
    // 떼어 낸다 — IVisibilityPolicy와 같은 이유다. 반환값은 0~1이다.
    public interface IComplexSpawnPolicy
    {
        float SpawnChance(int stabilityPosition);
    }
}
