using System.IO;
using GameName.UI.MemoryRoom.Space;
using UnityEditor;
using UnityEngine;

namespace GameName.UI.Editor.SceneSetup
{
    // 방 치수 에셋을 찾고, 없으면 기본값으로 하나 만든다.
    //
    // 이미 있는 것을 덮어쓰지 않는 것이 중요하다 — 기획자가 인스펙터에서
    // 조정해 둔 값이 도구를 다시 돌렸다는 이유로 기본값으로 되돌아가면 안 된다.
    // 그래서 "찾기"가 먼저이고 "만들기"는 하나도 없을 때뿐이다.
    public static class MemoryRoomLayoutAssetLocator
    {
        private const string DefaultFolder = "Assets/Settings";
        private const string DefaultAssetPath = DefaultFolder + "/MemoryRoomLayout.asset";

        public static MemoryRoomLayoutAsset FindOrCreate(SceneSetupReport report)
        {
            var existing = FindExisting();
            if (existing != null)
                return existing;

            if (!AssetDatabase.IsValidFolder(DefaultFolder))
            {
                Directory.CreateDirectory(DefaultFolder);
                AssetDatabase.Refresh();
            }

            // 기본값은 에셋 자신의 직렬화 필드 초기값(높이 4 / 길이 7 / 키 1)이
            // 그대로 들어간다 — 도구가 숫자를 다시 적지 않는다.
            var created = ScriptableObject.CreateInstance<MemoryRoomLayoutAsset>();
            AssetDatabase.CreateAsset(created, DefaultAssetPath);
            AssetDatabase.SaveAssets();

            report.Created($"방 치수 에셋 {DefaultAssetPath}");
            return created;
        }

        private static MemoryRoomLayoutAsset FindExisting()
        {
            var guids = AssetDatabase.FindAssets($"t:{nameof(MemoryRoomLayoutAsset)}");
            if (guids.Length == 0)
                return null;

            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<MemoryRoomLayoutAsset>(path);
        }
    }
}
