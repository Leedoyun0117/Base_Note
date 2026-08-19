namespace GameName.UI.Overlays
{
    // 오버레이 하나를 실제로 보이고 숨기는 방법. 라우터는 이 계약만 알고,
    // 그것이 UIDocument인지 GameObject인지 씬 카메라인지는 알지 못한다 —
    // 나중에 두 화면을 한 컨테이너의 탭으로 합칠 때 구현체만 갈아 끼우면 되게
    // 하기 위함이다.
    public interface IOverlayPanelContent
    {
        void SetVisible(bool visible);
    }
}
