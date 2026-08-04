namespace GameName.Core.Clues
{
    // 단서를 방에 되돌려놓으려는 시도의 실패 사유.
    public enum ClueReturnFailureReason
    {
        ClueNotInInventory,
        WrongRoom
    }
}
