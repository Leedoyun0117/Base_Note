using System;
using GameName.Core.Events;
using GameName.UI.Session;
using UnityEngine;

namespace GameName.UI.MemoryRoom.Space
{
    // 방마다 손으로 깔아 둔 배경 오브젝트를 방 전환에 맞춰 껐다 켠다.
    //
    // ── 왜 필요한가 ────────────────────────────────────────────────────
    // MemoryRoomSpaceView는 방 껍데기 하나를 재사용하고 단서 내용물만 갈아
    // 끼운다 — 그 뒤에 깔리는 배경 아트는 다루지 않는다(늘 켜진 장식). 방이
    // 하나일 때는 충분했지만, 방마다 다른 배경(그린룸 / 시계탑 …)을 쓰면
    // 지금 방의 것만 보여야 한다.
    //
    // ── 이미 놓은 것을 그대로 쓴다 ────────────────────────────────────
    // 배경을 컨테이너 하나로 묶으라고 강요하지 않는다. 손으로 위치·정렬을
    // 맞춰 둔 스프라이트 레이어들을 새 부모 밑으로 옮기면 조명 리그·연출이
    // 딸려가며 어긋날 위험이 있어서다. 방마다 "켤 오브젝트 목록"을 그대로
    // 인스펙터에 넣는다 — 컨테이너 하나여도 되고, 레이어 낱개 여러 개여도 된다.
    //
    // 시작하자마자 첫 방의 배경이 켜져 있어야 한다 — Session이 아직 없으면
    // 목록의 첫 방을 켠다(배선 전 씬에서도 검은 화면이 되지 않게).
    [DisallowMultipleComponent]
    public sealed class RoomArtSwitcher : MonoBehaviour
    {
        [Serializable]
        public struct RoomArt
        {
            [Tooltip("이 배경이 속한 방의 식별자. DemoGameData의 room-1 / room-2 와 같아야 한다.")]
            public string RoomId;

            [Tooltip("그 방에서만 켜질 오브젝트들. 컨테이너 하나든, 스프라이트 레이어 낱개 여러 개든.")]
            public GameObject[] Objects;
        }

        [SerializeField] private GameSessionBootstrap _gameSession;

        [Tooltip("방 식별자 → 그 방에서 켤 오브젝트들. 여기 없는 방으로 넘어가면 목록의 모든 오브젝트가 꺼진다.")]
        [SerializeField] private RoomArt[] _rooms = Array.Empty<RoomArt>();

        [Tooltip("켜면 방 전환·토글 결과를 콘솔에 찍는다. 배선이 맞는지 확인용.")]
        [SerializeField] private bool _debugLog = true;

        private IDisposable _subscription;

        private void OnEnable()
        {
            var session = _gameSession != null ? _gameSession.Session : null;

            if (_debugLog)
                Debug.Log($"[RoomArtSwitcher] OnEnable — gameSession={( _gameSession == null ? "NULL" : _gameSession.name)}, " +
                          $"session={(session == null ? "NULL" : "OK")}, rooms={_rooms.Length}", this);

            // 배선 전이면(플레이 아님) 첫 방 배경만 켜 두고 끝낸다.
            if (session == null)
            {
                if (_rooms.Length > 0)
                    Show(_rooms, _rooms[0].RoomId, _debugLog);
                return;
            }

            // 첫 RoomStartedEvent는 이 컴포넌트가 켜지기 전(세션 조립 중)에 이미
            // 지나갔으므로, 지금 진행 중인 방으로 곧바로 맞춘다.
            _subscription = session.EventBus.Subscribe<RoomStartedEvent>(e =>
            {
                if (_debugLog)
                    Debug.Log($"[RoomArtSwitcher] RoomStartedEvent → {e.RoomId.Value}", this);
                Show(_rooms, e.RoomId.Value, _debugLog);
            });
            Show(_rooms, session.CurrentRoomId.Value, _debugLog);
        }

        private void OnDisable()
        {
            _subscription?.Dispose();
            _subscription = null;
        }

        // 활성 방의 오브젝트만 켜고 나머지는 끈다. 씬 없이 검증하는 쪽이 이걸
        // 직접 부른다 — 구독 배선(EventBus·Session)은 토글 규칙과 분리한다.
        public static void Show(RoomArt[] rooms, string activeRoomId) => Show(rooms, activeRoomId, log: false);

        public static void Show(RoomArt[] rooms, string activeRoomId, bool log)
        {
            if (rooms == null)
                return;

            // 끄기를 먼저 전부 끝낸 뒤 켠다. 한 방에 배경용 Global Light 2D가 들어
            // 있으면, 새 방을 켜는 순간 이전 방 것이 아직 살아 있어 "레이어에 Global
            // Light가 둘"이라는 URP 에러가 한 프레임 번쩍인다. 순서를 갈라 그 겹침을
            // 없앤다. 활성 방·비활성 방 목록에 같은 오브젝트가 있으면 활성이 이긴다.
            foreach (var room in rooms)
            {
                if (room.Objects == null || IsActive(room, activeRoomId))
                    continue;
                Toggle(room, false, activeRoomId, log);
            }

            foreach (var room in rooms)
            {
                if (room.Objects == null || !IsActive(room, activeRoomId))
                    continue;
                Toggle(room, true, activeRoomId, log);
            }
        }

        // 대소문자·앞뒤 공백 무시 — 인스펙터 오타에 안 걸리게.
        private static bool IsActive(RoomArt room, string activeRoomId) =>
            string.Equals(
                (room.RoomId ?? string.Empty).Trim(),
                (activeRoomId ?? string.Empty).Trim(),
                StringComparison.OrdinalIgnoreCase);

        private static void Toggle(RoomArt room, bool on, string activeRoomId, bool log)
        {
            var names = log ? new System.Text.StringBuilder() : null;
            foreach (var go in room.Objects)
            {
                if (go == null)
                    continue;
                go.SetActive(on);
                names?.Append(names.Length == 0 ? "" : ", ").Append(go.name);
            }

            if (log)
                Debug.Log($"[RoomArtSwitcher]   '{room.RoomId}' (활성 방 '{activeRoomId}') → " +
                          $"{(on ? "ON" : "OFF")}  [{names}]");
        }
    }
}
