using System.IO;
using GameName.UI.Authoring;
using UnityEditor;
using UnityEngine;

namespace GameName.UI.Editor.SceneSetup
{
    // 가려진 대사 구간에 대신 그릴 문구 에셋(MemoryColorLabelAsset)을 찾고,
    // 없으면 시작값으로 하나 만든다.
    //
    // MemoryRoomLayoutAssetLocator와 같은 원칙이다 — 이미 있으면 덮지 않고,
    // 새로 만들 때만 비어 있는 색 이름 칸을 시드한다. 기획자가 이름이나 표기를
    // 바꿔 두었으면 도구를 다시 돌려도 그대로 둔다(BindStringIfEmpty).
    public static class MemoryColorLabelAssetLocator
    {
        private const string DefaultFolder = "Assets/Settings";
        private const string DefaultAssetPath = DefaultFolder + "/MemoryColorLabels.asset";

        public static MemoryColorLabelAsset FindOrCreate(SceneSetupReport report)
        {
            var existing = FindExisting();
            if (existing != null)
                return existing;

            if (!AssetDatabase.IsValidFolder(DefaultFolder))
            {
                Directory.CreateDirectory(DefaultFolder);
                AssetDatabase.Refresh();
            }

            var created = ScriptableObject.CreateInstance<MemoryColorLabelAsset>();
            AssetDatabase.CreateAsset(created, DefaultAssetPath);

            // 시작 색 이름 — 기획자가 인스펙터에서 바로 고칠 수 있는 자리다.
            SerializedFieldBinder.BindStringIfEmpty(created, "_redName", "빨강", report);
            SerializedFieldBinder.BindStringIfEmpty(created, "_greenName", "초록", report);
            SerializedFieldBinder.BindStringIfEmpty(created, "_blueName", "파랑", report);

            AssetDatabase.SaveAssets();

            report.Created($"기억색 문구 에셋 {DefaultAssetPath}");
            return created;
        }

        private static MemoryColorLabelAsset FindExisting()
        {
            var guids = AssetDatabase.FindAssets($"t:{nameof(MemoryColorLabelAsset)}");
            if (guids.Length == 0)
                return null;

            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<MemoryColorLabelAsset>(path);
        }
    }
}
