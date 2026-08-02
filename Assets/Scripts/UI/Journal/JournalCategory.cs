namespace GameName.UI.Journal
{
    // 기록지 화면의 탭 구분. 화면 전용 개념이라 Core에는 없다.
    // JournalScreenView의 public 이벤트/메서드 시그니처에 노출되므로 internal이
    // 아니라 public이어야 한다 — internal이면 CS0051/CS7025로 컴파일이 깨진다.
    public enum JournalCategory
    {
        Dialogue,
        Analysis,
        Ampoules
    }
}
