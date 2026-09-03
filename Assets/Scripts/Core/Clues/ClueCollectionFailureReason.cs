namespace GameName.Core.Clues
{
    // 단서를 집는 시도가 실패한 이유.
    //
    // 옛 수집기 시절의 사유들(WrongRoom, InventoryFull, Duplicate 등)은 사라졌다.
    // 방 판정은 이제 "지금 서 있는 방"이 아니라 RunProgressor가 정하는 활성
    // 방이고(그 방 밖의 단서는 애초에 화면에 뜨지 않는다), 인벤토리 슬롯 배치는
    // 수집 이벤트를 듣는 별도 리스너의 몫이라 수집의 성패에 관여하지 않는다.
    public enum ClueCollectionFailureReason
    {
        // 이 방에 등록된 단서가 아니거나, 이미 수집·대화 사용·추출 중 하나로
        // 소비된 단서다.
        NotAvailable,

        // 신뢰도가 깎여 방이 좁게 보이는 탓에 그 단서가 지금 보이는 범위 밖에
        // 있어 손이 닿지 않는다. 신뢰도가 회복되지 않는 게임이라 이 상태는 그
        // 방에 있는 한 되돌아오지 않는다.
        OutOfView,

        // 가방에 빈 칸이 없다. 방 단서 수보다 가방이 작아 "무엇을 들고 나갈지"
        // 고르게 하는 것이 규칙이므로, 자리를 비우기 전에는 더 집을 수 없다.
        // ClueState는 Available 그대로라 자리를 비우면 다시 집을 수 있다.
        InventoryFull
    }
}
