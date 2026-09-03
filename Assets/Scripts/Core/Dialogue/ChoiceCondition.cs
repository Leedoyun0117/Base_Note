using GameName.Core.Clues;

namespace GameName.Core.Dialogue
{
    // 선택지 하나에 걸린 표시 조건. 판정은 하지 않고 "무엇이 필요한가"만 담는다.
    //
    // 조건이 스스로 IsSatisfied()를 들고 있지 않은 이유: 그러려면 이 값이
    // 해금 상태와 단서 상태를 알아야 하고, 그 순간 저작 데이터가 실행 시점의
    // 상태 저장소들에 묶인다. 검증기는 게임을 켜지 않은 채 이 데이터만 읽어야
    // 하므로 판정은 조건 밖에 둔다.
    //
    // 검열 쪽 조건은 대사 속 검열과 같은 판정(ICensorResolver.IsRevealed)을 그대로
    // 쓴다. 판정을 따로 두면 "대사에서는 이미 드러난 사실인데 그 사실을 아는 자만
    // 할 수 있는 말은 잠겨 있는" 어긋남이 생긴다.
    public readonly struct ChoiceCondition
    {
        public static readonly ChoiceCondition None = default;

        public ChoiceConditionKind Kind { get; }

        // Kind가 CensorKeyRevealed일 때만 값이 있다.
        public CensorKey? RequiredCensorKey { get; }

        // Kind가 ClueUsed일 때만 값이 있다.
        public ClueId? RequiredClue { get; }

        private ChoiceCondition(ChoiceConditionKind kind, CensorKey? censorKey, ClueId? clue)
        {
            Kind = kind;
            RequiredCensorKey = censorKey;
            RequiredClue = clue;
        }

        public static ChoiceCondition RequiresCensorKeyRevealed(CensorKey censorKey) =>
            new ChoiceCondition(ChoiceConditionKind.CensorKeyRevealed, censorKey, null);

        public static ChoiceCondition ClueUsed(ClueId clue) =>
            new ChoiceCondition(ChoiceConditionKind.ClueUsed, null, clue);
    }
}
