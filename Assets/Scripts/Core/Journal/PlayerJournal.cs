using System;
using System.Collections.Generic;
using GameName.Core.Ampoules;
using GameName.Core.Emotions;
using GameName.Core.Events;
using GameName.Core.Inventory;
using GameName.Core.Judging;
using GameName.Core.MemoryRooms;

namespace GameName.Core.Journal
{
    // IJournal(쓰기) + IJournalReader(읽기)의 유일한 구현체.
    //
    // 클래스 이름을 네임스페이스(GameName.Core.Journal)와 다르게 PlayerJournal로
    // 둔다 — 둘 다 "Journal"이면, 이 타입을 쓰는 쪽이 GameName.Core.Journal과
    // 형제/상위 관계에 있는 네임스페이스(GameName.UI.Perfumery,
    // GameName.Core.Tests.EditMode 등)에 속할 때 컴파일러가 타입 대신 네임스페이스로
    // 먼저 해석해 CS0118 오류가 난다.
    //
    // 분석/제작/시향 기록은 ClueAnalyzedEvent, AmpouleCraftedEvent,
    // ScentJudgedEvent를 직접 구독해서 채운다 — 처리기들은 더 이상 IJournal을
    // 알지 못한다(중복 경로 제거, 자세한 근거는 설계 근거 문서 참고).
    //
    // IAmpouleStorage/IPlayerInventory를 읽기 전용으로 참조한다 — 앰플의
    // "지금 위치"를 이 타입이 별도로 추적하지 않고, 조회 시점에 두 저장소에
    // 직접 물어보고 계산하기 위함이다. 자체 상태로 추적하면 옮기기
    // (AmpouleTransferProcessor, 지금은 이벤트를 발행하지 않는다)와 같은
    // 변화를 놓쳐 실제 위치와 기록지 표시가 어긋날 수 있다.
    public sealed class PlayerJournal : IJournal, IJournalReader, IDisposable
    {
        private sealed class AmpouleEntry
        {
            public AmpouleId AmpouleId { get; }
            public MemoryRoomId TargetRoomId { get; }
            public Scent Scent { get; }
            public ScentJudgementResult? TestResult { get; set; }

            public AmpouleEntry(AmpouleId ampouleId, MemoryRoomId targetRoomId, Scent scent)
            {
                AmpouleId = ampouleId;
                TargetRoomId = targetRoomId;
                Scent = scent;
            }
        }

        private sealed class CommissionRecords
        {
            public readonly List<DialogueLine> Dialogue = new List<DialogueLine>();
            public readonly List<AnalysisRecord> Analyses = new List<AnalysisRecord>();
            public readonly List<AmpouleEntry> Ampoules = new List<AmpouleEntry>();
        }

        private readonly IAmpouleStorage _storage;
        private readonly IPlayerInventory _inventory;
        private readonly Dictionary<CommissionId, CommissionRecords> _recordsByCommission =
            new Dictionary<CommissionId, CommissionRecords>();

        private CommissionId? _activeCommissionId;

        private readonly IDisposable _clueAnalyzedSubscription;
        private readonly IDisposable _ampouleCraftedSubscription;
        private readonly IDisposable _scentJudgedSubscription;

        public PlayerJournal(IEventBus eventBus, IAmpouleStorage storage, IPlayerInventory inventory)
        {
            if (eventBus == null) throw new ArgumentNullException(nameof(eventBus));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));

