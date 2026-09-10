using System;

namespace GameName.Core.Complexes
{
    // 지금 활성인 컴플렉스 하나 — 그 정의와, 앞으로 몇 턴 더 사는지.
    //
    // 불변값이다. 지속 턴이 줄면 ActiveComplexList가 이 값을 새로 만들어
    // 갈아 끼운다(RestorationNode가 WithLabel로 갈리는 것과 같다).
    public readonly struct ActiveComplex
    {
        public ComplexDefinition Definition { get; }
        public int RemainingTurns { get; }

        public ActiveComplex(ComplexDefinition definition, int remainingTurns)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (remainingTurns < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(remainingTurns), remainingTurns, "남은 턴은 음수일 수 없다.");

            RemainingTurns = remainingTurns;
        }

        public ActiveComplex Decremented() =>
            new ActiveComplex(Definition, RemainingTurns - 1);

        public bool IsExpired => RemainingTurns <= 0;
    }
}
