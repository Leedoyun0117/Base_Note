using UnityEngine;

namespace GameName.UI.Authoring
{
    // 검증 규칙들이 쓰는 수치를 에셋으로 들고 있는 자리.
    //
    // 규칙 클래스들이 이 값을 스스로 들고 있지 않은 이유는 둘 다 기획·아트에
    // 따라 움직이는 값이기 때문이다. 방 개수는 기획이 정하고, 겹침 기준은
    // 포스터 크기와 방 길이가 정한다. 코드에 박아 두면 밸런싱할 때마다 빌드를
    // 다시 해야 한다.
    [CreateAssetMenu(menuName = "GameName/Script Validation Settings", fileName = "ScriptValidationSettings")]
    public sealed class ScriptValidationSettingsAsset : ScriptableObject
    {
        [SerializeField] private int _expectedRoomCount = 3;

        // 같은 종류의 단서 두 개가 이만큼도 떨어져 있지 않으면 경고한다.
        // 방 길이에 대한 비율이므로 방 크기가 바뀌어도 그대로 쓴다.
        [Range(0f, 1f)]
        [SerializeField] private float _minimumClueSeparation = 0.12f;

        public int ExpectedRoomCount => _expectedRoomCount;
        public float MinimumClueSeparation => _minimumClueSeparation;
    }
}
