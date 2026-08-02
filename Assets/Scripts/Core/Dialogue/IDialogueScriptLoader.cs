namespace GameName.Core.Dialogue
{
    // 진행 중인 대사 대본을 다른 의뢰의 대본으로 통째로 갈아 끼우는 권한
    // 하나만 표현하는 좁은 경계. IDialogueProgressor(정상 조회/진행 인터페이스)
    // 에는 이 능력이 없다 — FlowOverlay는 화면 전환에도 다시 만들어지지 않는
    // 상시 컴포넌트라 DialogueProgressor 객체 자체를 새로 만들 수 없고, 반드시
    // 이 인터페이스로 내부 대본만 바꿔치기해야 한다.
    public interface IDialogueScriptLoader
    {
        void LoadScript(DialogueScript script);
    }
}
