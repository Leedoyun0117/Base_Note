using System.Reflection;
using GameName.Core.Clues;
using GameName.UI.MemoryRoom;
using GameName.UI.MemoryRoom.Space;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameName.UI.Diagnostics
{
    // 진단이 들여다봐야 하는 비공개 필드를 읽는 자리. 리플렉션을 여기 한 곳에만
    // 모아 둔다.
    //
    // 왜 리플렉션인가: 로그를 찍자고 게임 클래스에 공개 속성을 새로 다는 것은
    // 진단이 끝난 뒤에도 남는 흔적이고, 무엇보다 "카메라를 어디서 얻는가",
    // "지금 무엇을 가리키고 있는가" 같은 것은 원래 밖에 알릴 필요가 없는 내부
    // 사정이다. 진단이 잠깐 훔쳐보는 것과 설계를 바꾸는 것은 다르다. 테스트가
    // ClueSceneObject의 _clueId를 같은 방식으로 읽고 있으므로 방식 자체도
    // 이 프로젝트에서 새로운 것이 아니다.
    //
    // 필드 이름이 바뀌면 여기서 null이 나오고 진단 줄에 "읽지 못함"이 찍힌다 —
    // 조용히 틀린 값을 찍는 것보다 낫다.
    internal static class ClueDebugReflection
    {
        private const BindingFlags InstanceFields = BindingFlags.NonPublic | BindingFlags.Instance;

        public static Camera CameraOf(ScenePointerInput pointerInput) =>
            Read<Camera>(pointerInput, "_camera");

        public static UIDocument[] UIDocumentsOf(ScenePointerInput pointerInput) =>
            Read<UIDocument[]>(pointerInput, "_uiDocuments");

        // 지금 강조 중인 대상. ScenePointerInput의 Update가 끝난 뒤에 읽어야
        // "그 코드가 실제로 무엇을 골랐는가"가 된다(프로브의 실행 순서를 뒤로
        // 미뤄 둔 이유다).
        public static object HoveredOf(ScenePointerInput pointerInput) =>
            Read<object>(pointerInput, "_hovered");

        public static bool TryReadClueId(ClueSceneObject sceneObject, out ClueId clueId)
        {
            clueId = default;

            var field = typeof(ClueSceneObject).GetField("_clueId", InstanceFields);
            if (field == null || sceneObject == null)
                return false;

            clueId = (ClueId)field.GetValue(sceneObject);
            return true;
        }

        public static MemoryRoomLayoutAsset LayoutAssetOf(MemoryRoomBootstrap bootstrap) =>
            Read<MemoryRoomLayoutAsset>(bootstrap, "_layoutAsset");

        private static T Read<T>(object target, string fieldName) where T : class
        {
            if (target == null)
                return null;

            var field = target.GetType().GetField(fieldName, InstanceFields);
            return field?.GetValue(target) as T;
        }
    }
}
