namespace GameName.Core.Events
{
    // 한 판의 방을 전부 지나왔다는 사실. 마지막 방이 클리어였는지 실패였는지와
    // 무관하게, 더 이상 넘어갈 방이 없을 때 발행된다.
    //
    // 결과 요약(모은 색, 푼 검열 등)을 여기 싣지 않는 것은 그 집계 규칙이 아직
    // 정해지지 않았기 때문이다. 필요해지면 별도 타입으로 만들어 더한다.
    public readonly struct RunCompletedEvent
    {
    }
}
