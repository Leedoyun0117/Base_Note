using System;
using System.Collections.Generic;
using GameName.Core.Events;

namespace GameName.Core.Complexes
{
    // 지금 활성인 컴플렉스 목록. 우선순위 순서로 정렬해 들고, 매 턴 지속 턴을
    // 줄이며, 0이 된 것을 스스로 걷어낸다.
    //
    // 옛 PsychologyTracker처럼 "단일 상태 + 변화 이벤트" 계열이지만, 여기서는
    // 여러 개가 동시에(최대 MaxConcurrent) 산다. 상한을 넘겨 새 컴플렉스가
    // 들어오려 하면 지금은 조용히 거절한다(TryActivate가 false) — 최저 우선순위를
    // 밀어내는 정책은 기획 결정이라 후속 단계로 남긴다.
    //
    // 지속 턴 감소는 TurnAdvancedEvent를 듣고 한다. "턴이 흘렀다"의 진실은
    // TurnCoordinator에 있고, 이 목록은 그 사건에 반응만 한다.
    //
    // 라운드 스코프인지 런 스코프인지(라운드가 바뀌면 비우는가)는 기획 확정
    // 대상이라, RoomStartedEvent를 구독하지 않고 Reset()만 열어 둔다 — 배선은
    // 후속 단계에서.
    public sealed class ActiveComplexList : IActiveComplexList
    {
        private readonly int _maxConcurrent;
        private readonly IEventBus _eventBus;
        private readonly List<ActiveComplex> _active = new List<ActiveComplex>();

        public ActiveComplexList(int maxConcurrent, IEventBus eventBus)
        {
            if (maxConcurrent < 1)
                throw new ArgumentOutOfRangeException(
                    nameof(maxConcurrent), maxConcurrent, "동시 활성 상한은 1 이상이어야 한다.");

            _maxConcurrent = maxConcurrent;
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));

            _eventBus.Subscribe<TurnAdvancedEvent>(_ => TickDown());
        }

        public IReadOnlyList<ActiveComplex> InPriorityOrder => new List<ActiveComplex>(_active);

        public IReadOnlyList<ComplexDefinition> DefinitionsInPriorityOrder
        {
            get
            {
                var result = new List<ComplexDefinition>(_active.Count);
                foreach (var entry in _active)
                    result.Add(entry.Definition);

                return result;
            }
        }

        public int Count => _active.Count;
        public int MaxConcurrent => _maxConcurrent;

        // 컴플렉스 하나를 활성화한다. 상한이 찼거나 같은 id가 이미 활성이면
        // false를 돌려주고 아무것도 바꾸지 않는다.
        public bool TryActivate(ComplexDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));

            if (_active.Count >= _maxConcurrent || Contains(definition.Id))
                return false;

            var entry = new ActiveComplex(definition, definition.DurationTurns);
            _active.Insert(InsertIndexFor(definition.Priority), entry);
            _eventBus.Publish(new ComplexActivatedEvent(definition.Id, entry.RemainingTurns));
            return true;
        }

        // 라운드 경계 등에서 목록을 통째로 비운다. 걷어낸 것마다 소멸 사건을 낸다.
        public void Reset()
        {
            var cleared = new List<ActiveComplex>(_active);
            _active.Clear();
            foreach (var entry in cleared)
                _eventBus.Publish(new ComplexExpiredEvent(entry.Definition.Id));
        }

        private void TickDown()
        {
            for (var i = _active.Count - 1; i >= 0; i--)
            {
                var next = _active[i].Decremented();
                if (next.IsExpired)
                {
                    _active.RemoveAt(i);
                    _eventBus.Publish(new ComplexExpiredEvent(next.Definition.Id));
                }
                else
                {
                    _active[i] = next;
                }
            }
        }

        private bool Contains(ComplexId id)
        {
            foreach (var entry in _active)
            {
                if (entry.Definition.Id.Equals(id))
                    return true;
            }

            return false;
        }

        // 우선순위 오름차순 자리. 같은 우선순위면 기존 것들 뒤에 넣어 활성화
        // 순서를 동률 타이브레이크로 지킨다.
        private int InsertIndexFor(int priority)
        {
            for (var i = 0; i < _active.Count; i++)
            {
                if (_active[i].Definition.Priority > priority)
                    return i;
            }

            return _active.Count;
        }
    }
}
