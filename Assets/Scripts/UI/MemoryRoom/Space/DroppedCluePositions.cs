using System.Collections.Generic;
using GameName.Core;
using GameName.Core.Clues;

namespace GameName.UI.MemoryRoom.Space
{
    // 플레이어가 단서를 버린 자리를 기억해 둔다.
    //
    // ── 왜 버린 자리를 유지하는가 ────────────────────────────────────────
    // 저작 위치가 생겼으니 버린 단서를 원래 자리로 되돌리는 선택지도 있었다.
    // 그러나 그건 이번 변경이 없애려는 문제("놓아둔 물건이 스스로 움직인다")를
    // 다른 모양으로 되살리는 것이다. 플레이어가 걸어가서 내려놓은 자리는
    // 자동 계산 값이 아니라 의도한 행동이므로, 저작 위치보다 더 존중받아야
    // 한다. 그래서 버린 자리가 저작 위치를 덮어쓴다 — 다만 덮어쓰는 것은
    // 이 표시용 기록뿐이고 저작 데이터 자체는 그대로다.
    //
    // ── 왜 여기 있는가 ──────────────────────────────────────────────────
    // 씬 컨트롤러의 필드로 두면 방을 옮기거나 다른 화면을 거칠 때마다 사라져
    // 단서가 제자리로 튀어 돌아간다. 그렇다고 Core에 둘 수도 없다 — 방 안
    // 어디쯤인지는 어떤 판정에도 쓰이지 않는 값이라, Core가 알면 규칙과
    // 무관한 좌표가 규칙 계층에 섞인다.
    //
    // 그래서 표시 계층에 두되 수명은 의뢰 하나만큼으로 맞춘다. Core에는
    // IResettable이라는 좁은 경계로만 노출되므로, CommissionSession은 "새
    // 의뢰가 시작되면 비워야 할 것이 하나 더 있다"는 사실만 알 뿐 그 안에
    // 무엇이 들었는지 알지 못한다.
    public sealed class DroppedCluePositions : IResettable
    {
        private readonly Dictionary<ClueId, CluePositionRatio> _positionsByClueId =
            new Dictionary<ClueId, CluePositionRatio>();

        public void Record(ClueId clueId, CluePositionRatio position) => _positionsByClueId[clueId] = position;

        public bool TryGet(ClueId clueId, out CluePositionRatio position) =>
            _positionsByClueId.TryGetValue(clueId, out position);

        public void Reset() => _positionsByClueId.Clear();
    }
}
