using System;
using System.Collections.Generic;
using GameName.Core.Clues;
using GameName.Core.Events;
using GameName.Core.Inventory;
using GameName.Core.MemoryRooms;
using GameName.UI.MemoryRoom.Space;

namespace GameName.UI.Session
{
    // 화면과 무관하게 공유되는 Core 객체 그래프를 조립하는 유일한 구성 루트.
    //
    // 이 조립을 화면 밖으로 뽑아 여기 한 곳에서만 하고, 각 화면의 Bootstrap은
    // 이미 만들어진 GameSession을 참조로 받아 UI만 그 위에 얹는다 — 화면마다
    // EventBus/Inventory 등을 따로 만들면 두 화면이 "같은 플레이어"를 가리키지
    // 않는 두 개의 독립된 상태로 갈라지기 때문이다.
    //
    // 이 타입도 게임 규칙은 계산하지 않는다 — 이미 있는 Core 타입들을 순서대로
    // 생성자에 밀어 넣어 서로 연결할 뿐이다.
    public sealed class GameSession
    {
        public IEventBus EventBus { get; }
        public IPlayerInventory Inventory { get; }
        public IMemoryRoomClueTracker ClueTracker { get; }
        public IMemoryRoomGraph Graph { get; }

        // 읽기 전용으로만 노출한다 — 위치를 실제로 바꿀 수 있는 것은
        // MovementProcessor 내부뿐이어야 한다는 규칙(IPlayerLocationMover의
        // 설계 의도)을 구성 루트 바깥에서도 지킨다.
        public IPlayerLocation PlayerLocation { get; }

        public MemoryRoomMovementProcessor MovementProcessor { get; }
        public ClueCollector ClueCollector { get; }
        public ClueDropProcessor ClueDropProcessor { get; }

        // 단서를 버린 자리(표시 전용). Core 객체가 아니지만 수명이 세션 하나와
        // 같아야 해서 여기서 함께 소유한다.
        public DroppedCluePositions DroppedCluePositions { get; }

        public IReadOnlyList<MemoryRoomId> RoomIds { get; }

        public GameSession(GameSessionData data, GameSessionSettings settings)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            EventBus = new EventBus(new NoOpEventExceptionHandler());

            Inventory = new PlayerInventory(settings.InventorySettings, new SharedSlotInventoryPolicy());

            var graph = new MemoryRoomGraph(data.Nodes, data.OpenConnections, data.LadderConnections);
            Graph = graph;

            var clueTracker = new MemoryRoomClueTracker(data.CluePlacements);
            ClueTracker = clueTracker;

            RoomIds = data.RoomIds;

            var playerLocation = new PlayerLocation(data.StartNodeId);
            PlayerLocation = playerLocation;

            MovementProcessor = new MemoryRoomMovementProcessor(graph, playerLocation, EventBus);
            ClueCollector = new ClueCollector(playerLocation, Inventory, clueTracker);
            ClueDropProcessor = new ClueDropProcessor(playerLocation, graph, Inventory, clueTracker, EventBus);

            DroppedCluePositions = new DroppedCluePositions();
        }
    }
}
