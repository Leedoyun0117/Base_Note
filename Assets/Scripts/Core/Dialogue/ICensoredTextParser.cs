namespace GameName.Core.Dialogue
{
    // 저작된 대사 원문을 검열 조각들로 잘라 내는 경계.
    //
    // 표기 문법(어떤 기호로 검열 구간을 감쌌는가)은 저작 도구 사정이라 바뀔 수
    // 있으므로 인터페이스로 둔다. 이 경계는 문법만 알 뿐 무엇이 풀렸는지는
    // 모른다 — 그 판단은 ICensorResolver의 몫이고, 실제 문자열을 만드는 것은
    // ICensorRenderer의 몫이다.
    public interface ICensoredTextParser
    {
        CensoredText Parse(string authoredText);
    }
}
