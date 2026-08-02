namespace GameName.Core.Clues
{
    // 단서 습득 실패 사유. 인벤토리 저장 실패 사유와 개념적으로 겹치는 부분이
    // 있지만, 이 열거형은 "단서를 집는 행위" 자체에 대한 사용자 관점의 사유이고
    // 인벤토리 쪽 사유는 저장 메커니즘 관점이라 의도적으로 분리해 둔다.
    public enum ClueCollectionFailureReason
    {
        // 이미 습득했거나 애초에 존재하지 않는 단서.
        NotAvailable,
        WrongRoom,
        InventoryFull,
        ItemTypeNotAccepted,
        Duplicate
    }
}
