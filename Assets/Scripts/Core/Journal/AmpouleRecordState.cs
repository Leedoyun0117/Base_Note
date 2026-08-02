namespace GameName.Core.Journal
{
    // 기록지가 보여주는, 앰플 하나의 현재 위치. 기록 자체(AmpouleRecord)는
    // 불변이지만 이 상태는 매 조회 시점에 보관함/인벤토리/시향 기록을 다시
    // 확인해 계산한다 — 기록지가 "지금 어디 있는지"를 별도로 추적하기
    // 시작하면 IAmpouleStorage/IPlayerInventory와 상태가 어긋날 위험이 생기기
    // 때문이다.
    public enum AmpouleRecordState
    {
        InStorage,
        InInventory,
        Consumed
    }
}
