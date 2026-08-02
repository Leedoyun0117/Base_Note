namespace GameName.Core.Upgrades
{
    // 업그레이드 종류별로 다음 레벨 옵션을 조회하는 경계. 실제 가격/효과
    // 수치는 이 인터페이스 뒤(카탈로그 구현체)에만 있다 — 상점(UpgradeShop)은
    // 그 수치를 몰라도 "다음 레벨이 있는지, 있다면 무엇인지"만 물어 구매를
    // 처리할 수 있다.
    public interface IUpgradeCatalog
    {
        bool TryGetOption(UpgradeCategory category, int level, out UpgradeOption option);
    }
}