            _clueAnalyzedSubscription = eventBus.Subscribe<ClueAnalyzedEvent>(OnClueAnalyzed);
            _ampouleCraftedSubscription = eventBus.Subscribe<AmpouleCraftedEvent>(OnAmpouleCrafted);
            _scentJudgedSubscription = eventBus.Subscribe<ScentJudgedEvent>(OnScentJudged);
        }

        public CommissionId? ActiveCommissionId => _activeCommissionId;

        public void BeginCommission(CommissionId commissionId)
        {
            _activeCommissionId = commissionId;
            if (!_recordsByCommission.ContainsKey(commissionId))
                _recordsByCommission[commissionId] = new CommissionRecords();
        }

        public void RecordDialogue(DialogueLine line)
        {
            var records = ActiveRecordsOrNull();
            records?.Dialogue.Add(line);
        }

        public IReadOnlyList<DialogueLine> GetDialogue(CommissionId commissionId) =>
            new List<DialogueLine>(RecordsFor(commissionId).Dialogue);

        public IReadOnlyList<AnalysisRecord> GetAnalyses(CommissionId commissionId) =>
            new List<AnalysisRecord>(RecordsFor(commissionId).Analyses);

        public IReadOnlyList<AmpouleRecord> GetAmpoules(CommissionId commissionId)
        {
            var entries = RecordsFor(commissionId).Ampoules;
            var result = new List<AmpouleRecord>(entries.Count);
            foreach (var entry in entries)
                result.Add(ProjectAmpouleRecord(entry));

            return result;
        }

        private void OnClueAnalyzed(ClueAnalyzedEvent evt)
        {
            var records = ActiveRecordsOrNull();
            records?.Analyses.Add(new AnalysisRecord(evt.ClueId, evt.Result));
        }

        private void OnAmpouleCrafted(AmpouleCraftedEvent evt)
        {
            var records = ActiveRecordsOrNull();
            if (records == null)
                return;

            foreach (var recipe in evt.Recipes)
                records.Ampoules.Add(new AmpouleEntry(recipe.Id, recipe.TargetRoomId, recipe.Scent));
        }

        private void OnScentJudged(ScentJudgedEvent evt)
        {
            var records = ActiveRecordsOrNull();
            if (records == null)
                return;

            foreach (var entry in records.Ampoules)
            {
                if (!entry.AmpouleId.Equals(evt.AmpouleId))
                    continue;

                entry.TestResult = evt.Result;
                return;
            }

            // 시향된 앰플은 반드시 같은 의뢰 안에서 먼저 제작 기록이 있어야
            // 한다 — 없다면 기록 경로 어딘가가 깨졌다는 뜻이라 조용히
            // 넘기지 않는다.
            throw new InvalidOperationException(
                $"제작 기록이 없는 앰플({evt.AmpouleId})의 시향 결과를 연결할 수 없다.");
        }

        private CommissionRecords ActiveRecordsOrNull()
        {
            if (_activeCommissionId == null)
                return null;

            return _recordsByCommission[_activeCommissionId.Value];
        }

        private CommissionRecords RecordsFor(CommissionId commissionId)
        {
            if (_recordsByCommission.TryGetValue(commissionId, out var records))
                return records;

            return new CommissionRecords();
        }

        private AmpouleRecord ProjectAmpouleRecord(AmpouleEntry entry)
        {
            if (entry.TestResult.HasValue)
            {
                var testRecord = new ScentTestRecord(entry.TargetRoomId, entry.Scent, entry.TestResult.Value);
                return new AmpouleRecord(
                    entry.AmpouleId, entry.TargetRoomId, entry.Scent, AmpouleRecordState.Consumed, testRecord);
            }

            var state = StorageContains(entry.AmpouleId) ? AmpouleRecordState.InStorage : AmpouleRecordState.InInventory;
            return new AmpouleRecord(entry.AmpouleId, entry.TargetRoomId, entry.Scent, state, null);
        }

        private bool StorageContains(AmpouleId ampouleId)
        {
            foreach (var ampoule in _storage.Ampoules)
            {
                if (ampoule.Id.Equals(ampouleId))
                    return true;
            }

            return false;
        }

        public void Dispose()
        {
            _clueAnalyzedSubscription.Dispose();
            _ampouleCraftedSubscription.Dispose();
            _scentJudgedSubscription.Dispose();
        }
    }
}
