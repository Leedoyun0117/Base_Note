using System.Collections.Generic;
using System.Text;
using GameName.UI.MemoryRoom.Space;
using GameName.UI.Overlays;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace GameName.UI.Diagnostics
{
    // 2번 관찰: 마우스를 눌렀을 때 판정이 실제로 어디를 지나가는가.
    //
    // 마우스가 움직일 때마다 찍으면 콘솔이 쓸려 나가므로 버튼을 누른 프레임에만
    // 한 줄을 남긴다.
    //
    // ── 왜 두 번에 나눠 재는가 ──────────────────────────────────────────
    // 처음에는 ScenePointerInput보다 뒤에서 한 번에 전부 읽었다. 그랬더니 클릭이
    // 성공한 경우에도 "씬조작허용=False, UI가림=True"로 찍혔다 — 클릭이 확대
    // 화면을 여는 데 성공했고, 그 결과로 씬 조작이 꺼지고 오버레이가 화면을
    // 덮은 뒤의 상태를 읽었기 때문이다. 원인이 아니라 결과를 원인처럼 찍은
    // 셈이라, 그대로 두면 성공한 클릭이 전부 "UI에 막힘"으로 보인다.
    //
    // 그래서 판정의 입력이 되는 값(조작 허용 여부, UI 가림, 그 지점의 콜라이더)은
    // 실제 판정이 돌기 전에 재고, 결과(무엇이 골라졌는가)는 판정이 끝난 뒤에
    // 읽는다. 프로브의 실행 순서를 앞으로 당기고 보고를 LateUpdate로 미룬 것이
    // 그 때문이다.
    internal sealed class PointerPickReporter
    {
        private readonly ClueDebugContext _context;
        private readonly List<Collider2D> _hits = new List<Collider2D>();
        private ContactFilter2D _filter;

        private bool _hasPending;
        private ScenePointerInput _pendingPointerInput;
        private Vector2 _pendingScreenPosition;
        private Vector2 _pendingWorldPoint;
        private bool _pendingInputEnabled;
        private bool _pendingOccluded;
        private string _pendingOcclusionDetail;
        private string _pendingHits;
        private int _pendingInteractableCount;
        private string _pendingOverlayBefore;
        private string _pendingCoordinateSanity;

        public PointerPickReporter(ClueDebugContext context)
        {
            _context = context;

            // 게임 코드와 같은 조건으로 찍어야 같은 것을 본다(ScenePointerInput과
            // 같은 방식으로 만든다 — NoFilter()는 Unity 6에서 폐기되었다).
            _filter = new ContactFilter2D { useTriggers = true };
        }

        // 판정이 돌기 전(프로브의 Update, 실행 순서가 앞이다).
        public void CaptureBeforePick()
        {
            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
                return;

            var pointerInput = _context.PointerInput;
            if (pointerInput == null)
            {
                ClueDebugLog.Suspect("마우스를 눌렀지만 씬에 ScenePointerInput이 없다.");
                return;
            }

            var camera = ClueDebugReflection.CameraOf(pointerInput);
            if (camera == null)
            {
                ClueDebugLog.Suspect("ScenePointerInput에 카메라가 연결되어 있지 않다 — 판정 자체가 돌지 않는다.");
                return;
            }

            _pendingPointerInput = pointerInput;
            _pendingScreenPosition = mouse.position.ReadValue();
            _pendingWorldPoint = camera.ScreenToWorldPoint(_pendingScreenPosition);
            _pendingCoordinateSanity = PointerCoordinateSanity.Describe(_pendingScreenPosition, camera);
            _pendingInputEnabled = pointerInput.InputEnabled;

            // 좌표가 화면 밖이면 그 뒤의 판정은 전부 무의미하다. 그 사실을 조용히
            // 지나가면 "콜라이더가 없다"는 결론만 남아 엉뚱한 곳을 파게 된다.
            if (PointerCoordinateSanity.IsOutsideViewport(_pendingScreenPosition, camera))
            {
                ClueDebugLog.Suspect(
                    $"마우스 좌표가 화면 밖이다 — 클릭이 방에 도달하지 못했다. " +
                    $"화면({_pendingScreenPosition.x:0},{_pendingScreenPosition.y:0}), {_pendingCoordinateSanity}");
            }
            _pendingOverlayBefore = _context.OverlayStateText();
            _pendingOcclusionDetail = DescribeOcclusion(pointerInput, _pendingScreenPosition, out _pendingOccluded);
            _pendingHits = DescribeHits(_pendingWorldPoint, out _pendingInteractableCount);
            _hasPending = true;
        }

        // 판정이 끝난 뒤(프로브의 LateUpdate).
        public void ReportAfterPick()
        {
            if (!_hasPending)
                return;

            _hasPending = false;

            var line = new StringBuilder();
            line.Append($"클릭 | 화면({_pendingScreenPosition.x:0},{_pendingScreenPosition.y:0})");
            line.Append($" → 월드({_pendingWorldPoint.x:0.###},{_pendingWorldPoint.y:0.###})");
            line.Append($" | {_pendingCoordinateSanity}");
            line.Append($" | [누르기 전] 씬조작허용={_pendingInputEnabled}");
            line.Append($", 오버레이={_pendingOverlayBefore}");
            line.Append($", {_pendingOcclusionDetail}");
            line.Append($", {_pendingHits}");
            line.Append($" | [누른 뒤] 선택={DescribeHovered(_pendingPointerInput)}");
            line.Append($", 오버레이={_context.OverlayStateText()}");

            var reason = FailureReason();
            if (reason != null)
                line.Append($" | 선택없음이유={reason}");

            ClueDebugLog.Write(line.ToString());
        }

        // UI 가림 판정. 최종 결과뿐 아니라 "무엇이 잡혔는지"까지 적는다.
        //
        // 이 프로젝트의 UIDocument는 전부 같은 PanelSettings를 쓴다 — 즉 문서가
        // 여럿이어도 런타임 패널은 하나다. 그래서 어느 문서에 물어보든 Pick은
        // 같은 답을 돌려준다. 로그에 문서 이름을 그대로 남겨 두는 이유가 이것이다:
        // 네 문서가 전부 같은 요소를 가리키고 있으면 그 사실이 한눈에 보인다.
        private static string DescribeOcclusion(
            ScenePointerInput pointerInput, Vector2 screenPosition, out bool occluded)
        {
            var documents = ClueDebugReflection.UIDocumentsOf(pointerInput);
            if (documents == null || documents.Length == 0)
            {
                occluded = false;
                return "UI가림=문서목록이 비어 있음(UI가 클릭을 전혀 막지 않는 상태)";
            }

            occluded = UIPointerOcclusion.IsPointerOverPickableElement(documents, screenPosition);

            var picked = new List<string>();
            foreach (var document in documents)
            {
                if (document == null || !document.isActiveAndEnabled || document.rootVisualElement == null)
                    continue;

                var panel = document.rootVisualElement.panel;
                if (panel == null)
                    continue;

                var element = panel.Pick(RuntimePanelUtils.ScreenToPanel(panel, screenPosition));
                picked.Add($"{document.name}→{Describe(element)}");
            }

            return $"UI가림={occluded} [{string.Join(", ", picked)}]";
        }

        private static string Describe(VisualElement element)
        {
            if (element == null)
                return "없음";

            var name = string.IsNullOrEmpty(element.name) ? "(이름없음)" : element.name;

            // 잡힌 요소가 어느 문서에서 왔는지까지 적는다 — 패널이 공유라
            // "물어본 문서"와 "답으로 온 요소의 문서"가 다를 수 있기 때문이다.
            var owner = OwnerDocumentNameOf(element);
            return $"{name}:{element.GetType().Name}:{element.pickingMode}@{owner}";
        }

        private static string OwnerDocumentNameOf(VisualElement element)
        {
            var root = element;
            while (root.parent != null)
                root = root.parent;

            return string.IsNullOrEmpty(root.name) ? "(패널루트)" : root.name;
        }

        // 겹친 콜라이더를 전부 적는다. 하나만 적으면 "왜 하필 저것이 잡혔는가"를
        // 영원히 알 수 없다.
        private string DescribeHits(Vector2 worldPoint, out int interactableCount)
        {
            _hits.Clear();
            Physics2D.OverlapPoint(worldPoint, _filter, _hits);

            interactableCount = 0;
            if (_hits.Count == 0)
                return "콜라이더=없음";

            var parts = new List<string>(_hits.Count);
            foreach (var hit in _hits)
            {
                var interactable = hit.GetComponent<IPointerInteractable>() != null;
                if (interactable)
                    interactableCount++;

                var bounds = hit.bounds;
                parts.Add(
                    $"{hit.name}[{bounds.min.x:0.##},{bounds.min.y:0.##}~{bounds.max.x:0.##},{bounds.max.y:0.##}]" +
                    $"{(interactable ? "(누를수있음)" : "(누를수없음)")}");
            }

            return $"콜라이더 {_hits.Count}개={string.Join(", ", parts)}";
        }

        private static string DescribeHovered(ScenePointerInput pointerInput)
        {
            var hovered = ClueDebugReflection.HoveredOf(pointerInput);
            if (hovered == null)
                return "없음";

            return hovered is MonoBehaviour behaviour && behaviour != null
                ? $"{behaviour.name}({behaviour.GetType().Name})"
                : hovered.GetType().Name;
        }

        private string FailureReason()
        {
            if (ClueDebugReflection.HoveredOf(_pendingPointerInput) != null)
                return null;

            // 판정을 따지기 전에 좌표부터 본다 — 화면 밖이면 나머지는 전부
            // 의미가 없다.
            if (_pendingCoordinateSanity != null && _pendingCoordinateSanity.Contains("화면 밖"))
                return "마우스 좌표가 게임 화면 밖이라 방까지 닿지 않음";

            if (!_pendingInputEnabled)
                return "누르기 전부터 씬 조작이 꺼져 있었음(오버레이가 떠 있는 상태)";

            if (_pendingOccluded)
                return "포인터 아래에 UI 요소가 있어 씬까지 내려가지 않음";

            return _pendingInteractableCount == 0
                ? "그 지점에 누를 수 있는 콜라이더가 없음"
                : "누를 수 있는 콜라이더는 있었는데 선택되지 않음";
        }
    }
}
