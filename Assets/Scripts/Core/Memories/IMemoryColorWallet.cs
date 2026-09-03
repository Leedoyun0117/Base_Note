namespace GameName.Core.Memories
{
    // 지금 어떤 색을 얼마나 들고 있는지 읽기만 하는 경계.
    // 대사 조건 판정, 화면 표시처럼 "보기만 하면 되는" 쪽에는 이쪽만 주입해
    // 소지량을 몰래 바꾸는 경로 자체를 없앤다.
    public interface IMemoryColorWallet
    {
        int GetCount(MemoryColor color);
    }
}
