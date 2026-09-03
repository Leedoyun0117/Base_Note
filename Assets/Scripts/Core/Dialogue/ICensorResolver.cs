namespace GameName.Core.Dialogue
{
    // 검열 하나가 지금 풀려 있는지 답하는 경계.
    //
    // 무엇이 검열을 푸는지(어떤 단서를 추출했는가, 어떤 색을 확보했는가)는 게임
    // 진행 규칙이고 앞으로 바뀔 여지가 크다. 파서와 갈라 둔 덕분에 그 규칙이
    // 어떻게 바뀌어도 대사를 다시 파싱할 필요는 없다.
    //
    // 판정 단위가 색이 아니라 키인 것이 핵심이다. 색으로 판정하면 그 색의 검열이
    // 전부 한꺼번에 열려, 어느 단서를 추출했는지와 무관하게 같은 결과가 된다.
    public interface ICensorResolver
    {
        bool IsRevealed(CensorKey key);
    }
}
