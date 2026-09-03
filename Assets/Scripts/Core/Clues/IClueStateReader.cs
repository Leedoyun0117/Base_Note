namespace GameName.Core.Clues
{
    // 단서의 현재 단계를 읽기만 하는 경계.
    // 대사 조건 판정, 방 화면 표시처럼 상태를 근거로 판단만 하는 쪽은 이쪽만
    // 참조한다.
    public interface IClueStateReader
    {
        // 이 방에 등록된 단서가 아니면 예외다 — 단계를 물을 자격이 있는 쪽은
        // 그 단서가 이 방 것임을 이미 알고 있어야 한다.
        ClueState GetState(ClueId clueId);

        // 등록 여부를 모른 채 물어야 하는 쪽(다른 방 단서를 가리킬 수도 있는
        // 선택지 조건 판정 등)을 위한 관대한 조회. 없는 식별자는 예외가 아니라
        // false다.
        bool TryGetState(ClueId clueId, out ClueState state);
    }
}
