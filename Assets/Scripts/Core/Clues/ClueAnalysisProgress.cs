using System.Collections.Generic;
using GameName.Core.Analysis;

namespace GameName.Core.Clues
{
    // IClueAnalysisProgress 기본 구현.
    //
    // IJournal과 역할이 다르다: 기록지는 플레이어가 나중에 다시 읽어보는
    // "대화록·분석 기록"이고, 이 타입은 "이미 아는 정보를 또 얻으려는 시도를
    // 막기 위한" 규칙 판정용 세션 상태다. 플레이어에게 보여주기 위한 것이
    // 아니며, 기록지와 달리 의뢰가 끝나면 Reset()으로 완전히 비워진다.
    public sealed class ClueAnalysisProgress : IClueAnalysisProgress, IResettable
    {
        private readonly Dictionary<ClueId, AnalysisDepth> _bestDepthByClueId = new Dictionary<ClueId, AnalysisDepth>();

        public bool TryGetBestDepth(ClueId clueId, out AnalysisDepth depth) =>
            _bestDepthByClueId.TryGetValue(clueId, out depth);

        public void RecordDepth(ClueId clueId, AnalysisDepth depth)
        {
            _bestDepthByClueId[clueId] = depth;
        }

        public void Reset()
        {
            _bestDepthByClueId.Clear();
        }
    }
}
