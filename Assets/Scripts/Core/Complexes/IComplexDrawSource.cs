namespace GameName.Core.Complexes
{
    // 발생 확률에 걸렸을 때 "그럼 어떤 컴플렉스를 넣을 것인가"를 정하는 경계.
    //
    // 라운드마다 등장 가능한 컴플렉스 풀이 다르고(가라앉다/천사/무제), 뽑기
    // 규칙(가중치, 중복 방지 등)도 기획 대상이라 발생 리스너에 박지 않고
    // 떼어 낸다. 뽑을 것이 없으면(풀 소진 등) false.
    public interface IComplexDrawSource
    {
        bool TryDraw(int turn, out ComplexDefinition definition);
    }
}
