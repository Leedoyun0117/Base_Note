using System.Collections.Generic;
using GameName.Core.Analysis;
using GameName.Core.Emotions;
using GameName.Core.Judging;
using GameName.Core.MemoryRooms;

namespace GameName.Core.Journal
{
    // 기록 남기기 경계.
    // 각 시스템은 이 인터페이스 뒤의 기록지 내부 구조(저장 형식, 표시 방식 등)를
    // 전혀 알지 못한 채 자신이 발생시킨 사건만 넘긴다.
    public interface IJournal
    {
        void RecordDialogue(DialogueLine line);
        void RecordAnalysis(string clueId, EmotionAnalysisResult result);
        void RecordAmpoulesCrafted(IReadOnlyList<Scent> recipes);
        void RecordScentTest(MemoryRoomId roomId, Scent testedScent, ScentJudgementResult result);
    }
}
