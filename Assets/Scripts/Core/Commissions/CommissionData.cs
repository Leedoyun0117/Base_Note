using System;
using System.Collections.Generic;
using GameName.Core.Clues;
using GameName.Core.Dialogue;
using GameName.Core.Journal;
using GameName.Core.MemoryRooms;
using GameName.Core.Rewards;

namespace GameName.Core.Commissions
{
    // 의뢰 하나를 통째로 정의하는 데이터 번들 — 방 그래프(허브를 제외한 이
    // 의뢰만의 기억 방 노드/연결), 방별 정답+단서, 대화 대본, 보상 등급표를
    // 한데 묶는다.
    //
    // 허브(계단/분석실/조향실)와 그 사이 연결은 여기 담지 않는다 — 그건 모든
    // 의뢰가 공유하는 고정 지형이라 GameSessionData가 한 번만 들고 있고,
    // GameSession.LoadCommission이 이 번들의 방 노드/연결과 합쳐 그래프를
    // 완성한다. "기억으로 들어가는 시작 위치"도 별도 필드로 두지 않는다 —
    // 계단(허브)이라는 고정된 물리적 지점 자체는 의뢰마다 바뀌지 않고, 계단이
    // 실제로 어느 방과 연결되는지(=사실상의 시작 위치)는 이미 OpenConnections
    // 안에 포함되어 있으므로 같은 사실을 두 곳에 중복해서 두지 않기 위함이다.
    public sealed class CommissionData
    {
        public CommissionId Id { get; }
        public IReadOnlyList<MemoryGraphNode> RoomNodes { get; }
        public IReadOnlyList<OpenConnection> OpenConnections { get; }
        public IReadOnlyList<LadderConnection> LadderConnections { get; }
        public IReadOnlyList<MemoryRoomData> RoomData { get; }
        public DialogueScript DialogueScript { get; }
        public RewardTable RewardTable { get; }

        public CommissionData(
            CommissionId id,
            IReadOnlyList<MemoryGraphNode> roomNodes,
            IReadOnlyList<OpenConnection> openConnections,
            IReadOnlyList<LadderConnection> ladderConnections,
            IReadOnlyList<MemoryRoomData> roomData,
            DialogueScript dialogueScript,
            RewardTable rewardTable)
        {
            Id = id;
            RoomNodes = roomNodes ?? throw new ArgumentNullException(nameof(roomNodes));
            OpenConnections = openConnections ?? throw new ArgumentNullException(nameof(openConnections));
            LadderConnections = ladderConnections ?? throw new ArgumentNullException(nameof(ladderConnections));
            RoomData = roomData ?? throw new ArgumentNullException(nameof(roomData));
            DialogueScript = dialogueScript ?? throw new ArgumentNullException(nameof(dialogueScript));
            RewardTable = rewardTable ?? throw new ArgumentNullException(nameof(rewardTable));
        }
    }
}
