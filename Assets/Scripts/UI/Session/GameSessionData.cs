using System;
using System.Collections.Generic;
using GameName.Core.Commissions;
using GameName.Core.MemoryRooms;

namespace GameName.UI.Session
{
    // GameSession을 조립하는 데 필요한 "세계" 데이터 묶음.
    //
    // 이전에는 방 그래프/정답/단서/대사까지 전부 여기 한 번만 담았지만, 그러면
    // 의뢰가 바뀔 때 갈아 끼울 데이터가 없다. 그래서 이 타입에는 모든 의뢰가
    // 공유하는 고정 지형(허브 노드/연결, 허브 안의 고정 지점들)만 남기고,
    // 의뢰마다 달라지는 데이터는 CommissionData 목록으로 분리했다.
    public sealed class GameSessionData
    {
        // 허브(계단·분석실·조향실)와 그 사이의 고정 연결 — 모든 의뢰가 공유하는
        // 지형이라 의뢰 교체 때 다시 만들지 않는다.
        public IReadOnlyList<MemoryGraphNode> HubNodes { get; }
        public IReadOnlyList<OpenConnection> HubOpenConnections { get; }

        // 기억으로 들어갈 때 시작하는 지점이자, 현실로 복귀할 때 반드시 서
        // 있어야 하는 지점(계단). 허브 소속 고정 지점이므로 의뢰가 바뀌어도
        // 값 자체는 바뀌지 않는다.
        public MemoryGraphNodeId MemoryEntryNodeId { get; }

        // 계단과는 별개의 공간이다 — 지도에서 이 노드를 누르면 곧장 계단으로
        // 돌아가 이탈 확인 절차가 시작된다(CommissionSession.TryReturnToEntryPoint).
        // 계단 자체는 여전히 평범한 허브 지점으로 남아 있고, 이 노드만 "나가는
        // 곳"이다.
        public MemoryGraphNodeId MemoryExitNodeId { get; }

        public MemoryGraphNodeId PerfumeryRoomNodeId { get; }
        public MemoryGraphNodeId AnalysisRoomNodeId { get; }

        public IReadOnlyList<CommissionData> Commissions { get; }

        public GameSessionData(
            IReadOnlyList<MemoryGraphNode> hubNodes,
            IReadOnlyList<OpenConnection> hubOpenConnections,
            MemoryGraphNodeId memoryEntryNodeId,
            MemoryGraphNodeId memoryExitNodeId,
            MemoryGraphNodeId perfumeryRoomNodeId,
            MemoryGraphNodeId analysisRoomNodeId,
            IReadOnlyList<CommissionData> commissions)
        {
            HubNodes = hubNodes ?? throw new ArgumentNullException(nameof(hubNodes));
            HubOpenConnections = hubOpenConnections ?? throw new ArgumentNullException(nameof(hubOpenConnections));
            MemoryEntryNodeId = memoryEntryNodeId;
            MemoryExitNodeId = memoryExitNodeId;
            PerfumeryRoomNodeId = perfumeryRoomNodeId;
            AnalysisRoomNodeId = analysisRoomNodeId;

            if (commissions == null) throw new ArgumentNullException(nameof(commissions));
            if (commissions.Count == 0)
                throw new ArgumentException("최소 한 개의 의뢰가 필요하다.", nameof(commissions));
            Commissions = commissions;
        }
    }
}
