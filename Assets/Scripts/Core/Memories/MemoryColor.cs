namespace GameName.Core.Memories
{
    // 기억에서 뽑아낸 색. 세 종류로 고정한 것은 밸런싱 수치가 아니라 규칙이다 —
    // 색의 개수가 늘면 조합 규칙과 화면 표현이 함께 바뀌므로 데이터가 아닌
    // 타입으로 못박는다.
    public enum MemoryColor
    {
        Red,
        Green,
        Blue
    }
}
