using System;
using GameName.Core.Events;

namespace GameName.Core.MemoryRooms
{
    // 기억 방 그래프 위에서 실제 이동 시도를 처리한다.
    // 그래프(구조)와 현재 위치(IPlayerLocationMover)는 각자 자기 몫만 알고
    // 있고, 이 타입이 그것들을 합쳐 이동 가능 여부를 판단한다. 출발지는
    // 호출부가 넘기지 않는다 — IPlayerLocation이 "지금 어디에 있는가"의 단일
    // 진실 원천이므로, 호출부가 실제와 다른 출발지를 주장해서 불가능한 이동이
    // 통과하는 일 자체가 생길 수 없다.
    public sealed class MemoryRoomMovementProcessor
    {
        private readonly IMemoryRoomGraph _graph;
        private readonly IPlayerLocationMover _playerLocation;
        private readonly IEventBus _eventBus;

        public MemoryRoomMovementProcessor(
            IMemoryRoomGraph graph, IPlayerLocationMover playerLocation, IEventBus eventBus)
        {
            _graph = graph ?? throw new ArgumentNullException(nameof(graph));
            _playerLocation = playerLocation ?? throw new ArgumentNullException(nameof(playerLocation));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        public MemoryGraphMoveResult Move(MemoryGraphNodeId to)
        {
            var from = _playerLocation.Current;

            // 현재 위치가 이 그래프에 없다는 것은 호출부의 잘못된 인자가 아니라
            // 위치 추적기와 그래프가 서로 어긋난 내부 상태 불일치이므로, 인자
            // 예외가 아니라 상태 예외로 구분해서 드러낸다.
            if (!_graph.TryGetNode(from, out _))
                throw new InvalidOperationException($"현재 위치({from})가 그래프에 없는 노드다.");
            if (!_graph.TryGetNode(to, out _))
                throw new ArgumentException($"그래프에 없는 노드({to})다.", nameof(to));

            // 사다리도 열린 통로와 똑같이 "이어져 있는가"만 본다 — 둘의 차이는
            // 화면이 문과 사다리를 다르게 그린다는 표현상의 구분뿐이다.
            var connected =
                _graph.TryGetLadderLowerRoom(from, to, out _) || _graph.AreOpenlyConnected(from, to);
            if (!connected)
                return MemoryGraphMoveResult.Failure(MemoryGraphMoveFailureReason.NoConnection);

            // 여기까지 왔다는 것은 이동이 확정되었다는 뜻이다. 위치 갱신과 이벤트
            // 발행은 반드시 성공 경로에서만 일어나야 하므로, 실패로 반환하는 모든
            // return문보다 뒤에 둔다. 출발지와 목적지가 같다면 실제로는 위치가
            // 바뀐 게 아니므로 이벤트를 발행하지 않는다.
            _playerLocation.MoveTo(to);
            if (!to.Equals(from))
                _eventBus.Publish(new MemoryRoomMoveCompletedEvent(from, to));

            return MemoryGraphMoveResult.Success();
        }
    }
}
