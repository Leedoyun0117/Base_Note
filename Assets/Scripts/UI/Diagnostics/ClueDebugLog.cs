using UnityEngine;

namespace GameName.UI.Diagnostics
{
    // ── 진단용 임시 코드 ────────────────────────────────────────────────
    // 이 폴더(Assets/Scripts/UI/Diagnostics) 전체는 단서 상호작용 버그의 원인을
    // 찾기 위한 한시적 코드다. 원인이 확인되면 폴더를 통째로 지우고, 아래
    // "제거 목록"에 적힌 호출 한 줄과 씬의 ClueDebug 오브젝트만 함께 지우면
    // 프로젝트에 아무 흔적도 남지 않는다.
    //
    // 제거 목록:
    //   1) 이 폴더 전체
    //   2) InventoryScreenController.RequestDrop 안의 [ClueDebug] 표시가 붙은 한 줄
    //   3) 씬의 ClueDebug 게임 오브젝트
    //
    // 게임 로직은 이 폴더의 어떤 코드도 호출하지 않는다(위 2번 한 줄만 예외이며,
    // 그 줄도 결과를 받아 적기만 할 뿐 흐름을 바꾸지 않는다). Core에는 아무것도
    // 넣지 않는다 — Core는 UnityEngine을 참조하지 않아야 하기 때문이다.
    // ───────────────────────────────────────────────────────────────────
    //
    // 모든 진단 출력이 지나는 단 하나의 문. 태그를 여기서만 붙이므로 콘솔
    // 검색창에 [ClueDebug]를 넣으면 이 작업의 출력만 걸러 볼 수 있다.
    internal static class ClueDebugLog
    {
        public const string Tag = "[ClueDebug]";

        // 프로브(ClueDebugProbe)가 인스펙터 값을 그대로 밀어 넣는다. 프로브가
        // 씬에 없으면 이 폴더의 코드는 아예 돌지 않으므로 기본값은 의미가 없지만,
        // 실수로 어딘가에서 불렸을 때 조용한 쪽이 안전하다.
        public static bool Enabled { get; set; }

        public static void Write(string line)
        {
            if (Enabled)
                Debug.Log($"{Tag} {line}");
        }

        // 진단이 "이건 이상하다"고 판단한 것만 경고로 올린다 — 콘솔에서 노란
        // 줄만 훑어도 의심 지점이 먼저 보이게 하기 위함이다.
        public static void Suspect(string line)
        {
            if (Enabled)
                Debug.LogWarning($"{Tag} 의심: {line}");
        }
    }
}
