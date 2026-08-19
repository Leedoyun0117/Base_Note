using System.Collections.Generic;
using GameName.Core.Clues;
using GameName.Core.MemoryRooms;

namespace GameName.UI.Diagnostics
{
    // "Core는 이 방에 무엇이 있다고 말하는가"를 한 번 물어 두는 자리.
    //
    // 씬 오브젝트는 자기 종류(포스터인지 바닥 물건인지)를 들고 있지 않다 —
    // 그게 이 설계의 경계이므로 진단이 그 경계를 흔들 이유는 없다. 대신 추적기에
    // 물어 식별자로 짝을 맞춘다. 덕분에 로그 한 줄에서 "Core가 말하는 목록"과
    // "씬에 실제로 있는 목록"을 나란히 비교할 수 있는데, 이번 버그에서 가장
    // 알고 싶은 것이 정확히 그 둘의 차이다.
    internal readonly struct ClueKindLookup
    {
        private readonly Dictionary<ClueId, ClueInfo> _byId;

        private ClueKindLookup(Dictionary<ClueId, ClueInfo> byId)
        {
            _byId = byId;
        }

        public static ClueKindLookup Empty => new ClueKindLookup(new Dictionary<ClueId, ClueInfo>());

        public static ClueKindLookup Of(IMemoryRoomClueTracker tracker, MemoryRoomId roomId)
        {
            var byId = new Dictionary<ClueId, ClueInfo>();
            if (tracker == null)
                return new ClueKindLookup(byId);

            foreach (var info in tracker.GetAvailableClueInfos(roomId))
                byId[info.Id] = info;

            return new ClueKindLookup(byId);
        }

        public bool TryGet(ClueId clueId, out ClueInfo info)
        {
            info = null;
            return _byId != null && _byId.TryGetValue(clueId, out info);
        }

        // 씬에는 있는데 Core 목록에는 없는 오브젝트를 눈에 띄게 표시한다 —
        // 이전 방의 잔재이거나 이미 집은 단서라는 뜻이기 때문이다.
        public string KindTextOf(ClueObjectState state)
        {
            if (!state.ClueIdRead)
                return "종류모름";

            return TryGet(state.ClueId, out var info) ? info.Kind.ToString() : "Core목록에없음";
        }

        public string Describe()
        {
            if (_byId == null || _byId.Count == 0)
                return "없음";

            var parts = new List<string>(_byId.Count);
            foreach (var pair in _byId)
                parts.Add($"{pair.Key.Value}/{pair.Value.Kind}/저작자리 {pair.Value.AuthoredPosition}");

            return string.Join(" | ", parts);
        }
    }
}
