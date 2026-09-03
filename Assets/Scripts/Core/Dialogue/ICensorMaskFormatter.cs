using GameName.Core.Memories;

namespace GameName.Core.Dialogue
{
    // 아직 풀리지 않은 구간을 대신 채울 문구를 내주는 경계.
    //
    // "[파란색]"처럼 화면에 그대로 나가는 글자라서 Core에는 구현이 없다. 색
    // 이름은 번역 대상이고 표기 형태(괄호를 칠지, 기호로 덮을지)도 연출에 따라
    // 바뀌므로, 렌더러는 이 경계 너머에서 받은 문자열을 끼워 넣기만 한다.
    public interface ICensorMaskFormatter
    {
        string FormatMask(MemoryColor color);
    }
}
