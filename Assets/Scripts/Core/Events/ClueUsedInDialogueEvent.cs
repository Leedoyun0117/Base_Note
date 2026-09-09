using GameName.Core.Clues;

namespace GameName.Core.Events
{
    // 단서 하나를 대화의 답으로 내밀어 소모했다는 사실.
    //
    // 정답이었는지는 여기 없다 — 그것은 ClueAnsweredEvent다. 맞든 틀리든 내민
    // 물건은 손에서 나가므로, "손에서 나갔다"는 사실만 따로 알린다(가방 격자는
    // 이 사건만, 완주 판정은 ClueAnsweredEvent만 본다).
    public readonly struct ClueUsedInDialogueEvent
    {
        public ClueId ClueId { get; }

        public ClueUsedInDialogueEvent(ClueId clueId)
        {
            ClueId = clueId;
        }
    }
}
