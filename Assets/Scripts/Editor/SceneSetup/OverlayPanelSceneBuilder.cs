using GameName.UI.Overlays;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace GameName.UI.Editor.SceneSetup
{
    // 오버레이 화면(기록지·인벤토리·확대) 셋과 그것들을 관리하는 컴포넌트를
    // 씬에 갖춘다.
    //
    // 기록지는 이미 씬에 있으므로 새로 만들지 않고 찾아서 쓴다 — 그 UIDocument에
    // 붙어 있던 참조들을 도구가 갈아엎으면 기존 화면이 조용히 망가진다.
    // 없는 것(인벤토리·확대)만 만들고, 있는 것은 잇기만 한다.
    public static class OverlayPanelSceneBuilder
    {
        private const string HostObjectName = "OverlayPanels";
        private const string JournalObjectName = "JournalScreen";
        private const string InventoryObjectName = "InventoryScreen";
        private const string ClueZoomObjectName = "ClueZoomScreen";

        private const string InventoryUxmlPath = "Assets/UI/Inventory/InventoryScreen.uxml";
        private const string ClueZoomUxmlPath = "Assets/UI/ClueZoom/ClueZoomScreen.uxml";

        // 세 오버레이를 같은 순서 값에 둔다. 서로 다른 값을 주면 "겹쳤을 때
        // 누가 위인가"라는 질문에 답한 셈이 되어, 마치 둘이 동시에 떠 있을 수
        // 있는 것처럼 읽힌다. 실제로는 OverlayPanelRouter가 한 번에 하나만
        // 보이도록 보장하므로 겹치는 상황 자체가 없다. 메인 화면(0)보다는 위,
        // 흐름 오버레이(대화·이탈 확인, 2)보다는 아래다.
        private const int OverlaySortingOrder = 1;

        public static OverlayPanelHost Build(SceneSetupReport report)
        {
            var host = FindOrCreateHost(report);

            var journal = FindDocument(JournalObjectName);
            if (journal == null)
                report.Problem($"{JournalObjectName} UIDocument를 찾지 못했습니다. 기록지 연결은 건너뜁니다.");

            var panelSettings = ResolvePanelSettings(journal, report);

            var inventory = FindOrCreateDocument(
                InventoryObjectName, InventoryUxmlPath, panelSettings, host.transform, report);
            var clueZoom = FindOrCreateDocument(
                ClueZoomObjectName, ClueZoomUxmlPath, panelSettings, host.transform, report);

            LinkDocument(host, "_journalDocument", journal, report);
            LinkDocument(host, "_inventoryDocument", inventory, report);
            LinkDocument(host, "_clueZoomDocument", clueZoom, report);

            // 기록지는 Tab, 인벤토리는 I. 지금 둘이 갈려 있는 것은 Tab이 이미
            // 기록지에 쓰이고 있어 충돌을 피하려는 임시 조치이며, 나중에 둘을
            // 탭으로 합칠 때 이 값만 바뀐다.
            if (SerializedFieldBinder.BindEnum(host, "_journalKey", (int)Key.Tab, report))
                report.Linked($"{HostObjectName}: 기록지 키 = Tab");
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
        // 된다. 그래서 기록지가 쓰는 것을 그대로 따라가고, 그마저 없을 때만
        // 프로젝트에서 아무거나 하나 찾는다.
        private static PanelSettings ResolvePanelSettings(UIDocument journal, SceneSetupReport report)
        {
            if (journal != null && journal.panelSettings != null)
                return journal.panelSettings;

            var guids = AssetDatabase.FindAssets("t:PanelSettings");
            if (guids.Length > 0)
                return AssetDatabase.LoadAssetAtPath<PanelSettings>(AssetDatabase.GUIDToAssetPath(guids[0]));

            report.Problem("PanelSettings 에셋을 찾지 못했습니다. 새 오버레이가 화면에 보이지 않을 수 있습니다.");
            return null;
        }
    }
}
