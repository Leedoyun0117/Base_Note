namespace GameName.Core.Hiromi
{
    // 히로민을 실제로 벌고 쓸 수 있는 경계.
    // 대화 적립 리스너, 추출 처리기, 기억 이동 처리기에만 주입한다.
    public interface IHiromiMutator : IHiromiReader
    {
        void Earn(int amount);

        // amount가 지금 가진 것(Remaining)보다 크면 예외다 — 쓸 수 있는지
        // 먼저 확인하는 책임은 호출자에게 있다(부분 성공 없이 통째로 실패해야
        // 하는 처리기들과 같은 규율). 다만 기억 이동처럼 "가진 만큼만 쓰고
        // 나머지는 다른 대가(기회)로 치르는" 호출자는 스스로 Remaining만큼만
        // 넘겨 항상 성립하게 만든다.
        void Spend(int amount);
    }
}
