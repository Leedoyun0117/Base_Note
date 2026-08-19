using UnityEngine;

namespace GameName.UI.MemoryRoom.Space
{
    // 기억 방 치수를 프로젝트 에셋으로 들고 있는 자리.
    //
    // 씬 오브젝트에 값을 흩어 놓지 않고 에셋 하나로 모으는 이유: 방 세 개가
    // 전부 같은 치수를 써야 하는데(같은 규격의 공간이라는 것이 기획 전제다)
    // 씬의 각 방 오브젝트가 자기 값을 들고 있으면 셋이 조용히 어긋날 수 있다.
    // 또 에셋이면 씬을 열지 않고도 값을 바꿀 수 있고 버전 관리에서 변경 내역이
    // 그대로 보인다.
    //
    // 기본값은 기획이 정한 "플레이어 키 1, 높이 4, 길이 7"에 맞춰 둔다 —
    // 코드의 매직 넘버가 아니라 에셋의 초기값이므로 인스펙터에서 언제든 바뀐다.
    [CreateAssetMenu(menuName = "GameName/Memory Room Layout", fileName = "MemoryRoomLayout")]
    public sealed class MemoryRoomLayoutAsset : ScriptableObject
    {
        [Header("방")]
        [SerializeField] private float _roomHeight = 4f;
        [SerializeField] private float _roomLength = 7f;
        [SerializeField] private float _wallThickness = 0.2f;

        [Header("플레이어")]
        [SerializeField] private float _playerHeight = 1f;
        [SerializeField] private float _playerWidth = 0.4f;
        [SerializeField] private float _playerMoveSpeed = 3f;

        [Header("배치")]
        [SerializeField] private float _edgeMargin = 0.6f;
        [SerializeField] private float _posterMountHeight = 2.4f;
        [SerializeField] private float _posterSize = 0.8f;
        [SerializeField] private float _floorObjectSize = 0.5f;

        [Header("드나드는 지점")]
        [SerializeField] private float _exitMarkerWidth = 0.5f;
        [SerializeField] private float _exitMarkerHeight = 1.6f;

        [Header("카메라")]
        [SerializeField] private float _cameraVerticalMargin = 0.6f;

        // 에셋 자체가 아니라 불변 값 객체로 변환해 넘긴다 — 계산 코드가
        // ScriptableObject에 묶이지 않게 하기 위함이다(ClueDefinition이
        // ClueInfo를 내보내는 것과 같은 이유의 단방향 변환이다).
        public MemoryRoomLayout ToLayout() =>
            new MemoryRoomLayout(
                _roomHeight, _roomLength, _wallThickness,
                _playerHeight, _playerWidth, _playerMoveSpeed,
                _edgeMargin, _posterMountHeight, _posterSize, _floorObjectSize,
                _exitMarkerWidth, _exitMarkerHeight, _cameraVerticalMargin);
    }
}
