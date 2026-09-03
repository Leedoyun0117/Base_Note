using System;
using GameName.Core.Events;
using GameName.Core.Inventory;
using GameName.Core.MemoryRooms;
using GameName.Core.Trust;

namespace GameName.Core.Clues
{
    // 방에 놓인 단서를 집어 드는 유일한 경로.
    //
    // "이 단서를 집었는가"의 진실은 ClueState 하나다. 이 처리기가 Available →
    // Collected 전이를 유일하게 소유하고, 인벤토리 슬롯 배치 같은 표시·저장 쪽
    // 일은 ClueCollectedEvent를 듣는 별도 리스너(InventoryProjection)가 맡는다 —
    // 한 타입이 상태 전이와 슬롯 관리를 함께 갖지 않게 한다.
    //
    // 다만 "가방에 빈 칸이 있는가"는 전이 전에 여기서 확인한다. 방 단서 수가
    // 가방 용량보다 많아, 사후에 InventoryProjection이 담지 못하면 ClueState는
    // Collected인데 손에는 없는 단서가 생겨 조용히 사라지기 때문이다 — 원인
    // 지점에서 막는다. 읽기만 하고 담는 것은 여전히 리스너의 몫이다.
    //
    // "지금 어느 방인가"는 플레이어가 서 있는 위치가 아니라 RunProgressor가 정한
    // 활성 방이다(RoomStartedEvent). 그 방 밖의 단서는 애초에 화면에 뜨지 않으므로
    // 별도의 "잘못된 방" 사유는 없다.
    //
    // 접근 가능 여부의 비교식은 이 처리기가 갖지 않는다 — 가시 비율은
    // IVisibilityPolicy가, 그 비율 안에 단서가 드는지는 IClueAccessPolicy가 정한다.
    public sealed class ClueCollectionProcessor
    {
        private readonly IClueStateMutator _clueState;
        private readonly IClueAccessPolicy _accessPolicy;
        private readonly ITrustReader _trust;
        private readonly IVisibilityPolicy _visibilityPolicy;
        private readonly IMemoryRoomClueTracker _clueTracker;
        private readonly IPlayerInventory _inventory;
        private readonly IEventBus _eventBus;

        private MemoryRoomId _activeRoomId;
        private bool _hasActiveRoom;

        public ClueCollectionProcessor(
            IClueStateMutator clueState,
            IClueAccessPolicy accessPolicy,
            ITrustReader trust,
            IVisibilityPolicy visibilityPolicy,
            IMemoryRoomClueTracker clueTracker,
            IPlayerInventory inventory,
            IEventBus eventBus)
        {
            _clueState = clueState ?? throw new ArgumentNullException(nameof(clueState));
            _accessPolicy = accessPolicy ?? throw new ArgumentNullException(nameof(accessPolicy));
            _trust = trust ?? throw new ArgumentNullException(nameof(trust));
            _visibilityPolicy = visibilityPolicy ?? throw new ArgumentNullException(nameof(visibilityPolicy));
            _clueTracker = clueTracker ?? throw new ArgumentNullException(nameof(clueTracker));
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));

            _eventBus.Subscribe<RoomStartedEvent>(e =>
            {
                _activeRoomId = e.RoomId;
                _hasActiveRoom = true;
            });
        }

        public ClueCollectionResult Collect(ClueId clueId)
        {
            // 이 방에 등록되지 않았거나(다른 방 단서), 이미 소비된 단서는 집을 수 없다.
            // 이 검사가 곧 "이미 Collected인 단서를 가시 밖으로 밀어내도 뺏지 않는다"는
            // 규칙이다 — 가시성은 아직 Available인 단서에게만 묻는다.
            if (!_clueState.TryGetState(clueId, out var state) || state != ClueState.Available)
                return ClueCollectionResult.Failure(ClueCollectionFailureReason.NotAvailable);

            if (!_clueTracker.TryGetDefinition(clueId, out var definition))
                return ClueCollectionResult.Failure(ClueCollectionFailureReason.NotAvailable);

            var visibleRatio = _visibilityPolicy.GetVisibleRatio(_trust.Current);
            if (!_accessPolicy.IsAccessible(definition.AuthoredPosition, visibleRatio))
                return ClueCollectionResult.Failure(ClueCollectionFailureReason.OutOfView);

            // 손이 닿는 자리여도 가방이 가득 차 있으면 집지 않는다. 상태를
            // 바꾸기 전에 막아야 "Collected인데 손엔 없는" 단서가 생기지 않는다.
            if (_inventory.Items.Count >= _inventory.Capacity)
                return ClueCollectionResult.Failure(ClueCollectionFailureReason.InventoryFull);

            _clueState.SetState(clueId, ClueState.Collected);
            if (_hasActiveRoom)
                _eventBus.Publish(new ClueCollectedEvent(clueId, _activeRoomId));

            return ClueCollectionResult.Success();
        }
    }
}
