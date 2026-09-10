using UnityEngine;

namespace GameName.UI.Session
{
    // 씬에 하나만 두는 공유 구성 루트. 각 화면의 Bootstrap이 이 컴포넌트를
    // 인스펙터에서 참조해 같은 GameSession 인스턴스를 나눠 쓴다.
    //
    // DefaultExecutionOrder로 다른 화면의 Bootstrap보다 먼저 실행되게 한다.
    [DefaultExecutionOrder(-100)]
    public sealed class GameSessionBootstrap : MonoBehaviour
    {
        // 앞에서부터 몇 개의 라운드만 도는 판으로 할지. 0 = 전체(기본).
        [SerializeField] private int _roomLimit;

        public GameSession Session { get; private set; }

        private void Awake()
        {
            Session = new GameSession(DemoGameData.CreateWorldData(_roomLimit));
        }

        private void OnDestroy()
        {
            Session = null;
        }
    }
}
