namespace GameName.Core.Hiromi
{
    // 기회를 실제로 줄일 수 있는 경계. MemoryMoveProcessor 하나에만 주입한다 —
    // 기회가 줄어드는 유일한 계기가 히로민 부족 상태의 강제 이동이기 때문이다.
    public interface IChanceTracker : IChanceReader
    {
        void Decrease();
    }
}
