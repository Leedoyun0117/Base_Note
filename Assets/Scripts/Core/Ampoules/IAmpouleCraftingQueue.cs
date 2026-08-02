using System.Collections.Generic;

namespace GameName.Core.Ampoules
{
    // 조향실에서 "아직 만들지 않은 배합 대기열"을 들고 있는 경계.
    //
    // 예전에는 이 대기열을 화면 컨트롤러(PerfumeryCompositionPanelController)가
    // 직접 List로 들고 있었다 — 화면 전환이 SetActive로 이루어지는 한, 컨트롤러는
    // 화면이 다시 켜질 때마다 Bootstrap에 의해 새로 만들어지므로 대기열도 함께
    // 사라졌다. 이 타입을 GameSession(화면과 무관하게 유지되는 조립 루트)이
    // 대신 들고 있게 하면, 화면 컨트롤러는 매번 새로 만들어져도 대기열 내용은
    // 그대로 이어진다.
    public interface IAmpouleCraftingQueue
    {
        IReadOnlyList<AmpouleCraftingRequest> Items { get; }

        void Add(AmpouleCraftingRequest request);
        void RemoveAt(int index);

        // 대기열을 제작 처리기에 넘겨 실제로 앰플을 만든 뒤 비우는, 화면의
        // 정상적인 사용 흐름이다 — 다른 의뢰의 상태를 침범하는 파괴적 초기화가
        // 아니므로 IResettable과 별개로 여기 정상 인터페이스에 둔다.
        void Clear();
    }
}
