namespace GameName.Core.Ampoules
{
    // 조향(앰플 제작) 실패 사유.
    public enum AmpouleCraftingFailureReason
    {
        NotInPerfumeryRoom,
        InvalidComposition,

        // 조향실 앰플 보관 상한, 또는 단서와 공유하는 일반 인벤토리 용량 중
        // 하나라도 넘치면 이 사유로 통일한다 — 플레이어 입장에서는 "더 이상
        // 넣을 곳이 없다"는 하나의 사실일 뿐이다.
        StorageFull,
        InsufficientMentality
    }
}
