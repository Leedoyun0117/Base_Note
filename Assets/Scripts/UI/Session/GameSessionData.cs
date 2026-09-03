using System;
using System.Collections.Generic;
using GameName.Core.Authoring;
using GameName.Core.Clues;
using GameName.Core.Memories;
using GameName.Core.MemoryRooms;

namespace GameName.UI.Session
{
    // GameSession을 조립하는 데 필요한 세계 데이터 묶음 — 단서의 배치와 한 판의
    // 저작 데이터(RunDefinition)다.
    //
    // 이 타입은 규칙을 하나도 담지 않는다. 실제 콘텐츠 소스(레벨 에디터
    // 산출물, 세이브 데이터 등)가 생기면 이 묶음을 채우는 코드만 바뀐다.
    public sealed class GameSessionData
    {
        // 단서가 어느 방에 놓여 있는가 + 그 정의(진실 색 포함). 카탈로그로만 쓰인다.
        public IReadOnlyList<CluePlacement> CluePlacements { get; }

        // 방 식별자 목록(진행 순서).
        public IReadOnlyList<MemoryRoomId> RoomIds { get; }

        // 한 판의 저작 데이터 — 방별 단서·대사, 시작 신뢰도, 추출 자원.
        public RunDefinition Run { get; }

        // 신뢰도 → 방 가시 비율 대응표. 밸런싱 값이라 코드가 아니라 데이터로 온다.
        public IReadOnlyDictionary<int, float> VisibilityByTrust { get; }

        // 판을 시작할 때 지갑에 들고 있는 색별 개수. 추출 자원·시작 신뢰도와
        // 같은 성격의 시작 상태라 여기 데이터로 온다.
        public IReadOnlyDictionary<MemoryColor, int> StartingMemoryColors { get; }

        public GameSessionData(
            IReadOnlyList<CluePlacement> cluePlacements,
            IReadOnlyList<MemoryRoomId> roomIds,
            RunDefinition run,
            IReadOnlyDictionary<int, float> visibilityByTrust,
            IReadOnlyDictionary<MemoryColor, int> startingMemoryColors = null)
        {
            CluePlacements = cluePlacements ?? throw new ArgumentNullException(nameof(cluePlacements));
            RoomIds = roomIds ?? throw new ArgumentNullException(nameof(roomIds));
            Run = run ?? throw new ArgumentNullException(nameof(run));
            VisibilityByTrust = visibilityByTrust ?? throw new ArgumentNullException(nameof(visibilityByTrust));
            StartingMemoryColors = startingMemoryColors ?? new Dictionary<MemoryColor, int>();
        }
    }
}
