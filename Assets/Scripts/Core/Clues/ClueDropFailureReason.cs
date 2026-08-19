namespace GameName.Core.Clues
{
    // 단서를 버리려는 시도의 실패 사유.
    //
    // 예전 되돌리기에 있던 WrongRoom("이 방의 단서가 아니다")은 사라졌다 —
    // 어느 방에나 버릴 수 있게 된 순간 그 실패 자체가 성립하지 않는다.
    public enum ClueDropFailureReason
    {
        ClueNotInInventory,

        // 계단/분석실/조향실처럼 기억 방이 아닌 곳에 서 있다.
        NotInMemoryRoom
    }
}
