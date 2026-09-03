namespace GameName.Core.Restoration
{
    // 복원도 노드의 종류. 편집 권한이 여기서 갈린다.
    public enum RestorationNodeKind
    {
        // 색 하나의 뿌리(허브). 그 색으로 첫 추출이 일어날 때 딱 하나 생긴다.
        // 플레이어가 지우거나 이름을 바꿀 수 없다.
        ColorRoot,

        // 추출된 단서 하나에 대응하는 노드. 추출과 함께 자동으로 생기고 뿌리에
        // 잠긴 간선으로 이어진다. 라벨(단서 이름)은 잠겨 있다.
        ClueLinked,

        // 플레이어가 직접 적어 넣은 노드(배경·인물 메모 등). 이름 수정·삭제·간선
        // 연결이 전부 자유롭다.
        PlayerAuthored
    }
}
