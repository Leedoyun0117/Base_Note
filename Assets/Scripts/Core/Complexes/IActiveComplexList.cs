namespace GameName.Core.Complexes
{
    // 활성 컴플렉스 목록을 실제로 바꿀 수 있는 경계.
    //
    // 목록에 컴플렉스를 넣을 자격이 있는 쪽(라운드 시작 시드, 발생 리스너)에만
    // 주입한다. 지속 턴 감소는 목록이 TurnAdvancedEvent를 스스로 듣고 하므로
    // 여기 없다 — 밖에서 턴을 "밀어 넣는" 문은 열지 않는다.
    public interface IActiveComplexList : IActiveComplexListReader
    {
        // 컴플렉스 하나를 활성화한다. 상한이 찼거나 같은 id가 이미 활성이면
        // false를 돌려주고 아무것도 바꾸지 않는다.
        bool TryActivate(ComplexDefinition definition);

        // 라운드 경계 등에서 목록을 통째로 비운다.
        void Reset();
    }
}
