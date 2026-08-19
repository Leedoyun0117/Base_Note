namespace GameName.UI.Overlays
{
    // 오버레이를 여는 키를 화면에 보여줄 한 줄로 만든다.
    //
    // 문구를 UXML에 박아 두지 않는 이유: 키는 OverlayPanelHost의 직렬화
    // 필드라 인스펙터에서 바뀔 수 있는데, 안내가 코드/마크업에 고정되어
    // 있으면 실제 키와 조용히 어긋난다. 여기서는 실제 설정값을 받아 문구를
    // 만들므로 둘이 갈라질 수 없다.
    //
    // 기록지와 인벤토리만 안내한다 — 이 둘은 화면에 버튼이 없어서 알려주지
    // 않으면 존재 자체를 알 수 없다. 지도와 시향은 상단 바에 버튼이 있다.
    public static class OverlayKeyHint
    {
        private const string Separator = " · ";

        public static string Describe(string journalKeyName, string inventoryKeyName) =>
            $"{journalKeyName} 기록지{Separator}{inventoryKeyName} 인벤토리(버리기)";
    }
}
