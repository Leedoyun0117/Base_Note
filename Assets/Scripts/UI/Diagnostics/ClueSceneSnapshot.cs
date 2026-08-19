using System.Collections.Generic;
using GameName.Core.Clues;
using GameName.UI.MemoryRoom.Space;
using UnityEngine;

namespace GameName.UI.Diagnostics
{
    // 지금 이 순간 씬에 실제로 존재하는 단서 오브젝트 하나의 상태.
    //
    // 컴포넌트 참조를 함께 들고 있는 것이 요점이다. 다음 프레임에 이 참조가
    // Unity의 가짜 null이 되었는지 보면 "정말 파괴되었는가"를 확인할 수 있고,
    // 반대로 목록에서는 사라졌는데 참조는 살아 있다면 어딘가로 옮겨졌거나
    // 파괴가 미뤄진 것이다 — 이번 버그에서 가장 확인하고 싶은 구분이다.
    internal readonly struct ClueObjectState
    {
        public ClueSceneObject Component { get; }
        public int InstanceId { get; }
        public string Name { get; }
        public ClueId ClueId { get; }
        public bool ClueIdRead { get; }
        public Vector3 WorldPosition { get; }
        public bool HasCollider { get; }
        public Bounds ColliderBounds { get; }
        public bool ActiveInHierarchy { get; }

        public ClueObjectState(ClueSceneObject component)
        {
            Component = component;
            InstanceId = component.GetInstanceID();
            Name = component.name;
            ClueIdRead = ClueDebugReflection.TryReadClueId(component, out var clueId);
            ClueId = clueId;
            WorldPosition = component.transform.position;
            ActiveInHierarchy = component.gameObject.activeInHierarchy;

            var collider = component.GetComponent<BoxCollider2D>();
            HasCollider = collider != null;
            ColliderBounds = HasCollider ? collider.bounds : default;
        }

        public string ClueIdText => ClueIdRead ? ClueId.Value : "(식별자를 읽지 못함)";

        public string Describe(string kindText)
        {
            var collider = HasCollider
                ? $"콜라이더 {Format(ColliderBounds.min)}~{Format(ColliderBounds.max)}"
                : "콜라이더 없음";

            var active = ActiveInHierarchy ? "" : ", 꺼져 있음";

            return $"{ClueIdText}/{kindText} 좌표{Format(WorldPosition)} {collider}{active}";
        }

        private static string Format(Vector3 value) => $"({value.x:0.###},{value.y:0.###})";
    }

    // 씬을 훑어 단서 오브젝트 상태를 모은다. 읽기만 하고 아무것도 바꾸지 않는다.
    internal static class ClueSceneSnapshot
    {
        public static List<ClueObjectState> Capture(MemoryRoomSpaceView view)
        {
            var states = new List<ClueObjectState>();
            if (view == null)
                return states;

            // 꺼져 있는 것까지 포함해서 센다 — "감춰졌을 뿐 살아 있는" 오브젝트가
            // 마우스 판정에 참여하는지가 이번 진단의 핵심 질문 중 하나다.
            foreach (var clue in view.GetComponentsInChildren<ClueSceneObject>(includeInactive: true))
                states.Add(new ClueObjectState(clue));

            return states;
        }

        public static string Describe(IReadOnlyList<ClueObjectState> states, ClueKindLookup kinds)
        {
            if (states.Count == 0)
                return "없음";

            var parts = new List<string>(states.Count);
            foreach (var state in states)
                parts.Add(state.Describe(kinds.KindTextOf(state)));

            return string.Join(" | ", parts);
        }
    }
}
