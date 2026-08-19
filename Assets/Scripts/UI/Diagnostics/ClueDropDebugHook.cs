using GameName.Core.Clues;

namespace GameName.UI.Diagnostics
{
    // 버리기의 Core 처리 결과를 진단 쪽으로 넘기는 창구.
    //
    // 이 폴더에서 유일하게 게임 코드가 한 줄 불러 주는 곳이다
    // (InventoryScreenController.RequestDrop). 그 한 줄이 필요한 이유는 하나다:
    // 버리기가 "실패"했을 때는 이벤트가 발행되지 않으므로, 밖에서 지켜보는
    // 방식으로는 실패 사실도 사유도 영영 볼 수 없다. 성공만 보이는 진단은
    // "버렸는데 안 나타난다"는 증상 앞에서 아무 말도 해 주지 못한다.
    //
    // 결과를 받아 적기만 하고 아무것도 되돌려주지 않는다 — 부르는 쪽의 흐름은
    // 이 호출이 있든 없든 완전히 같다. 진단이 끝나면 그 한 줄과 이 파일을 함께
    // 지우면 된다.
    internal static class ClueDropDebugHook
    {
        public static ClueId LastClueId { get; private set; }
        public static bool HasResult { get; private set; }
        public static bool Succeeded { get; private set; }
        public static ClueDropFailureReason? FailureReason { get; private set; }

        public static void Record(ClueId clueId, ClueDropResult result)
        {
            LastClueId = clueId;
            HasResult = true;
            Succeeded = result.Succeeded;
            FailureReason = result.FailureReason;

            // 실패는 이벤트가 없어 다른 보고가 따라오지 않으므로 여기서 바로 남긴다.
            if (!result.Succeeded)
                ClueDebugLog.Write($"버리기 | {clueId.Value} | Core 결과=실패({result.FailureReason})");
        }

        public static void Clear() => HasResult = false;
    }
}
