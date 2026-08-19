using System.Collections.Generic;
using GameName.Core.Clues;
using UnityEngine;

namespace GameName.UI.MemoryRoom.Space
{
    // 플레이어가 버린 단서를 방 안 어느 자리에 내려놓을지 정하는 순수 계산.
    //
    // 예전에는 이 판단이 없었다 — 플레이어가 서 있던 가로 위치를 그대로 썼다.
    // 그 자리는 하필 두 가지 이유로 최악의 자리다.
    //   1) 플레이어 스프라이트가 단서보다 앞에 그려진다. 바닥 물건은 플레이어
    //      몸에 완전히 가려서, 분명히 버렸는데 방에 아무것도 나타나지 않은
    //      것처럼 보인다.
    //   2) 그 자리에 이미 다른 단서가 놓여 있는지 아무도 보지 않는다. 단서를
    //      보려고 그 앞까지 걸어간 다음 버리는 것이 가장 자연스러운 동작이라,
    //      두 단서가 정확히 겹치는 일이 오히려 쉽게 일어난다.
    //
    // 그래서 발밑이 아니라 옆에 내려놓고, 이미 찬 자리는 피한다. 자동으로 자리를
    // 옮기는 것이 저작 배치를 존중하지 않는 것 아니냐는 반론이 있을 수 있는데,
    // 여기서 옮기는 것은 "지금 버리는 단서를 어디에 둘 것인가"뿐이고 이미 놓여
    // 있는 단서는 하나도 건드리지 않는다. 남은 단서가 스스로 움직이던 예전
    // 문제와는 방향이 반대다.
    //
    // 저작 데이터 쪽에는 CluePositionsAreSeparatedRule이라는 같은 취지의 간격
    // 규칙이 이미 있었다. 규칙이 저작 시점에만 있고 플레이 중 배치에는 없었다는
    // 것이 이 버그의 모양이다.
    public static class DroppedCluePlacement
    {
        // occupiedPositions는 같은 방에 이미 놓여 있는 "같은 종류" 단서들의 자리다.
        // 종류가 다르면 포스터는 벽, 물건은 바닥이라 가로가 같아도 겹치지 않으므로
        // 비교하지 않는다 — 저작 데이터 검사 규칙이 종류를 나눠 보는 것과 같은
        // 이유이고, "창문 아래 떨어진 반지" 같은 배치는 오히려 만들고 싶은 그림이다.
        public static CluePositionRatio Resolve(
            MemoryRoomLayout layout,
            ClueKind kind,
            float playerX,
            IReadOnlyList<CluePositionRatio> occupiedPositions)
        {
            var desired = BesidePlayer(layout, kind, playerX);
            var separation = CluePlacementLayout.MinimumSeparationRatio(layout, kind);

            // 크기가 0인 단서는 서로 겹칠 수도 없다. 이 경우를 걸러 두지 않으면
            // 아래 탐색 횟수가 무한이 된다.
            if (separation <= 0f)
                return desired;

            if (IsFree(desired, occupiedPositions, separation))
                return desired;

            // 원하는 자리에서 한 칸씩 좌우로 번갈아 벌어지며 빈자리를 찾는다.
            // 한 칸이 곧 최소 간격이므로, 방을 한 번 훑고도 못 찾았다면 이 방에는
            // 더 놓을 자리가 없다는 뜻이다.
            var maximumSteps = Mathf.CeilToInt(1f / separation);
            for (var step = 1; step <= maximumSteps; step++)
            {
                var offset = separation * step;

                if (TryCandidate(desired.Value + offset, occupiedPositions, separation, out var right))
                    return right;

                if (TryCandidate(desired.Value - offset, occupiedPositions, separation, out var left))
                    return left;
            }

            // 자리가 없어도 버리기 자체는 성공해야 한다 — 규칙(무엇을 버릴 수
            // 있는가)은 Core가 이미 판단했고, 여기서 그 판단을 뒤집을 수는 없다.
            // 겹쳐 놓더라도 원하던 자리에 둔다.
            return desired;
        }

        // 플레이어 몸에 가려지지 않도록 옆으로 비켜 놓는다. 얼마나 비키는지는
        // 두 폭의 절반을 더한 값 — 서로 딱 닿는 최소 거리다. 방 오른쪽 끝이라
        // 오른쪽에 놓을 수 없으면 왼쪽에 놓는다.
        private static CluePositionRatio BesidePlayer(MemoryRoomLayout layout, ClueKind kind, float playerX)
        {
            var gap = (layout.PlayerWidth + CluePlacementLayout.SizeOf(layout, kind)) / 2f;

            var toTheRight = playerX + gap;
            if (toTheRight <= RoomGeometry.MaxInteriorX(layout))
                return CluePlacementLayout.RatioOf(layout, toTheRight);

            return CluePlacementLayout.RatioOf(layout, playerX - gap);
        }

        private static bool TryCandidate(
            float value,
            IReadOnlyList<CluePositionRatio> occupiedPositions,
            float separation,
            out CluePositionRatio candidate)
        {
            candidate = default;

            // 방 밖으로 벗어난 후보는 버린다 — 비율을 잘라서 억지로 끌어들이면
            // 벽에 붙은 자리에 여러 단서가 도로 겹친다.
            if (value < CluePositionRatio.Minimum || value > CluePositionRatio.Maximum)
                return false;

            candidate = new CluePositionRatio(value);
            return IsFree(candidate, occupiedPositions, separation);
        }

        private static bool IsFree(
            CluePositionRatio position,
            IReadOnlyList<CluePositionRatio> occupiedPositions,
            float separation)
        {
            foreach (var occupied in occupiedPositions)
            {
                if (position.DistanceTo(occupied) < separation)
                    return false;
            }

            return true;
        }
    }
}
