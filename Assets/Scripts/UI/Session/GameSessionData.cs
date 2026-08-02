using System;
using System.Collections.Generic;
using GameName.Core.Clues;
using GameName.Core.Journal;
using GameName.Core.MemoryRooms;

namespace GameName.UI.Session
{
    // GameSession을 조립하는 데 필요한 "세계" 데이터 묶음 — 기억 방 그래프
    // 구조와 방별 정답+단서. 전부 생성자로 주입받는다(매직 넘버 금지와 같은
    // 이유로, 그래프 배치를 코드 여기저기에 흩어 상수로 박아두지 않는다).
    public sealed class GameSessionData
    {
        public IReadOnlyList<MemoryGraphNode> Nodes { get; }
        public IReadOnlyList<OpenConnection> OpenConnections { get; }
        public IReadOnlyList<LadderConnection> LadderConnections { get; }
        public IReadOnlyList<MemoryRoomData> RoomData { get; }
        public MemoryGraphNodeId InitialPlayerPosition { get; }
        public MemoryGraphNodeId PerfumeryRoomNodeId { get; }
        public CommissionId InitialCommissionId { get; }

        public GameSessionData(
            IReadOnlyList<MemoryGraphNode> nodes,
            IReadOnlyList<OpenConnection> openConnections,
            IReadOnlyList<LadderConnection> ladderConnections,
            IReadOnlyList<MemoryRoomData> roomData,
            MemoryGraphNodeId initialPlayerPosition,
            MemoryGraphNodeId perfumeryRoomNodeId,
            CommissionId initialCommissionId)
        {
            Nodes = nodes ?? throw new ArgumentNullException(nameof(nodes));
            OpenConnections = openConnections ?? throw new ArgumentNullException(nameof(openConnections));
            LadderConnections = ladderConnections ?? throw new ArgumentNullException(nameof(ladderConnections));
            RoomData = roomData ?? throw new ArgumentNullException(nameof(roomData));
            InitialPlayerPosition = initialPlayerPosition;
            PerfumeryRoomNodeId = perfumeryRoomNodeId;
            InitialCommissionId = initialCommissionId;
        }
    }
}
