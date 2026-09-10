namespace GameName.UI.Session
{
    // RoomSceneMap.Plan이 내는 결정: 지금 방에 맞추려면 어떤 씬을 내리고 어떤
    // 씬을 올려야 하는가. 로더는 이 값만 받아 그대로 실행한다 — 판단과 부작용을
    // 갈라, 판단은 씬 없이 검증할 수 있게 한다.
    public readonly struct RoomSceneSwap
    {
        // 내릴 씬 이름. null이면 내릴 것 없음(첫 진입).
        public string ToUnload { get; }

        // 올릴 씬 이름. null이면 올릴 것 없음(이미 로드됨, 또는 이 방에 매핑된 씬 없음).
        public string ToLoad { get; }

        public RoomSceneSwap(string toUnload, string toLoad)
        {
            ToUnload = string.IsNullOrEmpty(toUnload) ? null : toUnload;
            ToLoad = string.IsNullOrEmpty(toLoad) ? null : toLoad;
        }

        public bool NoChange => ToUnload == null && ToLoad == null;

        public static RoomSceneSwap None => new RoomSceneSwap(null, null);
    }
}
