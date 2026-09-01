using GameName.UI.Overlays;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace GameName.UI.Editor.SceneSetup
{
    // 오버레이 화면(인벤토리·확대)과 그것들을 관리하는 컴포넌트를 씬에 갖춘다.
    //
    // 이미 씬에 있는 것은 새로 만들지 않고 찾아서 쓴다 — 그 UIDocument에 붙어
    // 있던 참조들을 도구가 갈아엎으면 기존 화면이 조용히 망가진다.
    public static class OverlayPanelSceneBuilder
    {
        private const string HostObjectName = "OverlayPanels";
        private const string InventoryObjectName = "InventoryScreen";
        private const string ClueZoomObjectName = "ClueZoomScreen";

        private const string InventoryUxmlPath = "Assets/UI/Inventory/InventoryScreen.uxml";
        private const string ClueZoomUxmlPath = "Assets/UI/ClueZoom/ClueZoomScreen.uxml";

        // 두 오버레이를 같은 순서 값에 둔다. 서로 다른 값을 주면 "겹쳤을 때
        // 누가 위인가"라는 질문에 답한 셈이 되어, 마치 둘이 동시에 떠 있을 수
        // 있는 것처럼 읽힌다. 실제로는 OverlayPanelRouter가 한 번에 하나만
        // 보이도록 보장하므로 겹치는 상황 자체가 없다. 메인 화면(0)보다는 위다.
        private const int OverlaySortingOrder = 1;

        public static OverlayPanelHost Build(SceneSetupReport report)
        {
            var host = FindOrCreateHost(report);
            var panelSettings = ResolvePanelSettings(report);

            var inventory = FindOrCreateDocument(
                InventoryObjectName, InventoryUxmlPath, panelSettings, host.transform, report);
            var clueZoom = FindOrCreateDocument(
                ClueZoomObjectName, ClueZoomUxmlPath, panelSettings, host.transform, report);

            LinkDocument(host, "_inventoryDocument", inventory, report);
            LinkDocument(host, "_clueZoomDocument", clueZoom, report);

            if (SerializedFieldBinder.BindEnum(host, "_inventoryKey", (int)Key.I, report))
                report.Linked($"{HostObjectName}: 인벤토리 키 = I");

            return host;
        }

        private static OverlayPanelHost FindOrCreateHost(SceneSetupReport report)
        {
            var existing = Object.FindFirstObjectByType<OverlayPanelHost>(FindObjectsInactive.Include);
            if (existing != null)
                return existing;

            var hostObject = new GameObject(HostObjectName);
            Undo.RegisterCreatedObjectUndo(hostObject, "기억 방 씬 구성");
            var host = Undo.AddComponent<OverlayPanelHost>(hostObject);

            report.Created($"{HostObjectName} (OverlayPanelHost)");
            return host;
        }

        private static void LinkDocument(
            OverlayPanelHost host, string fieldName, UIDocument document, SceneSetupReport report)
        {
            if (document == null)
                return;

            if (SerializedFieldBinder.BindObject(host, fieldName, document, report))
                report.Linked($"{HostObjectName}.{fieldName} → {document.name}");
        }

        private static UIDocument FindOrCreateDocument(
            string objectName,
            string uxmlPath,
            PanelSettings panelSettings,
            Transform parent,
            SceneSetupReport report)
        {
            var existing = FindDocument(objectName);
            if (existing != null)
                return existing;

            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
            if (visualTree == null)
            {
                report.Problem($"{uxmlPath}를 찾지 못했습니다. {objectName}을 만들지 못했습니다.");
                return null;
            }

            var documentObject = new GameObject(objectName);
            Undo.RegisterCreatedObjectUndo(documentObject, "기억 방 씬 구성");
            documentObject.transform.SetParent(parent, worldPositionStays: false);

            var document = Undo.AddComponent<UIDocument>(documentObject);
            document.panelSettings = panelSettings;
            document.visualTreeAsset = visualTree;
            document.sortingOrder = OverlaySortingOrder;

            report.Created($"{objectName} (UIDocument, {uxmlPath})");
            return document;
        }

        private static UIDocument FindDocument(string objectName)
        {
            foreach (var document in Object.FindObjectsByType<UIDocument>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (document.name == objectName)
                    return document;
            }

            return null;
        }

        // 새 오버레이는 기존 화면과 같은 PanelSettings를 써야 한다 — 서로 다른
        // 패널에 그려지면 sortingOrder로 위아래를 정할 수 없어 겹침이 제멋대로가
        // 된다. 프로젝트에 PanelSettings가 하나뿐이라는 전제로 그것을 찾아 쓴다.
        private static PanelSettings ResolvePanelSettings(SceneSetupReport report)
        {
            var guids = AssetDatabase.FindAssets("t:PanelSettings");
            if (guids.Length > 0)
                return AssetDatabase.LoadAssetAtPath<PanelSettings>(AssetDatabase.GUIDToAssetPath(guids[0]));

            report.Problem("PanelSettings 에셋을 찾지 못했습니다. 새 오버레이가 화면에 보이지 않을 수 있습니다.");
            return null;
        }
    }
}
