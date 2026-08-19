using UnityEngine;

namespace GameName.UI.MemoryRoom.Space
{
    // 씬 오브젝트를 "지웠다"는 말이 그 자리에서 참이 되게 하는 단 하나의 방법.
    //
    // Unity의 Destroy는 그 프레임이 끝난 뒤에야 실제로 지운다. 그 사이에도 파괴가
    // 예약된 오브젝트의 콜라이더와 렌더러는 멀쩡히 살아 있어서, 방을 다시 그린
    // 프레임 동안 이전 방의 단서와 새 방의 단서가 함께 존재한다. 마우스 판정은
    // 겹친 것 중 하나만 집어 오므로 이미 사라졌어야 할 단서가 잡히고(눌러도 아무
    // 반응이 없다), 화면에는 둘이 겹쳐 보인다.
    //
    // 그래서 즉시 지운다. "지금 씬에 있는 단서 오브젝트는 곧 지금 방의 단서"라는
    // 불변식이 프레임 중간을 포함한 어느 순간에도 참이어야 하기 때문이다. 먼저
    // 꺼서 콜라이더와 렌더러를 그 자리에서 무력화한 뒤 지우는 이유는, 즉시 파괴가
    // 허용되지 않는 호출 문맥(물리 콜백 등)에서도 최소한 판정에는 잡히지 않게
    // 남겨 두기 위한 안전장치다.
    //
    // 파괴 방식을 실행 모드에 따라 나누지 않는 것도 의도다. 예전에는 실행 중에는
    // Destroy, 에디터에서는 DestroyImmediate를 썼는데, 그러면 EditMode 테스트가
    // 보는 세계(즉시 사라짐)와 실제 플레이가 보는 세계(프레임 끝까지 남음)가
    // 달라진다 — 이번 버그가 테스트를 전부 통과하고도 살아남은 이유가 그것이다.
    internal static class SceneObjectLifetime
    {
        public static void Destroy(GameObject target)
        {
            if (target == null)
                return;

            target.SetActive(false);
            Object.DestroyImmediate(target);
        }
    }
}
