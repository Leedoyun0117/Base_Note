namespace GameName.Core.Ampoules
{
    // 보관함↔인벤토리 이동 실패 사유.
    public enum AmpouleTransferFailureReason
    {
        NotInPerfumeryRoom,
        AmpouleNotFound,
        DestinationFull
    }
}
