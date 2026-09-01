using System;
using GameName.Core.Events;
using GameName.Core.Inventory;
using GameName.Core.MemoryRooms;

namespace GameName.Core.Clues
{
    // 인벤토리의 단서를 지금 서 있는 방에 버리는 처리기.
    //
    // 예전 규칙("원래 있던 방에만 되돌려놓을 수 있다")을 기획 판단으로 바꾼
    // 자리다. 지금은 어느 기억 방에서든 버릴 수 있고, 버린 단서는 그 방 소속으로
    // 재배정된다 — 그래서 이 타입 이름도 "되돌리기(Return)"가 아니라
    // "버리기(Drop)"다. 되돌린다는 말은 원래 자리가 있다는 전제를 담고 있는데,
    // 그 전제가 사라졌기 때문이다.
    //
    // 규칙이 느슨해진 만큼 남은 검사는 두 가지뿐이다: 실제로 들고 있는
    // 단서인가, 그리고 지금 서 있는 곳이 기억 방인가. 두 번째 검사가 필요한
    // 이유는 계단 같은 허브에는 "단서가 놓인 방"이라는 개념
    // 자체가 없기 때문이다. 그 판단은 그래프 노드 종류로 하며, 이 처리기가
    // 방 목록을 따로 들고 다니지 않는다.
    public sealed class ClueDropProcessor
    {
        private readonly IPlayerLocation _playerLocation;
        private readonly IMemoryRoomGraph _graph;
        private readonly IPlayerInventory _inventory;
        private readonly IMemoryRoomClueTracker _clueTracker;
        private readonly IEventBus _eventBus;

        public ClueDropProcessor(
            IPlayerLocation playerLocation,
            IMemoryRoomGraph graph,
            IPlayerInventory inventory,
            IMemoryRoomClueTracker clueTracker,
            IEventBus eventBus)
        {
            _playerLocation = playerLocation ?? throw new ArgumentNullException(nameof(playerLocation));
            _graph = graph ?? throw new ArgumentNullException(nameof(graph));
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            _clueTracker = clueTracker ?? throw new ArgumentNullException(nameof(clueTracker));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        public ClueDropResult Drop(ClueInfo clue)
        {
            if (clue == null) throw new ArgumentNullException(nameof(clue));

            if (!ContainsClue(clue))
                return ClueDropResult.Failure(ClueDropFailureReason.ClueNotInInventory);

            if (!TryResolveCurrentRoom(out var roomId))
                return ClueDropResult.Failure(ClueDropFailureReason.NotInMemoryRoom);

            _inventory.TryRemove(clue);
            _clueTracker.PlaceInRoom(clue.Id, roomId);

            _eventBus.Publish(new ClueDroppedEvent(clue.Id, roomId));

            return ClueDropResult.Success();
        }

        // 노드 식별자만으로는 지금 있는 곳이 기억 방인지 허브인지 알 수 없으므로
        // 그래프에 노드 종류를 물어본다. 기억 방 노드의 식별자는
        // MemoryGraphNodeId.OfRoom으로 만들어진 것이라 그 역변환이 성립한다 —
        // 이동 처리기가 방문 기록을 남길 때 쓰는 것과 같은 변환이다.
        private bool TryResolveCurrentRoom(out MemoryRoomId roomId)
        {
            var current = _playerLocation.Current;
            if (_graph.TryGetNode(current, out var node) && node.Type == MemoryGraphNodeType.MemoryRoom)
            {
                roomId = new MemoryRoomId(current.Value);
                return true;
            }

            roomId = default;
            return false;
        }

        private bool ContainsClue(ClueInfo clue)
        {
            foreach (var item in _inventory.Items)
            {
                if (item.Equals(clue))
                    return true;
            }

            return false;
        }
    }
}
