namespace GameName.Core
{
    // 상태를 통째로 비우거나 초깃값으로 되돌릴 수 있는 권한 하나만 표현하는
    // 좁은 경계. IPlayerLocation/IPlayerLocationMover를 나눴던 것과 같은
    // 이유로 존재한다 — 정신력·인벤토리·보관함·트래커 등은 각자의 정상
    // 동작 인터페이스(IMentalityGauge, IPlayerInventory, ...)에 초기화
    // 메서드를 두지 않는다. 그 능력은 오직 이 인터페이스로만 노출되고,
    // 이 인터페이스는 의뢰 세션(CommissionSession)만 주입받는다 — 정상
    // 동작을 위해 그 시스템들을 주입받는 다른 모든 처리기·화면은 상태를
    // 통째로 날릴 방법이 아예 없다.
    public interface IResettable
    {
        void Reset();
    }
}
