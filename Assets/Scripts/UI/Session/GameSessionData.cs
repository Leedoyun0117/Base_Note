using System;
using System.Collections.Generic;
using GameName.Core.Clues;
using GameName.Core.MemoryRooms;

namespace GameName.UI.Session
{
    // GameSession을 조립하는 데 필요한 세계 데이터 묶음 — 방 그래프의 구조와
    // 단서의 최초 배치가 전부다.
    //
    // 이 타입은 규칙을 하나도 담지 않는다. 실제 콘텐츠 소스(레벨 에디터
    // 산출물, 세이브 데이터 등)가 생기면 이 묶음을 채우는 코드만 바뀐다.
    public sealed class GameSessionData
    {
        public IReadOnlyList<MemoryGraphNode> Nodes { get; }
        public IReadOnlyList<OpenConnection> OpenConnections { get; }
        public IReadOnlyList<LadderConnection> LadderConnections { get; }

        // 단서가 처음에 어느 방에 놓여 있는가. 그 뒤의 소속 변경은 전부
        // IMemoryRoomClueTracker 안에서 일어난다.
        public IReadOnlyList<CluePlacement> CluePlacements { get; }

        // 기억 방 노드에 대응하는 방 식별자 목록. 화면이 "지금 서 있는 곳이
        // 기억 방인가"를 판별할 때 쓴다(CurrentRoomResolver).
        public IReadOnlyList<MemoryRoomId> RoomIds { get; }

        // 플레이어가 처음 서 있는 노드.
        public MemoryGraphNodeId StartNodeId { get; }

        public GameSessionData(
            IReadOnlyList<MemoryGraphNode> nodes,
            IReadOnlyList<OpenConnection> openConnections,
            IReadOnlyList<LadderConnection> ladderConnections,
            IReadOnlyList<CluePlacement> cluePlacements,
            IReadOnlyList<MemoryRoomId> roomIds,
            MemoryGraphNodeId startNodeId)
        {
            Nodes = nodes ?? throw new ArgumentNullException(nameof(nodes));
            OpenConnections = openConnections ?? throw new ArgumentNullException(nameof(openConnections));
            LadderConnections = ladderConnections ?? throw new ArgumentNullException(nameof(ladderConnections));
            CluePlacements = cluePlacements ?? throw new ArgumentNullException(nameof(cluePlacements));
            RoomIds = roomIds ?? throw new ArgumentNullException(nameof(roomIds));
            StartNodeId = startNodeId;
        }
    }
}
