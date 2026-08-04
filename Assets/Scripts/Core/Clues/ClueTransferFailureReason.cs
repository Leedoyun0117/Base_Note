namespace GameName.Core.Clues
{
    // 보관대↔인벤토리 이동 실패 사유.
    public enum ClueTransferFailureReason
    {
        NotInAnalysisRoom,
        ClueNotFound,
        DestinationFull
    }
}
