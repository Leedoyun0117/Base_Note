using UnityEngine;

namespace GameName.UI.Session
{
    // 씬에 하나만 두는 공유 구성 루트. 각 화면의 Bootstrap이 이 컴포넌트를
    // 인스펙터에서 참조해 같은 GameSession 인스턴스를 나눠 쓴다 — 그래서 어느
    // 화면에서 물건을 집거나 방을 옮겨도 다른 화면이 곧바로 같은 상태를 본다.
    //
    // DefaultExecutionOrder로 다른 화면의 Bootstrap보다 먼저 실행되게 한다 —
    // 화면 Bootstrap이 OnEnable에서 Session을 참조하는 시점에는 이미 조립이
    // 끝나 있어야 하기 때문이다(Unity는 서로 다른 오브젝트의 Awake 순서를
    // 기본적으로 보장하지 않는다).
    [DefaultExecutionOrder(-100)]
    public sealed class GameSessionBootstrap : MonoBehaviour
    {
        // 앞에서부터 몇 개의 방만 도는 판으로 할지. 0 = 전체(기본). 방 3을
        // 건드리지 않고 "앞 두 방짜리" 씬을 만드는 데만 쓰는 임시 손잡이다 —
        // 기존 씬의 컴포넌트는 이 필드가 없어 0으로 역직렬화되므로 동작이
        // 그대로다.
        [SerializeField] private int _roomLimit;

        public GameSession Session { get; private set; }

        private void Awake()
        {
            Session = new GameSession(
                DemoGameData.CreateWorldData(_roomLimit), DemoGameData.CreateSettings());
        }

        private void OnDestroy()
        {
            Session = null;
        }
    }
}
