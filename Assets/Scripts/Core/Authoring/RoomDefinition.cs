using System;
using System.Collections.Generic;
using GameName.Core.Clues;
using GameName.Core.Complexes;
using GameName.Core.MemoryRooms;

namespace GameName.Core.Authoring
{
    // 라운드 하나에 대해 기획이 적어 넣은 모든 것.
    //
    // (타입 이름은 아직 RoomDefinition이지만 3차 개편에서 "방"은 곧 "라운드"다 —
    // 가라앉다 / 천사 / 무제.)
    //
    // 방 치수(MemoryRoomLayout)는 여기 없다 — 표시 계층의 것이라 에셋으로
    // 공유된다. 이 타입은 "그 라운드에 무슨 단서가 있고, 몇 턴을 버텨야 하고,
    // 어떤 컴플렉스가 걸려 시작하는가"만 안다.
    public sealed class RoomDefinition
    {
        public MemoryRoomId Id { get; }
        public IReadOnlyList<ClueDefinition> Clues { get; }

        // 이 라운드를 클리어하려면 버텨야 하는 턴 수. 단서를 한 번 클릭해 읽으면
        // 한 턴이 지나가고, 이 수만큼 버티면 RoundSurvivedEvent가 난다.
        public int TurnsToSurvive { get; }

        // 라운드 시작 시 걸려 있는 컴플렉스. 없으면 null(컴플렉스 없이 시작).
        public ComplexId? StartingComplexId { get; }

        // 이 라운드에서 안정 축 이탈로 추가 발생할 수 있는 컴플렉스 후보들.
        // 발생 처리기(ComplexSpawnListener)가 이 풀에서 하나씩 뽑는다.
        public IReadOnlyList<ComplexId> ComplexPoolIds { get; }

        public RoomDefinition(
            MemoryRoomId id,
            IReadOnlyList<ClueDefinition> clues,
            int turnsToSurvive,
            ComplexId? startingComplexId = null,
            IReadOnlyList<ComplexId> complexPoolIds = null)
        {
            if (turnsToSurvive < 1)
                throw new ArgumentOutOfRangeException(
                    nameof(turnsToSurvive), turnsToSurvive, "버텨야 하는 턴 수는 1 이상이어야 한다.");

            Id = id;
            Clues = clues ?? throw new ArgumentNullException(nameof(clues));
            TurnsToSurvive = turnsToSurvive;
            StartingComplexId = startingComplexId;
            ComplexPoolIds = complexPoolIds ?? Array.Empty<ComplexId>();
        }
    }
}
