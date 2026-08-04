namespace GameName.Core.Events
{
    // 플레이어가 지도에서 계단(기억 진입/이탈 지점)을 눌러 "이탈하겠다"는
    // 의사를 명시적으로 표현했을 때만 발행된다. 단순히 계단 위에 있다는
    // 사실(예: 기억 진입 직후의 시작 위치)과는 구분해야 한다 — 그렇지 않으면
    // 아무것도 하지 않았는데도 이탈 확인 화면이 뜨는 오작동이 생긴다.
    public readonly struct MemoryExitRequestedEvent
    {
    }
}
