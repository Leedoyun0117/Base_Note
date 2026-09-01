using System.Collections.Generic;
using GameName.Core.Clues;
using GameName.Core.Inventory;
using GameName.Core.MemoryRooms;

namespace GameName.UI.Session
{
    // 실제 기획 데이터(레벨 에디터 산출물, 세이브 데이터 등)가 생기기 전까지
    // 쓰는 더미 데이터.
    //
    // 이 파일 하나만 실제 데이터 소스로 교체하면 GameSession 이하 나머지 코드는
    // 전혀 바뀌지 않는다 — 그 지점을 명확히 하기 위해 더미 데이터 구성을 전부
    // 여기 한 파일에 모아 둔다.
    internal static class DemoGameData
    {
        public static readonly MemoryGraphNodeId Staircase = new MemoryGraphNodeId("staircase");

        public static readonly MemoryRoomId Room1 = new MemoryRoomId("room-1");
        public static readonly MemoryRoomId Room2 = new MemoryRoomId("room-2");
        public static readonly MemoryRoomId Room3 = new MemoryRoomId("room-3");

        public static GameSessionData CreateWorldData()
        {
            // 사다리로 이어진 세 방을 한 줄로 세운다. Room1이 허브에서 가장
            // 먼저 들어가는 방이라 Row가 가장 크다(가장 과거) — 사다리를 타고
            // 올라갈수록(Room2 -> Room3) Row가 작아진다.
            //
            // Column은 계단(1)과 겹치지 않는 3을 쓴다 — 계단과 같은 줄에 두면
            // 계단-Room1 직선 연결이 그 사이에 낀 Room2/Room3 자리를 관통해
            // 지나가면서 마치 계단이 그 방들과도 연결된 것처럼 보인다.
            var nodes = new List<MemoryGraphNode>
            {
                new MemoryGraphNode(Staircase, MemoryGraphNodeType.Staircase, new MemoryGraphCoordinate(1, 0)),
                new MemoryGraphNode(MemoryGraphNodeId.OfRoom(Room1), MemoryGraphNodeType.MemoryRoom, new MemoryGraphCoordinate(3, 3)),
                new MemoryGraphNode(MemoryGraphNodeId.OfRoom(Room2), MemoryGraphNodeType.MemoryRoom, new MemoryGraphCoordinate(3, 2)),
                new MemoryGraphNode(MemoryGraphNodeId.OfRoom(Room3), MemoryGraphNodeType.MemoryRoom, new MemoryGraphCoordinate(3, 1)),
            };

            var openConnections = new List<OpenConnection>
            {
                new OpenConnection(Staircase, MemoryGraphNodeId.OfRoom(Room1)),
            };

            var ladderConnections = new List<LadderConnection>
            {
                new LadderConnection(upperRoom: Room2, lowerRoom: Room1),
                new LadderConnection(upperRoom: Room3, lowerRoom: Room2),
            };

            var cluePlacements = new List<CluePlacement>();
            cluePlacements.AddRange(CreateRoomClues(Room1, posterAt: 0.18f, floorObjectAt: 0.72f));
            cluePlacements.AddRange(CreateRoomClues(Room2, posterAt: 0.82f, floorObjectAt: 0.28f));
            cluePlacements.AddRange(CreateRoomClues(Room3, posterAt: 0.35f, floorObjectAt: 0.88f));

            var roomIds = new List<MemoryRoomId> { Room1, Room2, Room3 };

            return new GameSessionData(
                nodes, openConnections, ladderConnections, cluePlacements, roomIds,
                // 시작 지점은 반드시 기억 방이어야 한다 — 허브(계단)에는 방이
                // 그려지지 않아(MemoryRoomSpaceController.Refresh) 출입구 오브젝트도
                // 없고, 그러면 씬에서 빠져나갈 방법이 없다.
                startNodeId: MemoryGraphNodeId.OfRoom(Room1));
        }

        public static GameSessionSettings CreateSettings()
        {
            // 확대 화면의 인벤토리 사이드바가 2x2로 그려지는 것과 맞물리는
            // 값이다. 다만 그 화면이 4를 전제로 그리는 것이 아니라, 이 값을 읽어
            // 칸 개수를 정한다 — 여기서 6으로 올리면 화면도 여섯 칸을 그린다.
            return new GameSessionSettings(new InventorySettings(initialCapacity: 4));
        }

        // 단서의 가로 자리는 방 길이에 대한 비율이다(0 = 왼쪽 끝, 1 = 오른쪽
        // 끝). 방마다 포스터와 바닥 물건을 서로 다른 자리에 두어, 배치가 코드가
        // 아니라 이 데이터에서 온다는 것이 눈으로 보이게 한다. 세로는 여기
        // 적지 않는다 — 포스터는 벽, 바닥 물건은 바닥이라는 것이 종류만으로
        // 이미 정해진다.
        private static IEnumerable<CluePlacement> CreateRoomClues(
            MemoryRoomId roomId, float posterAt, float floorObjectAt)
        {
            yield return new CluePlacement(roomId, new ClueDefinition(
                new ClueId($"clue-{roomId.Value}-poster"), ClueKind.Poster, new CluePositionRatio(posterAt)));

            yield return new CluePlacement(roomId, new ClueDefinition(
                new ClueId($"clue-{roomId.Value}-object"), ClueKind.FloorObject, new CluePositionRatio(floorObjectAt)));
        }
    }
}
