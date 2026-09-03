namespace GameName.Core.Authoring
{
    // 저작 데이터 검증 결과의 심각도.
    //
    // 둘을 가르는 기준은 "이대로 플레이할 수 있는가"다. Error는 진행이 막히거나
    // 영영 풀 수 없는 검열이 남는 경우이고, Warning은 플레이는 되지만 기획 의도와
    // 다르게 보일 수 있는 경우다. 경고를 코드가 알아서 고치지 않는 이유는
    // 기획자가 일부러 그렇게 뒀을 수 있기 때문이다.
    public enum ScriptIssueSeverity
    {
        Warning,
        Error
    }
}
