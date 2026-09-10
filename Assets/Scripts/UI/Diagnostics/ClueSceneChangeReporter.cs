using System.Collections.Generic;
using System.Text;

namespace GameName.UI.Diagnostics
{
    // 1번 관찰: 방을 그릴 때마다 무엇이 생기고 무엇이 사라졌는가.
    //
    // 뷰나 컨트롤러에 로그 줄을 심지 않고 밖에서 프레임마다 씬을 훑어 비교한다.
    // 두 가지 이점이 있다. 첫째, 게임 코드에 아무 흔적도 남지 않는다. 둘째,
    // "다시 그리는 경로"가 여러 개여도(이동, 버리기, 습득, 화면 재활성화) 전부
    // 똑같이 잡힌다 — 지금까지 두 번 빗나간 추정이 바로 "어느 경로가 도는가"에
    // 대한 것이었으므로, 경로를 가정하지 않는 관찰이 필요하다.
    //
    // 사라진 오브젝트는 참조가 정말 죽었는지까지 확인한다. 목록에서만 빠지고
    // 참조는 살아 있다면 파괴가 미뤄졌거나 다른 곳으로 옮겨진 것이고, 그 상태의
    // 콜라이더는 여전히 마우스 판정에 참여한다.
    internal sealed class ClueSceneChangeReporter
    {
        private readonly ClueDebugContext _context;
        private List<ClueObjectState> _previous = new List<ClueObjectState>();
        private string _previousPlace = "";

        public ClueSceneChangeReporter(ClueDebugContext context)
        {
            _context = context;
        }

        public void Tick()
        {
            var current = ClueSceneSnapshot.Capture(_context.View);
            var place = _context.CurrentPlaceText();

            if (!HasChanged(current, place))
            {
                _previous = current;
                _previousPlace = place;
                return;
            }

            Report(current, place);

            _previous = current;
            _previousPlace = place;
        }

        private bool HasChanged(List<ClueObjectState> current, string place)
        {
            if (place != _previousPlace || current.Count != _previous.Count)
                return true;

            for (var i = 0; i < current.Count; i++)
            {
                if (current[i].InstanceId != _previous[i].InstanceId)
                    return true;
            }

            return false;
        }

        private void Report(List<ClueObjectState> current, string place)
        {
            var kinds = _context.CurrentRoomClues();
            var currentIds = new HashSet<int>();
            foreach (var state in current)
                currentIds.Add(state.InstanceId);

            var destroyed = 0;
            var vanishedButAlive = new List<string>();
            foreach (var state in _previous)
            {
                if (currentIds.Contains(state.InstanceId))
                    continue;

                // Unity의 가짜 null: 참조가 살아 있으면 아직 파괴되지 않은 것이다.
                if (state.Component == null)
                    destroyed++;
                else
                    vanishedButAlive.Add(state.Describe("이전목록"));
            }

            var line = new StringBuilder();
            line.Append($"방그리기 | 위치={place}");
            line.Append($" | 씬오브젝트 {current.Count}개: {ClueSceneSnapshot.Describe(current, kinds)}");
            line.Append($" | Core가 말하는 이 방의 단서: {kinds.Describe()}");
            line.Append($" | 파괴된 이전 오브젝트 {destroyed}개");

            ClueDebugLog.Write(line.ToString());

            if (vanishedButAlive.Count > 0)
            {
                ClueDebugLog.Suspect(
                    $"목록에서는 빠졌는데 파괴되지 않은 오브젝트 {vanishedButAlive.Count}개 — " +
                    $"콜라이더가 아직 마우스 판정에 참여한다: {string.Join(" | ", vanishedButAlive)}");
            }

            ReportMismatch(current, kinds);
        }

        // 씬과 Core가 서로 다른 말을 하고 있는지 바로 알려 준다.
        private static void ReportMismatch(List<ClueObjectState> current, ClueKindLookup kinds)
        {
            var strays = new List<string>();
            foreach (var state in current)
            {
                if (state.ClueIdRead && !kinds.TryGet(state.ClueId, out _))
                    strays.Add(state.ClueIdText);
            }

            if (strays.Count > 0)
            {
                ClueDebugLog.Suspect(
                    $"씬에는 있는데 Core는 이 방에 없다고 하는 단서: {string.Join(", ", strays)}");
            }
        }
    }
}
