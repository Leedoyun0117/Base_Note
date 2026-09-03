namespace GameName.Core.Dialogue
{
    // 검열 해금 시도가 실패한 이유.
    public enum CensorUnlockFailureReason
    {
        // 그 키가 이 판의 어느 대사에도 없어 어느 색으로 풀리는지 알 수 없다.
        // 저작 데이터와 어긋난 호출이다.
        UnknownKey,

        // 그 키를 푸는 데 필요한 기억색을 지갑에 하나도 들고 있지 않다.
        // 렌더링은 계속 마스크된 채로 남는다.
        InsufficientMemory
    }
}
