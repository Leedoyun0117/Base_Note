namespace GameName.Core.Memories
{
    // 소지량을 실제로 바꿀 수 있는 경계.
    // 추출 처리기와 소비 처리기처럼 변경 권한이 있는 쪽에만 주입한다.
    // 읽기 인터페이스를 상속하는 이유는 변경 주체가 스스로 현재 소지량을
    // 확인해야 하기 때문이다 — 읽기/쓰기를 한 타입으로 함께 받는다.
    public interface IMemoryColorWalletMutator : IMemoryColorWallet
    {
        void Add(MemoryColor color, int amount);
        void Remove(MemoryColor color, int amount);
    }
}
