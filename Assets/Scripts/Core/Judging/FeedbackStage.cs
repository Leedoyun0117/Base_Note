namespace GameName.Core.Judging
{
    // 시향 시 나오는 청각 피드백 단계.
    // 값 순서 자체가 일치도가 높아지는 방향을 의미하므로 순서를 바꾸지 않는다.
    public enum FeedbackStage
    {
        Silence,
        Piano,
        PianoAndViolin,
        PianoAndViolinAndDrum
    }
}
