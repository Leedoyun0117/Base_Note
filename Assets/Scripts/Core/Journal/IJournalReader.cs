using System.Collections.Generic;

namespace GameName.Core.Journal
{
    // 기록지 읽기 경계. 쓰기(IJournal)와 분리해 화면은 이 인터페이스만
    // 알면 되고, 기록을 남기는 시스템은 이 인터페이스를 전혀 몰라도 된다.
    //
    // 반환하는 목록/기록 타입은 전부 불변이다 — 호출부가 받아간 것을 고쳐도
    // 기록지 내부 상태에 영향을 줄 수 없다.
    public interface IJournalReader
    {
        CommissionId? ActiveCommissionId { get; }

        IReadOnlyList<DialogueLine> GetDialogue(CommissionId commissionId);
        IReadOnlyList<AnalysisRecord> GetAnalyses(CommissionId commissionId);
        IReadOnlyList<AmpouleRecord> GetAmpoules(CommissionId commissionId);
    }
}
