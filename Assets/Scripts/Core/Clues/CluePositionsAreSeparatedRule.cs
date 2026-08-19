using System.Collections.Generic;

namespace GameName.Core.Clues
{
    // 같은 방에서 같은 종류의 단서 둘이 너무 가까이 놓여 있으면 경고한다.
    //
    // 자동으로 밀어내지 않는 이유가 이 규칙의 핵심이다. 자리를 코드가 옮기기
    // 시작하면 "기획이 정한 자리에 그대로 놓인다"는 이번 변경의 전제가 다시
    // 무너진다 — 단서 하나를 집었다는 이유로 남은 단서가 움직이던 예전 문제와
    // 같은 종류의 배신이다. 그래서 런타임 동작은 하나도 바꾸지 않고, 저작
    // 시점에 사람에게만 알린다.
    //
    // 그렇다고 기획에 전부 맡기지도 않는다. 두 단서를 같은 비율에 적어 넣는
    // 것은 눈으로 찾기 어려운 실수인데(겹쳐서 하나로 보이고 클릭도 애매해진다)
    // 데이터만 보면 바로 드러나기 때문이다.
    //
    // 종류가 다르면 비교하지 않는다 — 포스터는 벽 높이, 바닥 물건은 바닥에
    // 놓이므로 가로 위치가 같아도 서로 겹치지 않는다. 오히려 "창문 아래
    // 떨어진 반지" 같은 배치는 의도적으로 만들고 싶은 그림이다.
    //
    // 최소 간격은 생성자로 주입받는다. 얼마나 떨어져야 충분한지는 단서 크기와
    // 방 길이에 따라 달라지는 표시 쪽 사정이라, 이 규칙이 스스로 정할 값이
    // 아니다.
    public sealed class CluePositionsAreSeparatedRule : IRoomDataConsistencyRule
    {
        private readonly float _minimumSeparation;

        public CluePositionsAreSeparatedRule(float minimumSeparation)
        {
            _minimumSeparation = minimumSeparation;
        }

        public IReadOnlyList<RoomDataIssue> Check(MemoryRoomData data)
        {
            var issues = new List<RoomDataIssue>();

            for (var i = 0; i < data.Clues.Count; i++)
            {
                for (var j = i + 1; j < data.Clues.Count; j++)
                {
                    var left = data.Clues[i];
                    var right = data.Clues[j];

                    if (left.Kind != right.Kind)
                        continue;

                    var separation = left.AuthoredPosition.DistanceTo(right.AuthoredPosition);
                    if (separation >= _minimumSeparation)
                        continue;

                    issues.Add(new RoomDataIssue(
                        RoomDataIssueSeverity.Warning,
                        $"단서 {left.Id}와 {right.Id}의 자리가 너무 가깝다" +
                        $"(간격 {separation:0.##}, 최소 {_minimumSeparation:0.##})."));
                }
            }

            return issues;
        }
    }
}
