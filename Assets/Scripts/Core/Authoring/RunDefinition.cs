using System;
using System.Collections.Generic;

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

        // 한 판 동안 추출할 수 있는 총 횟수. 실제로 깎아 나가는 것은
        // IExtractionBudgetSpender이고, 여기 있는 것은 그 시작값이다.
        public int ExtractionBudget { get; }

        public RunDefinition(IReadOnlyList<RoomDefinition> rooms, int startingTrust, int extractionBudget)
        {
            Rooms = rooms ?? throw new ArgumentNullException(nameof(rooms));
            StartingTrust = startingTrust;
            ExtractionBudget = extractionBudget;
        }
    }
}
