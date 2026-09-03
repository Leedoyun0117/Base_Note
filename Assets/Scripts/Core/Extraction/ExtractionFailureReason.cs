namespace GameName.Core.Extraction
{
    // 추출 시도가 실패한 이유.
    public enum ExtractionFailureReason
    {
        // 남은 추출 자원이 없다.
        ResourceExhausted,

        // 아직 수집되지 않은 단서. 방에 놓인 채로는 추출할 수 없다.
        NotCollected,

        // 이미 대화에 꺼내 쓴 단서. 손에는 남아 있지만 추출은 안 된다.
        AlreadyUsedInDialogue,

        // 이미 추출된 단서.
        AlreadyExtracted
    }
}
