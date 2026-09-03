using System.Collections.Generic;
using System.Text;
using GameName.Core.Authoring;
using GameName.UI.Authoring;
using UnityEditor;
using UnityEngine;

namespace GameName.UI.Editor.Authoring
{
    // 저작 데이터를 지금 검사해 보는 메뉴.
    //
    // 검증기가 Core에 있어도 기획자가 돌릴 방법이 없으면 저작 시점 검사가
    // 아니다. 이 메뉴가 그 손잡이다 — 규칙 자체는 하나도 여기 없고, 조립은
    // RunValidationFactory에, 판단은 규칙들에 있다.
    public static class RunDefinitionValidationMenu
    {
        private const string MenuPath = "GameName/저작 데이터 검증";

        [MenuItem(MenuPath)]
        private static void Validate()
        {
            var run = Selection.activeObject as RunDefinitionAsset;
            if (run == null)
            {
                Debug.LogWarning($"{MenuPath}: 검사할 {nameof(RunDefinitionAsset)}를 먼저 선택해야 한다.");
                return;
            }

            var settings = FindSettings();
            if (settings == null)
            {
                Debug.LogWarning(
                    $"{MenuPath}: {nameof(ScriptValidationSettingsAsset)}가 프로젝트에 없다. " +
                    "검사 수치를 코드가 대신 정하지 않으므로 에셋을 먼저 만들어야 한다.");
                return;
            }

            var validator = DialogueScriptValidatorFactory.Create(
                settings.ExpectedRoomCount, settings.MinimumClueSeparation);

            var issues = validator.Validate(run.ToDefinition());
            if (issues.Count == 0)
            {
                Debug.Log($"{MenuPath}: {run.name} — 문제 없음.");
                return;
            }

            // 문제를 한 줄씩 따로 찍지 않고 심각도별로 묶어 두 번만 찍는다.
            // 콘솔이 수십 줄로 흐르면 정작 무엇이 진행을 막는지 보이지 않는다.
            Report(run, issues, ScriptIssueSeverity.Error);
            Report(run, issues, ScriptIssueSeverity.Warning);
        }

        private static void Report(
            RunDefinitionAsset run,
            IReadOnlyList<ScriptIssue> issues,
            ScriptIssueSeverity severity)
        {
            var text = new StringBuilder();
            var count = 0;

            foreach (var issue in issues)
            {
                if (issue.Severity != severity)
                    continue;

                text.Append("\n- ").Append(issue.Description);
                count++;
            }

            if (count == 0)
                return;

            var label = severity == ScriptIssueSeverity.Error ? "오류" : "경고";
            var header = $"{MenuPath}: {run.name} — {label} {count}건{text}";
            if (severity == ScriptIssueSeverity.Error)
                Debug.LogError(header, run);
            else
                Debug.LogWarning(header, run);
        }

        private static ScriptValidationSettingsAsset FindSettings()
        {
            var guids = AssetDatabase.FindAssets($"t:{nameof(ScriptValidationSettingsAsset)}");
            if (guids.Length == 0)
                return null;

            return AssetDatabase.LoadAssetAtPath<ScriptValidationSettingsAsset>(
                AssetDatabase.GUIDToAssetPath(guids[0]));
        }
    }
}
