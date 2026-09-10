using System;
using GameName.Core.MemoryRooms;
using UnityEngine;

namespace GameName.UI.Session
{
    // 방 식별자 → 그 방에서 additive로 얹을 씬 이름. RoomSceneLoader가 참조한다.
    //
    // 씬 이름을 로더 컴포넌트에 인라인으로 박지 않고 에셋으로 빼는 이유는
    // MemoryRoomLayoutAsset과 같다 — 씬을 열지 않고도 매핑을 바꿀 수 있고,
    // 버전 관리에서 변경 내역이 그대로 보이고, 테스트가 이 에셋 하나만
    // 만들어 로더 로직을 씬 없이 검증할 수 있다.
    //
    // 방 id는 DemoGameData의 room-1 / room-2 / room-3 와 같아야 한다. 대소문자·
    // 앞뒤 공백은 무시한다(인스펙터 오타에 안 걸리게) — RoomArtSwitcher와 같은 관례.
    [CreateAssetMenu(menuName = "GameName/Room Scene Map", fileName = "RoomSceneMap")]
    public sealed class RoomSceneMap : ScriptableObject
    {
        [Serializable]
        private struct Entry
        {
            [Tooltip("방 식별자. DemoGameData의 room-1 / room-2 / room-3 와 같아야 한다.")]
            public string RoomId;

            [Tooltip("그 방에서 additive로 얹을 씬 이름. 빌드 설정에 등록돼 있어야 한다.")]
            public string SceneName;
        }

        [SerializeField] private Entry[] _entries = Array.Empty<Entry>();

        // roomId에 매인 씬 이름. 매핑이 없으면 null — 로더는 이 경우 배경 없이
        // 방을 진행한다(아직 안 만든 방 씬 때문에 런이 막히지 않게).
        public string SceneFor(string roomId)
        {
            var key = Normalize(roomId);
            if (key.Length == 0)
                return null;

            foreach (var entry in _entries)
            {
                if (Normalize(entry.RoomId) == key)
                {
                    var scene = entry.SceneName?.Trim();
                    return string.IsNullOrEmpty(scene) ? null : scene;
                }
            }

            return null;
        }

        public string SceneFor(MemoryRoomId roomId) => SceneFor(roomId.Value);

        // 로더가 "지금 방에 맞춰 무엇을 내리고 무엇을 올릴지" 판단하는 순수 함수.
        // 씬 로드/언로드(부작용)는 로더가 하고, 이 결정은 씬 없이 검증한다.
        public RoomSceneSwap Plan(string currentScene, string nextRoomId)
        {
            var target = SceneFor(nextRoomId);
            var current = string.IsNullOrEmpty(currentScene) ? null : currentScene.Trim();

            // 매핑이 없으면: 지금 방 씬만 내리고 새로 올리지 않는다(배경 없는 방).
            if (target == null)
                return new RoomSceneSwap(current, null);

            // 이미 그 씬이면 아무것도 안 한다.
            if (string.Equals(target, current, StringComparison.OrdinalIgnoreCase))
                return RoomSceneSwap.None;

            return new RoomSceneSwap(current, target);
        }

        private static string Normalize(string value) =>
            (value ?? string.Empty).Trim().ToLowerInvariant();
    }
}
