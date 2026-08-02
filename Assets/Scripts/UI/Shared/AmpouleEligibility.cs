using GameName.Core.MemoryRooms;

namespace GameName.UI.Shared
{
    // "이 앰플을 지금 방에서 시향할 수 있는가"라는 순수 판정 — 목표 방과 현재
    // 방이 같은지만 본다. 별도 Core 규칙이 필요 없을 만큼 단순하지만(시향
    // 처리기 자체도 같은 조건을 WrongRoom으로 이미 검사한다), 화면 쪽에서
    // "버튼을 활성화할지"를 미리 보여주기 위해 존재한다 — 실제 차단은 여전히
    // ScentTestingProcessor.Test가 한다. public으로 둔 이유는 별도 어셈블리의
    // 테스트에서 이 판정 하나만 직접 검증할 수 있게 하기 위함이다.
    public static class AmpouleEligibility
    {
        public static bool CanTestInCurrentRoom(MemoryRoomId targetRoomId, MemoryRoomId? currentRoomId) =>
            currentRoomId.HasValue && targetRoomId.Equals(currentRoomId.Value);
    }
}
