using System;
using System.Collections.Generic;

namespace GameName.Core.Authoring
{
    // 한 판 전체의 저작 데이터.
    //
    // 3차 개편에서 크게 줄었다: 히로민·기회·신뢰·심리 상태·왜곡 수치가 전부
    // 빠졌다. 남은 것은 라운드 목록과, 안정 축의 시작·범위, 결정적 난수 시드다.
    // 컴플렉스 발생 확률표·감정 이동표 같은 밸런싱 딕셔너리는 GameSessionData가
    // 든다(그릇 모양이 Core 타입이 아니라서).
    public sealed class RunDefinition
    {
        // 진행 순서대로의 라운드들.
        public IReadOnlyList<RoomDefinition> Rooms { get; }

        // 결정적 난수 시드. 컴플렉스 발생 굴림·뽑기가 이 값으로 재현된다.
        public int Seed { get; }

        // 런 시작 시 나츠의 안정 축 위치와 그 축의 침체·흥분 양 끝.
        public int StartingStability { get; }
        public int StabilityMin { get; }
        public int StabilityMax { get; }

        public RunDefinition(
            IReadOnlyList<RoomDefinition> rooms,
            int seed = 0,
            int startingStability = 0,
            int stabilityMin = -100,
            int stabilityMax = 100)
        {
            Rooms = rooms ?? throw new ArgumentNullException(nameof(rooms));
            Seed = seed;
            StartingStability = startingStability;
            StabilityMin = stabilityMin;
            StabilityMax = stabilityMax;
        }
    }
}
