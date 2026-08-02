namespace GameName.Core.Ampoules
{
    // 앰플 식별자 생성 경계. Guid 같은 구체적인 생성 방식을 조향 처리기가 직접
    // 알지 않도록 분리한다 — 테스트에서 결정적인 식별자를 주입하거나, 나중에
    // 세이브/로드에 맞는 다른 정책으로 교체할 수 있게 하기 위함이다.
    public interface IAmpouleIdGenerator
    {
        AmpouleId Generate();
    }
}
