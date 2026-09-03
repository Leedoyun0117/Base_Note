namespace GameName.Core.Extraction
{
    // 추출을 앞으로 몇 번 더 할 수 있는지 읽기만 하는 경계.
    // 화면 표시와 "추출 가능한가" 판정은 이쪽만 참조한다.
    public interface IExtractionBudget
    {
        int Remaining { get; }
    }
}
