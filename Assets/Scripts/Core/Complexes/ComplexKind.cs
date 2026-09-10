namespace GameName.Core.Complexes
{
    // 컴플렉스가 서사 태그를 어떻게 다루는가 — 내부 분류다. UI에 직접 노출할
    // 필요는 없지만, 체인 적용기(ComplexChainResolver)가 규칙을 해석하는
    // 방식과 해석 로그의 표현이 이 값에 딸려 있으므로 밸런싱 데이터가 아니라
    // 타입으로 못박는다.
    //
    // ComplexDefinition.Kind는 그 컴플렉스의 대표 분류이고, 실제로 적용기가
    // 보고 동작하는 것은 규칙 하나하나에 달린 TagTransformRule.Kind다. 보통
    // 둘은 일치하며, 어긋난 저작은 검증 규칙(후속 단계)이 잡는다.
    public enum ComplexKind
    {
        // 변환형 — 걸린 태그를 다른 태그로 바꾼다.
        Transform,

        // 삭제형 — 걸린 태그를 지운다.
        Remove,

        // 추가형 — 조건이 맞으면 새 태그를 더한다.
        Add,

        // 증폭형 — 걸린 태그는 그대로 두고 강조 태그를 함께 얹는다.
        Amplify,

        // 거부형 — 걸린 태그를 지우고, 그 뒤 체인에서 같은 태그가 다시
        // 더해지는 것을 막는다.
        Reject
    }
}
