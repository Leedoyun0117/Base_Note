namespace GameName.Core.Dialogue
{
    // 검열 해금을 실제로 기록할 수 있는 경계.
    //
    // 읽기(ICensorResolver)와 쓰기를 가른 이유는 IClueStateReader/IClueStateMutator,
    // IMemoryColorWallet/IMemoryColorWalletMutator와 같다 — 무엇이 풀렸는지 판단만
    // 하는 쪽(렌더러, 선택지 필터)은 읽기만 받고, 실제로 풀 자격이 있는 쪽
    // (CensorUnlockProcessor 하나)만 이 쓰기 경계를 받는다.
    //
    // Record는 멱등이다. 같은 키가 여러 대사에 흩어져 있어 한 번의 해금으로 전부
    // 열려야 하고, 이미 열린 키를 다시 기록해도 아무 일도 일어나지 않아야 한다.
    public interface ICensorUnlockRecord : ICensorResolver
    {
        void Record(CensorKey key);
    }
}
