using System;
using GameName.Core.Clues;
using GameName.Core.Events;

namespace GameName.Core.Restoration
{
    // 추출로 색이 드러날 때마다 그 색 복원도에 뿌리와 단서 노드를 자동으로
    // 채워 넣는 리스너.
    //
    // MemoryColorRevealedEvent를 구독하는 이유: 이 사건이 색(어느 보드인지)과
    // 출처 단서(ClueId)를 함께 싣는 유일한 사건이다. ClueExtractedEvent에는 색이
    // 없다(추출이 늘 색을 남기는 것은 아니라서). 복원도는 색이 나온 추출만
    // 담으므로 이쪽이 정확히 맞다.
    //
    // 단서 이름은 새 저장소를 만들지 않고 기존 카탈로그(IMemoryRoomClueTracker)의
    // 정의를 그대로 읽는다. 자동 노드의 라벨은 이 이름으로 잠긴다.
    public sealed class RestorationBoardExtractionListener
    {
        private readonly IRestorationBoardMutator _board;
        private readonly IMemoryRoomClueTracker _clueTracker;

        public RestorationBoardExtractionListener(
            IRestorationBoardMutator board,
            IMemoryRoomClueTracker clueTracker,
            IEventBus eventBus)
        {
            _board = board ?? throw new ArgumentNullException(nameof(board));
            _clueTracker = clueTracker ?? throw new ArgumentNullException(nameof(clueTracker));
            if (eventBus == null) throw new ArgumentNullException(nameof(eventBus));

            eventBus.Subscribe<MemoryColorRevealedEvent>(OnColorRevealed);
        }

        private void OnColorRevealed(MemoryColorRevealedEvent revealed)
        {
            _board.EnsureColorRoot(revealed.Color);

            var label = _clueTracker.TryGetDefinition(revealed.Source, out var definition)
                ? definition.DisplayName
                : revealed.Source.Value;

            _board.AddClueNode(revealed.Color, revealed.Source, label);
        }
    }
}
