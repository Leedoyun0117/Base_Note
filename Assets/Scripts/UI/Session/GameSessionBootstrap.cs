using UnityEngine;

namespace GameName.UI.Session
{
    // 씬에 하나만 두는 공유 구성 루트. 여러 화면(조향실, 기억 방 등)의
    // Bootstrap이 이 컴포넌트를 인스펙터에서 참조해 같은 GameSession 인스턴스를
    // 나눠 쓴다 — 그래서 어느 화면에서 정신력을 쓰거나 방을 옮겨도 다른 화면이
    // 곧바로 같은 상태를 보게 된다.
    //
    // DefaultExecutionOrder로 다른 화면의 Bootstrap보다 먼저 실행되게 한다 —
    // 화면 Bootstrap이 OnEnable에서 Session을 참조하는 시점에는 이미 조립이
    // 끝나 있어야 하기 때문이다(Unity는 서로 다른 오브젝트의 Awake 순서를
    // 기본적으로 보장하지 않는다).
    [DefaultExecutionOrder(-100)]
    public sealed class GameSessionBootstrap : MonoBehaviour
    {
        public GameSession Session { get; private set; }

        private void Awake()
        {
            Session = new GameSession(DemoGameData.CreateWorldData(), DemoGameData.CreateSettings());
            DemoGameData.SeedDemoState(Session);
        }

        private void OnDestroy()
        {
            Session?.Dispose();
            Session = null;
        }
    }
}
