using GameName.Core.MemoryRooms;
using GameName.UI.MemoryRoom;
using GameName.UI.MemoryRoom.Space;
using GameName.UI.Overlays;
using GameName.UI.Session;
using GameName.UI.Shared;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameName.UI.Diagnostics
{
    // 진단이 볼 대상들을 씬에서 찾아 두는 자리.
    //
    // 매번 다시 찾는 이유: 기억 방 화면은 SceneScreenSwitcher가 껐다 켜고, 그때마다
    // 컨트롤러가 새로 만들어진다. 참조를 한 번만 잡아 두면 화면이 한 번 꺼진
    // 뒤부터 진단이 조용히 멈춰 버린다 — 진단이 침묵하는 것이 가장 나쁘다.
    // 찾기 비용은 프레임마다가 아니라 참조가 끊겼을 때만 낸다.
    internal sealed class ClueDebugContext
    {
        private GameSessionBootstrap _sessionBootstrap;
        private MemoryRoomBootstrap _roomBootstrap;
        private MemoryRoomSpaceView _view;
        private ScenePointerInput _pointerInput;
        private OverlayPanelHost _overlayPanels;

        public GameSession Session => SessionBootstrap() == null ? null : SessionBootstrap().Session;

        public MemoryRoomSpaceView View
        {
            get
            {
                if (_view == null)
                    _view = Object.FindFirstObjectByType<MemoryRoomSpaceView>(FindObjectsInactive.Include);

                return _view;
            }
        }

        public ScenePointerInput PointerInput
        {
            get
            {
                if (_pointerInput == null)
                    _pointerInput = Object.FindFirstObjectByType<ScenePointerInput>(FindObjectsInactive.Include);

                return _pointerInput;
            }
        }

        public MemoryRoomBootstrap RoomBootstrap
        {
            get
            {
                if (_roomBootstrap == null)
                    _roomBootstrap = Object.FindFirstObjectByType<MemoryRoomBootstrap>(FindObjectsInactive.Include);

                return _roomBootstrap;
            }
        }

        public OverlayPanelHost OverlayPanels
        {
            get
            {
                if (_overlayPanels == null)
                    _overlayPanels = Object.FindFirstObjectByType<OverlayPanelHost>(FindObjectsInactive.Include);

                return _overlayPanels;
            }
        }

        public MemoryRoomLayout Layout
        {
            get
            {
                if (_roomBootstrap == null)
                    _roomBootstrap = Object.FindFirstObjectByType<MemoryRoomBootstrap>(FindObjectsInactive.Include);

                var asset = ClueDebugReflection.LayoutAssetOf(_roomBootstrap);
                return asset == null ? null : asset.ToLayout();
            }
        }

        // 지금 어떤 오버레이가 떠 있는가. 클릭 보고에서 "누르기 전"과 "누른 뒤"를
        // 나란히 찍어, 오버레이가 원래 떠 있었던 것인지 이번 클릭이 연 것인지를
        // 구분하는 데 쓴다 — 그 구분을 못 해서 성공한 클릭을 막힌 클릭으로 읽었다.
        public string OverlayStateText()
        {
            if (_overlayPanels == null)
                _overlayPanels = Object.FindFirstObjectByType<OverlayPanelHost>(FindObjectsInactive.Include);

            if (_overlayPanels == null)
                return "(호스트 없음)";

            if (!_overlayPanels.IsAnyVisible)
                return "없음";

            // System을 using하지 않는다 — UnityEngine.Object와 System.Object가
            // 부딪혀 아래 Find 호출이 모호해진다.
            foreach (OverlayPanel panel in System.Enum.GetValues(typeof(OverlayPanel)))
            {
                var root = _overlayPanels.RootOf(panel);
                if (root != null && root.style.display.value == DisplayStyle.Flex)
                    return panel.ToString();
            }

            return "떠 있음(어느 것인지 못 읽음)";
        }

        // HUD에 지금 떠 있는 안내 문구. 방 컨트롤러가 "이 단서는 더 이상 이 방에
        // 없습니다" 같은 사유를 여기로 보내므로, 확대 화면이 안 떴을 때 그 이유가
        // 이 한 줄에 남아 있는지로 원인이 갈린다.
        public string HudMessageText()
        {
            if (_roomBootstrap == null)
                _roomBootstrap = Object.FindFirstObjectByType<MemoryRoomBootstrap>(FindObjectsInactive.Include);

            if (_roomBootstrap == null)
                return string.Empty;

            var document = _roomBootstrap.GetComponent<UIDocument>();
            var root = document == null ? null : document.rootVisualElement;
            if (root == null)
                return string.Empty;

            var label = root.Q<Label>("move-failure-message");
            return label == null ? string.Empty : label.text;
        }

        // 지금 인벤토리에 무엇이 들어 있는가. 방에서 단서가 사라졌을 때 그것이
        // 인벤토리로 옮겨 간 것인지(정상) 그냥 없어진 것인지(사고)를 가르려면
        // 방만 봐서는 알 수 없다.
        public string InventoryText()
        {
            var session = Session;
            if (session == null)
                return "(세션 없음)";

            var names = new System.Collections.Generic.List<string>();
            foreach (var item in session.Inventory.Items)
            {
                var clue = item as GameName.Core.Clues.ClueInfo;
                names.Add(clue == null ? item.GetType().Name : clue.Id.Value);
            }

            return names.Count == 0 ? "비어 있음" : string.Join(", ", names);
        }

        // 지금 서 있는 곳이 기억 방인가, 그렇다면 어느 방인가. 화면이 쓰는 것과
        // 똑같은 판정을 쓴다 — 진단이 자기만의 답을 따로 계산하면 화면과 다른
        // 것을 보게 된다.
        public bool TryGetCurrentRoom(out MemoryRoomId roomId)
        {
            roomId = default;

            var session = Session;
            if (session == null)
                return false;

            return CurrentRoomResolver.TryResolve(session.PlayerLocation.Current, session.RoomIds, out roomId);
        }

        public string CurrentPlaceText()
        {
            var session = Session;
            if (session == null)
                return "(세션 없음)";

            return TryGetCurrentRoom(out var roomId)
                ? roomId.Value
                : $"방 아님({session.PlayerLocation.Current.Value})";
        }

        public ClueKindLookup CurrentRoomClues() =>
            TryGetCurrentRoom(out var roomId) && Session != null
                ? ClueKindLookup.Of(Session.ClueTracker, roomId)
                : ClueKindLookup.Empty;

        private GameSessionBootstrap SessionBootstrap()
        {
            if (_sessionBootstrap == null)
                _sessionBootstrap = Object.FindFirstObjectByType<GameSessionBootstrap>(FindObjectsInactive.Include);

            return _sessionBootstrap;
        }
    }
}
