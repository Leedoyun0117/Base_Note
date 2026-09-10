namespace GameName.Core.Complexes
{
    // 서사 태그가 걸리는 세 축.
    //
    // 옛 ClueTagAxis(중심축/곁축)를 대체한다. 그때는 "이 태그가 정답 판정에서
    // 중심이냐 곁이냐"라는 판정용 메타였지만, 질문-답변이 사라진 3차 개편에서
    // 태그는 "이 단서가 무엇에 대한 것인가"를 세 갈래로 나눠 적는 값이다:
    //   Person  — 누구에 관한 기억인가
    //   Emotion — 어떤 감정인가 (감정 반응·안정 축 이동이 이 축만 본다)
    //   Time    — 어느 시간대인가
    //
    // 색·개수가 아니라 타입으로 못박는 이유는 MemoryColor와 같다: 축이 늘거나
    // 줄면 컴플렉스 규칙·해석 로그 표현이 함께 바뀌므로 밸런싱 값이 아니다.
    public enum StoryTagAxis
    {
        Person,
        Emotion,
        Time
    }
}
