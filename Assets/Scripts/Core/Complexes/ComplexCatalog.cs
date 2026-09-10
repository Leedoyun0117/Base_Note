using System;
using System.Collections.Generic;

namespace GameName.Core.Complexes
{
    // 이 판에 등장할 수 있는 모든 컴플렉스 정의를 id로 찾을 수 있게 든 목록.
    //
    // 라운드 시작 컴플렉스(RoomDefinition.StartingComplexId)와 발생 풀
    // (RoomDefinition.ComplexPoolIds)이 id로만 가리키므로, 그 id를 실제 정의로
    // 푸는 자리가 하나 있어야 한다. 저작은 SO 패턴으로 채운다(후속 단계).
    public sealed class ComplexCatalog
    {
        private readonly Dictionary<ComplexId, ComplexDefinition> _byId = new Dictionary<ComplexId, ComplexDefinition>();

        public ComplexCatalog(IReadOnlyList<ComplexDefinition> definitions)
        {
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));

            foreach (var definition in definitions)
            {
                if (definition == null)
                    throw new ArgumentException("컴플렉스 목록에 null이 있다.", nameof(definitions));
                if (_byId.ContainsKey(definition.Id))
                    throw new ArgumentException($"컴플렉스 식별자가 중복되었다({definition.Id}).", nameof(definitions));

                _byId.Add(definition.Id, definition);
            }
        }

        public bool TryGet(ComplexId id, out ComplexDefinition definition) =>
            _byId.TryGetValue(id, out definition);

        public IReadOnlyCollection<ComplexDefinition> All => _byId.Values;
    }
}
