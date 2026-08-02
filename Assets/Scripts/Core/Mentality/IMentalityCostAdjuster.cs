namespace GameName.Core.Mentality
{
    // 정신력 비용 수치를 낮추는 권한 하나만 표현하는 좁은 경계.
    // IMentalityCostSettings(정상 조회 인터페이스)를 참조하는 모든 처리기는
    // 비용을 읽기만 할 뿐 바꿀 수 없다 — 오직 업그레이드 상점(UpgradeShop)만
    // 이 인터페이스로 비용을 낮출 권한을 받는다.
    public interface IMentalityCostAdjuster
    {
        // 기억 방 이동 비용을 낮춘다("방 이동 효율" 업그레이드).
        void ReduceMemoryRoomMoveCost(int amount);

        // 기본/고급 분석 비용을 함께 낮춘다("분석 효율" 업그레이드). 두 비용을
        // 따로 조정하는 세분화된 업그레이드는 두지 않는다 — 플레이어가 체감하는
        // 효과는 "분석이 더 저렴해졌다"는 사실 하나뿐이라, 굳이 두 개의 슬라이더로
        // 나누는 것은 불필요한 복잡함이다.
        void ReduceAnalysisCost(int amount);
    }
}
