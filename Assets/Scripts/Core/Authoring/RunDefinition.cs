using System;
using System.Collections.Generic;
using GameName.Core.Mind;

namespace GameName.Core.Authoring
{
    // 한 판 전체의 저작 데이터.
    //
    // 방 개수를 생성자에서 막지 않는 이유: 방이 몇 개여야 하는가는 기획 규칙이지
    // 이 타입이 성립하기 위한 조건이 아니다. 그런 규칙은 전부 검증기의 규칙
    // 하나(RoomCountRule)로 모아 두어야, 방 개수가 바뀌는 날 이 타입을 건드리지
    // 않고 검증기 조립만 고치면 된다.
    public sealed class RunDefinition
    {
        public IReadOnlyList<RoomDefinition> Rooms { get; }

        public int StartingTrust { get; }

        // 런 시작 시 손에 쥐고 시작하는 히로민. 대화 한 번(+3)마다 벌고, 추출
        // 한 번(-9)과 다음 기억으로 이동(-15)에 쓰는 단일 자원의 시작값이다.
        // 실제로 벌고 쓰는 것은 IHiromiMutator이고, 여기 있는 것은 그 시작값이다.
        public int StartingHiromi { get; }

        // 런당 총 기회. 히로민이 모자란 채로 다음 기억으로 강제 이동할 때마다
        // 하나씩 줄고, 0이 되면 런이 끝난다.
        public int StartingChance { get; }

        // 다음 기억으로 이동하는 데 드는 히로민이자, "지금 그냥 이동해도 되는가"를
        // 가르는 문턱이기도 하다(MemoryMoveProcessor는 이 값을 그대로 소모액으로
        // 쓴다). 상수로 박지 않는 이유: 밸런싱 값이고, 화면(HUD 게이지의 문턱
        // 표시, 가방 레버가 확인 팝업을 띄울지 판단하는 기준)도 같은 값을 알아야
        // 하는데 코드에 흩어져 박히면 저작이 바뀔 때 어딘가는 빠뜨리게 된다.
        public int MoveHiromiCost { get; }

        // 랜덤 분기 풀을 확정할 때 쓰는 시드. 런 시작 시 BranchResolver가 이
        // 값으로 각 풀의 후보를 결정적으로 고른다. 테스트에서 고정하려고 밖에서
        // 주입할 수 있게 데이터로 들고 있으며, 분기 풀이 없으면 아무 데도 안 쓰인다.
        public int Seed { get; }

        // 런 시작 시 나츠의 안정 축 위치와 그 축의 침체·흥분 양 끝.
        // -100..0..+100은 지금의 저작값일 뿐이라 상수가 아니라 데이터로 둔다.
        public int StartingStability { get; }
        public int StabilityMin { get; }
        public int StabilityMax { get; }

        // 신뢰(유키의 인내심)가 안정 축 이탈로 깎이는 규칙의 두 수치.
        // |안정 위치| 가 자유 폭(FreeBand) 이내면 깎이지 않고, 넘어서면 매 답변마다
        // round((|위치| - FreeBand) / Divisor) 만큼 깎인다. 계산은
        // StabilityTrustErosionListener가 하고, 여기 있는 것은 그 두 수치뿐이다.
        public int TrustErosionFreeBand { get; }
        public int TrustErosionDivisor { get; }

        // 런 시작 시 나츠의 심리 상태. 기억 해석 방식을 결정한다.
        public PsychologyState StartingPsychology { get; }

        public RunDefinition(
            IReadOnlyList<RoomDefinition> rooms,
            int startingTrust,
            int startingHiromi,
            int seed = 0,
            int startingChance = 2,
            int moveHiromiCost = 15,
            int startingStability = 0,
            int stabilityMin = -100,
            int stabilityMax = 100,
            int trustErosionFreeBand = 20,
            int trustErosionDivisor = 10,
            PsychologyState startingPsychology = PsychologyState.Optimism)
        {
            Rooms = rooms ?? throw new ArgumentNullException(nameof(rooms));
            StartingTrust = startingTrust;
            StartingHiromi = startingHiromi;
            Seed = seed;
            StartingChance = startingChance;
            MoveHiromiCost = moveHiromiCost;
            StartingStability = startingStability;
            StabilityMin = stabilityMin;
            StabilityMax = stabilityMax;
            TrustErosionFreeBand = trustErosionFreeBand;
            TrustErosionDivisor = trustErosionDivisor;
            StartingPsychology = startingPsychology;
        }
    }
}
