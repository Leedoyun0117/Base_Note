using System.Collections.Generic;

namespace GameName.Core.Ampoules
{
    // 조향실 보관함 경계. 인벤토리와는 다른 장소다 — 만들어진 앰플은 여기 먼저
    // 담기고, 조향실에서 인벤토리로 옮겨야 목표 방까지 가져갈 수 있다.
    public interface IAmpouleStorage
    {
        int Capacity { get; }
        IReadOnlyList<Ampoule> Ampoules { get; }

        AmpouleStorageStoreResult TryStore(Ampoule ampoule);
        AmpouleStorageRemoveResult TryRemove(Ampoule ampoule);

        // 지금 개수에 더해 additionalCount만큼 더 담아도 상한을 넘지 않는지
        // 확인한다. UI 같은 소비자가 "몇 개를 더 담으려 하는데 괜찮은지"를
        // 스스로 계산하지 않고 이 저장소에 직접 물어볼 수 있게 하기 위한 것이다.
        bool CanAccept(int additionalCount);
    }
}
