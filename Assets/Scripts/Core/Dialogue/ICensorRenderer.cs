namespace GameName.Core.Dialogue
{
    // 파싱된 대사와 지금의 해금 상태를 합쳐 화면에 나갈 한 줄을 만드는 경계.
    //
    // 파서와 갈라 둔 이유가 여기서 드러난다. 같은 CensoredText 하나가 해금
    // 상태에 따라 여러 번 다른 문자열로 그려지는데, 그 매번을 파싱까지 되돌아가
    // 처리하면 원문 표기 문법이 화면 갱신 경로에 계속 끌려 나오게 된다.
    public interface ICensorRenderer
    {
        string Render(CensoredText text);
    }
}
