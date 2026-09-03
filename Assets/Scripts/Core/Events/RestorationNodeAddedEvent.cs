using GameName.Core.Restoration;

namespace GameName.Core.Events
{
    // 복원도에 노드가 하나 생겼다는 사실. 자동 생성(뿌리·단서)이든 플레이어가
    // 놓은 것이든 똑같이 이 사건으로 알린다.
    //
    // 노드는 불변이라 그대로 실어 보낸다 — 받는 쪽이 나중에 이 값을 다시
    // 읽어도 생성 시점 그대로다(이후 이동·이름 변경은 별도 사건).
    public readonly struct RestorationNodeAddedEvent
    {
        public RestorationNode Node { get; }

        public RestorationNodeAddedEvent(RestorationNode node)
        {
            Node = node;
        }
    }
}
