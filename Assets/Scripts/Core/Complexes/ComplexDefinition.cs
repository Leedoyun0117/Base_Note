using System;
using System.Collections.Generic;

namespace GameName.Core.Complexes
{
    // 컴플렉스 하나에 대해 기획이 적어 넣은 모든 것. 순수 데이터다 —
    // UnityEngine에 닿지 않으므로 검증기와 테스트가 게임을 켜지 않고 읽는다.
    //
    // 저작은 SO(*Asset + ToDefinition) 패턴을 그대로 따른다(후속 단계).
    public sealed class ComplexDefinition
    {
        public ComplexId Id { get; }

        // 화면·해석 로그가 "이 컴플렉스"를 사람 말로 부를 때 쓰는 이름("가라앉다").
        // 저작 데이터에서 흘러 들어오며, 비어 있으면 식별자를 그대로 쓴다.
        public string DisplayName { get; }

        // 이 컴플렉스가 서사를 어떻게 비트는지를 한 문장으로 적은 관찰형 서술.
        // 플레이어가 화면에서 읽고 결과를 스스로 추론하게 두는 문장이라 공략
        // 지시("~하지 마세요")는 담지 않는다. 저작 데이터에서 흘러 들어온다.
        public string Description { get; }

        // 체인 적용 순서. 낮을수록 먼저 적용된다 — 앞 컴플렉스의 출력이 다음
        // 컴플렉스의 입력이 되므로 순서가 결과를 바꾼다. 동시에 여러 컴플렉스가
        // 활성일 때 ActiveComplexList가 이 값으로 정렬한다.
        public int Priority { get; }

        // 이 컴플렉스가 몇 턴 동안 살아 있는가. 매 턴 하나씩 줄고 0이 되면
        // 소멸한다. 1 이상이어야 한다 — 0턴짜리는 활성화되자마자 사라져 아무
        // 의미가 없다.
        public int DurationTurns { get; }

        // 대표 분류. 적용기가 보고 동작하는 것은 각 규칙의 Kind이고, 이 값은
        // 해석 로그·저작 도구가 "이건 무슨 컴플렉스인가"를 한마디로 말할 때 쓴다.
        public ComplexKind Kind { get; }

        // 태그 집합에 차례로 적용할 규칙들. 한 컴플렉스 안에서 규칙 순서가
        // 결과에 영향을 주지 않도록 적용기가 종류별로 모아서 처리한다(주석은
        // ComplexChainResolver 참조).
        public IReadOnlyList<TagTransformRule> Rules { get; }

        public ComplexDefinition(
            ComplexId id,
            int priority,
            int durationTurns,
            ComplexKind kind,
            IReadOnlyList<TagTransformRule> rules,
            string displayName = null,
            string description = null)
        {
            if (durationTurns < 1)
                throw new ArgumentOutOfRangeException(
                    nameof(durationTurns), durationTurns, "지속 턴은 1 이상이어야 한다.");

            Id = id;
            Priority = priority;
            DurationTurns = durationTurns;
            Kind = kind;
            Rules = rules ?? throw new ArgumentNullException(nameof(rules));
            DisplayName = string.IsNullOrEmpty(displayName) ? id.Value : displayName;
            Description = description ?? string.Empty;
        }
    }
}
