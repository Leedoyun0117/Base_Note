namespace GameName.Core.Extraction
{
    // 추출 횟수를 실제로 깎을 수 있는 경계.
    // 추출 처리기 하나에만 주입한다 — 이 인터페이스가 넓게 퍼지면 어디서
    // 횟수가 줄었는지 추적할 수 없게 된다.
    public interface IExtractionBudgetSpender : IExtractionBudget
    {
        void Spend();
    }
}
