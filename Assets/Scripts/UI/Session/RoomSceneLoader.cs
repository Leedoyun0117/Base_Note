using System;
using System.Collections;
using GameName.Core.Events;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameName.UI.Session
{
    // 방이 바뀌면 그 방의 배경 씬을 additive로 갈아 끼운다.
    //
    // ── 왜 필요한가 ────────────────────────────────────────────────────
    // 예전엔 방마다 손으로 깔아 둔 아트를 RoomArtSwitcher가 SetActive로 껐다
    // 켰다 했다(방 하나짜리 임시책). 이제 방마다 씬 하나 — 부트스트랩·UI·
    // 절차적 방 껍데기는 이 App 씬에 상주하고, 배경 아트+라이트만 방 씬으로
    // 빠져 additive로 오간다. GameSession은 App 씬에 있어 방 전환에도 살아남는다.
    //
    // ── 진실은 이벤트 ──────────────────────────────────────────────────
    // 어느 방인지는 RunProgressor가 RoomStartedEvent로 정한다. 이 로더는 그걸
    // 듣고 씬을 따라갈 뿐, 방 진행에 관여하지 않는다. 첫 RoomStartedEvent는 이
    // 컴포넌트가 켜지기 전(GameSession 조립 중)에 이미 지나갔으므로, OnEnable에서
    // 지금 진행 중인 방(session.CurrentRoomId)으로 곧바로 맞춘다 — RoomArtSwitcher와
    // 같은 패턴.
    //
    // ── 내리고 올리는 순서 ────────────────────────────────────────────
    // 옛 방 씬을 먼저 내린 뒤 새 방 씬을 올린다. 반대로 하면 두 방의 배경용
    // Global Light 2D가 한 프레임 겹쳐 URP가 "레이어에 Global Light가 둘"이라는
    // 에러를 뱉는다(RoomArtSwitcher 두-패스에서 겪은 것과 같은 함정). 전환 사이
    // 짧은 암전은 기억과 기억 사이라 자연스럽다 — 나중에 페이드로 감싸도 된다.
    [DisallowMultipleComponent]
    public sealed class RoomSceneLoader : MonoBehaviour
    {
        [SerializeField] private GameSessionBootstrap _gameSession;
        [SerializeField] private RoomSceneMap _map;

        [Tooltip("켜면 방 전환·씬 로드 결과를 콘솔에 찍는다. 배선 확인용.")]
        [SerializeField] private bool _debugLog;

        private IDisposable _subscription;
        private Coroutine _running;

        // 지금 얹혀 있는 방 씬 이름. 매핑이 없거나 로드 실패면 null.
        public string CurrentRoomScene { get; private set; }

        // 전환 코루틴이 도는 중인지. PlayMode 테스트가 전환 완료를 기다릴 때 본다.
        public bool IsSwapping => _running != null;

        // 전환 도중 다음 방 요청이 들어오면 여기 담아 두고, 돌던 코루틴이 끝나며 이어 처리한다.
        private string _queuedRoomId;

        private void OnEnable()
        {
            var session = _gameSession != null ? _gameSession.Session : null;
            if (session == null)
            {
                if (_debugLog)
                    Debug.LogWarning("[RoomSceneLoader] GameSession이 없다 — 방 씬 로드를 건너뛴다.", this);
                return;
            }

            _subscription = session.EventBus.Subscribe<RoomStartedEvent>(e => SwapTo(e.RoomId.Value));
            SwapTo(session.CurrentRoomId.Value);
        }

        private void OnDisable()
        {
            _subscription?.Dispose();
            _subscription = null;

            if (_running != null)
            {
                StopCoroutine(_running);
                _running = null;
            }
            _queuedRoomId = null;
        }

        private void SwapTo(string roomId)
        {
            if (_map == null)
            {
                if (_debugLog)
                    Debug.LogWarning("[RoomSceneLoader] RoomSceneMap이 없다.", this);
                return;
            }

            // 전환 중이면 최신 요청만 남겨 두고 코루틴이 끝날 때 이어 처리한다.
            if (_running != null)
            {
                _queuedRoomId = roomId;
                return;
            }

            var swap = _map.Plan(CurrentRoomScene, roomId);
            if (swap.NoChange)
                return;

            _running = StartCoroutine(Transition(swap, roomId));
        }

        private IEnumerator Transition(RoomSceneSwap swap, string roomId)
        {
            if (_debugLog)
                Debug.Log($"[RoomSceneLoader] {roomId}: 내림 '{swap.ToUnload}' → 올림 '{swap.ToLoad}'", this);

            if (swap.ToUnload != null)
            {
                var old = SceneManager.GetSceneByName(swap.ToUnload);
                if (old.IsValid() && old.isLoaded)
                    yield return SceneManager.UnloadSceneAsync(old);
            }

            if (swap.ToLoad != null)
            {
                if (Application.CanStreamedLevelBeLoaded(swap.ToLoad))
                {
                    if (!SceneManager.GetSceneByName(swap.ToLoad).isLoaded)
                        yield return SceneManager.LoadSceneAsync(swap.ToLoad, LoadSceneMode.Additive);
                    CurrentRoomScene = swap.ToLoad;
                }
                else
                {
                    // 아직 안 만든 방 씬 — 배경 없이 진행한다(런은 Core가 몰고 간다).
                    if (_debugLog)
                        Debug.LogWarning(
                            $"[RoomSceneLoader] 씬 '{swap.ToLoad}'을 빌드 설정에서 못 찾음 — 배경 없이 진행.", this);
                    CurrentRoomScene = null;
                }
            }
            else
            {
                CurrentRoomScene = null;
            }

            _running = null;

            // 전환 도중 들어온 방 요청이 있으면 이어서 처리한다.
            if (_queuedRoomId != null)
            {
                var next = _queuedRoomId;
                _queuedRoomId = null;
                SwapTo(next);
            }
        }
    }
}
