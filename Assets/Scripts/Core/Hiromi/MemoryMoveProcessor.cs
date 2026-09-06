using System;
using GameName.Core.Progression;

namespace GameName.Core.Hiromi
{
    // 플레이어가 스스로 "다음 기억으로 이동"을 선택하는 유일한 경로.
    //
    // 대화를 끝까지 보거나 신뢰가 0이 되어야만 방이 넘어가던 것과 달리, 이
    // 처리기는 그 어느 쪽도 기다리지 않고 곧장 RunProgressor.Advance()를
    // 부른다 — 이동 자체가 히로민을 쓰는 행동이므로, 완주와는 독립된 별개의
    // 나가는 문이다.
    //
    // 비용(moveCost)은 저작 데이터(RunDefinition.MoveHiromiCost)에서 온다 —
    // 코드에 박은 상수가 아니다. 화면(HUD 게이지의 문턱 표시, 가방 레버가
    // 확인 팝업을 띄울지 판단하는 기준)도 같은 값을 알아야 하기 때문이다.
    //
    // 가진 히로민이 그만큼 안 되면 실패하는 대신 "강제 이동"이 된다: 가진
    // 히로민을 전부 쓰고, 그 부족분의 대가로 기회를 하나 잃는다. 기회가
    // 그걸로 바닥나면 이동은 일어나지 않고 런이 그 자리에서 끝난다 —
    // ChanceTracker가 낸 ChanceChangedEvent를 ChanceExhaustionListener가 듣고
    // RunCompletedEvent를 낸다.
    public sealed class MemoryMoveProcessor
    {
        private readonly int _moveCost;
        private readonly IHiromiMutator _hiromi;
        private readonly IChanceTracker _chance;
        private readonly RunProgressor _runProgressor;

        public MemoryMoveProcessor(
            int moveCost, IHiromiMutator hiromi, IChanceTracker chance, RunProgressor runProgressor)
        {
            if (moveCost < 0)
                throw new ArgumentOutOfRangeException(nameof(moveCost), moveCost, "이동 비용은 음수일 수 없다.");

            _moveCost = moveCost;
            _hiromi = hiromi ?? throw new ArgumentNullException(nameof(hiromi));
            _chance = chance ?? throw new ArgumentNullException(nameof(chance));
            _runProgressor = runProgressor ?? throw new ArgumentNullException(nameof(runProgressor));
        }

        public MemoryMoveResult Move()
        {
            if (_hiromi.Remaining >= _moveCost)
            {
                _hiromi.Spend(_moveCost);
                _runProgressor.Advance();
                return MemoryMoveResult.Normal();
            }

            // 강제 이동 — 가진 히로민을 전부 쓴다(모자란 만큼은 기회로 대신 치른다).
            _hiromi.Spend(_hiromi.Remaining);
            _chance.Decrease();

            if (_chance.Remaining == 0)
                return MemoryMoveResult.ForcedMove(runEnded: true);

            _runProgressor.Advance();
            return MemoryMoveResult.ForcedMove(runEnded: false);
        }
    }
}
