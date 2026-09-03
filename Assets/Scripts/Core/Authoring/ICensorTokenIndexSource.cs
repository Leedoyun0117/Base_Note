namespace GameName.Core.Authoring
{
    // 판 하나에 대한 검열 토큰 목록을 내주는 경계.
    //
    // 규칙이 CensorTokenIndex를 직접 만들지 않고 이 경계에서 받아 가는 이유는
    // 한 번의 검증에서 같은 원문을 여러 번 파싱하지 않기 위해서다. 규칙은
    // 자기가 처음 묻는지 나중에 묻는지 알 필요가 없다.
    public interface ICensorTokenIndexSource
    {
        CensorTokenIndex For(RunDefinition run);
    }
}
