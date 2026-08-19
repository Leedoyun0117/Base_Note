using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GameName.UI.Editor.SceneSetup
{
    // 인스펙터에 드러나는 [SerializeField] 비공개 필드에 값을 넣어 주는 헬퍼.
    //
    // 이 도구를 위해 필드를 public으로 열지 않는다 — 그러면 런타임 코드의 캡슐화가
    // 에디터 편의 때문에 무너진다. SerializedObject는 인스펙터가 값을 넣는 것과
    // 똑같은 경로라, 런타임 타입은 아무것도 바꾸지 않아도 된다. 되돌리기(Ctrl+Z)와
    // 씬 더티 표시도 이 경로가 알아서 처리한다.
    //
    // 이미 같은 값이면 아무 일도 하지 않고 false를 돌려준다 — 도구를 여러 번
    // 돌렸을 때 "이번에 실제로 바뀐 것"만 보고되게 하기 위함이다.
    public static class SerializedFieldBinder
    {
        public static bool BindObject(Object target, string fieldName, Object value, SceneSetupReport report)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(fieldName);

            if (property == null)
            {
                report.Problem($"{target.name}의 {fieldName} 필드를 찾지 못했습니다(이름이 바뀌었을 수 있습니다).");
                return false;
            }

            if (property.objectReferenceValue == value)
                return false;

            property.objectReferenceValue = value;
            serialized.ApplyModifiedProperties();
            return true;
        }

        // 오브젝트 참조 배열. 이미 같은 목록이면 아무 일도 하지 않는다.
        public static bool BindObjectArray(
            Object target, string fieldName, IReadOnlyList<Object> values, SceneSetupReport report)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(fieldName);

            if (property == null || !property.isArray)
            {
                report.Problem($"{target.name}의 {fieldName} 배열 필드를 찾지 못했습니다.");
                return false;
            }

            if (IsSameList(property, values))
                return false;

            property.arraySize = values.Count;
            for (var i = 0; i < values.Count; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];

            serialized.ApplyModifiedProperties();
            return true;
        }

        private static bool IsSameList(SerializedProperty property, IReadOnlyList<Object> values)
        {
            if (property.arraySize != values.Count)
                return false;

            for (var i = 0; i < values.Count; i++)
            {
                if (property.GetArrayElementAtIndex(i).objectReferenceValue != values[i])
                    return false;
            }

            return true;
        }

        // 열거형은 값 자체가 아니라 선언 순서(enumValueIndex)로 직렬화되므로,
        // 정수 값을 그대로 넣으면 어긋날 수 있다. intValue를 쓰면 실제 열거형
        // 값으로 들어간다.
        public static bool BindEnum(Object target, string fieldName, int enumValue, SceneSetupReport report)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(fieldName);

            if (property == null)
            {
                report.Problem($"{target.name}의 {fieldName} 필드를 찾지 못했습니다.");
                return false;
            }

            if (property.intValue == enumValue)
                return false;

            property.intValue = enumValue;
            serialized.ApplyModifiedProperties();
            return true;
        }
    }
}
