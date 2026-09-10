using System;
using System.Collections.Generic;
using GameName.Core.Authoring;
using GameName.Core.Clues;
using GameName.Core.Complexes;
using GameName.Core.MemoryRooms;

namespace GameName.UI.Session
{
    // GameSession을 조립하는 데 필요한 세계 데이터 묶음.
    //
    // 규칙은 하나도 담지 않는다. 실제 콘텐츠 소스(레벨 에디터 산출물, 세이브
    // 데이터 등)가 생기면 이 묶음을 채우는 코드만 바뀐다.
    public sealed class GameSessionData
    {
        // 단서가 어느 라운드에 놓여 있는가 + 그 정의(서사·태그 포함). 카탈로그로만 쓰인다.
        public IReadOnlyList<CluePlacement> CluePlacements { get; }

        // 라운드 식별자 목록(진행 순서).
        public IReadOnlyList<MemoryRoomId> RoomIds { get; }

        // 한 판의 저작 데이터 — 라운드별 단서·턴 수·컴플렉스, 시드, 안정 축 범위.
        public RunDefinition Run { get; }

        // 이 판에 등장할 수 있는 모든 컴플렉스 정의.
        public IReadOnlyList<ComplexDefinition> Complexes { get; }

        // 감정 태그 값 → 안정 축 이동량. 최종 태그의 감정 축이 축을 얼마나 미는지.
        public IReadOnlyDictionary<string, int> EmotionShiftByValue { get; }

        // |안정 위치| → 그 턴에 새 컴플렉스가 생길 확률(계단식).
        public IReadOnlyDictionary<int, float> SpawnChanceByStabilityDistance { get; }

        public GameSessionData(
            IReadOnlyList<CluePlacement> cluePlacements,
            IReadOnlyList<MemoryRoomId> roomIds,
            RunDefinition run,
            IReadOnlyList<ComplexDefinition> complexes,
            IReadOnlyDictionary<string, int> emotionShiftByValue,
            IReadOnlyDictionary<int, float> spawnChanceByStabilityDistance)
        {
            CluePlacements = cluePlacements ?? throw new ArgumentNullException(nameof(cluePlacements));
            RoomIds = roomIds ?? throw new ArgumentNullException(nameof(roomIds));
            Run = run ?? throw new ArgumentNullException(nameof(run));
            Complexes = complexes ?? throw new ArgumentNullException(nameof(complexes));
            EmotionShiftByValue = emotionShiftByValue ?? throw new ArgumentNullException(nameof(emotionShiftByValue));
            SpawnChanceByStabilityDistance = spawnChanceByStabilityDistance
                ?? throw new ArgumentNullException(nameof(spawnChanceByStabilityDistance));
        }
    }
}
