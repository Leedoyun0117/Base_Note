namespace GameName.Core.Dialogue
{
    // 검열 해금 시도가 실패한 이유.
    public enum CensorUnlockFailureReason
    {
        // 그 키에 요구 태그가 저작되어 있지 않다 — 저작 데이터와 어긋난 호출이다.
        UnknownKey,

        // 제시하려는 출처 단서로 추출된 기억이 지금 손에 없다(아직 추출하지
        // 않았거나, 이미 다른 키를 푸는 데 써 버렸다).
        MemoryNotFound,

        // 제시한 기억은 있지만 그 태그가 이 키가 요구하는 태그와 하나도
        // 겹치지 않는다. 색이 맞아도(힌트가 맞아도) 여기 걸릴 수 있다 —
        // 정답 판정은 색이 아니라 태그이기 때문이다. 렌더링은 계속 마스크된
        // 채로 남는다.
        TagMismatch
    }
}
