namespace GameName.UI.Overlays
{
    // 플레이 화면 위에 전체를 덮고 뜨는 오버레이 화면의 종류.
    //
    // 한 번에 하나만 보인다는 규칙은 OverlayPanelRouter가 지킨다 — 종류가
    // 늘어나도 그 규칙을 다시 구현할 필요는 없다.
    public enum OverlayPanel
    {
        Inventory,

        // 단서를 눌렀을 때 뜨는 확대 화면. 키로 여는 것이 아니라 씬에서
        // 열리지만, 화면 전체를 덮는다는 점은 나머지와 똑같다 — 가시성을 여기
        // 함께 두면 두 오버레이가 겹쳐 뜨는 조합을 라우터가 알아서 막아 준다.
        ClueZoom
    }
}
