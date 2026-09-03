using GameName.UI.MemoryRoom;
using GameName.UI.MemoryRoom.Space;
using System.Collections.Generic;
using GameName.UI.Overlays;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameName.UI.Editor.SceneSetup
{
    // 2D 기억 방 공간을 씬에 갖추고 카메라가 그 방을 담도록 맞춘다.
    //
    // 방의 벽·바닥·플레이어·단서는 실행 중에 MemoryRoomSpaceView가 만든다 —
    // 이 도구가 만드는 것은 그 뷰가 붙을 빈 오브젝트와 마우스 입력뿐이다.
    // 그래야 방 치수를 바꿨을 때 씬을 다시 손볼 필요가 없다.
    public static class MemoryRoomSpaceSceneBuilder
    {
        private const string SpaceObjectName = "Space";

        public static MemoryRoomSpaceView Build(
            MemoryRoomBootstrap memoryRoomScreen,
            MemoryRoomLayoutAsset layoutAsset,
            OverlayPanelHost overlayHost,
            SceneSetupReport report)
        {
            var spaceView = FindOrCreateSpace(memoryRoomScreen, report);
            var pointerInput = FindOrAddPointerInput(spaceView, report);

            LinkUIDocuments(pointerInput, memoryRoomScreen, overlayHost, report);

            if (SerializedFieldBinder.BindObject(spaceView, "_spaceRoot", spaceView.transform, report))
                report.Linked($"{SpaceObjectName}.Space Root → 자기 Transform");

            if (SerializedFieldBinder.BindObject(spaceView, "_pointerInput", pointerInput, report))
                report.Linked($"{SpaceObjectName}.Pointer Input → ScenePointerInput");

            SetUpCamera(pointerInput, memoryRoomScreen, layoutAsset, report);

            return spaceView;
        }

        // 반드시 기억 방 화면의 자식이어야 한다 — SceneScreenSwitcher가 그
        // 오브젝트를 껐다 켜므로, 자식으로 두어야 조향실·분석실에 있는 동안
        // 방이 함께 꺼진다. 밖에 두면 다른 화면 뒤에 방이 계속 남는다.
        private static MemoryRoomSpaceView FindOrCreateSpace(
            MemoryRoomBootstrap memoryRoomScreen, SceneSetupReport report)
        {
            var existing = memoryRoomScreen.GetComponentInChildren<MemoryRoomSpaceView>(includeInactive: true);
            if (existing != null)
                return existing;

            var spaceObject = new GameObject(SpaceObjectName);
            Undo.RegisterCreatedObjectUndo(spaceObject, "기억 방 씬 구성");
            spaceObject.transform.SetParent(memoryRoomScreen.transform, worldPositionStays: false);
            spaceObject.transform.localPosition = Vector3.zero;

            var view = Undo.AddComponent<MemoryRoomSpaceView>(spaceObject);
            report.Created($"{memoryRoomScreen.name}/{SpaceObjectName} (MemoryRoomSpaceView)");
            return view;
        }

        private static ScenePointerInput FindOrAddPointerInput(
            MemoryRoomSpaceView spaceView, SceneSetupReport report)
        {
            var existing = spaceView.GetComponent<ScenePointerInput>();
            if (existing != null)
                return existing;

            var pointerInput = Undo.AddComponent<ScenePointerInput>(spaceView.gameObject);
            report.Created($"{SpaceObjectName}에 ScenePointerInput 추가");
            return pointerInput;
        }

        // 신뢰가 깎일 때 방 안 콘텐츠를 짧게 흔든다. 카메라에 붙여야 UI 레이어
        // (마스크·HUD·대화 패널)는 화면 좌표라 흔들리지 않고 씬만 떨린다.
        private static CameraShake FindOrAddCameraShake(Camera camera, SceneSetupReport report)
        {
            var existing = camera.GetComponent<CameraShake>();
            if (existing != null)
                return existing;

            var shake = Undo.AddComponent<CameraShake>(camera.gameObject);
            report.Created("Main Camera에 CameraShake 추가");
            return shake;
        }

        // 포인터가 UI 위에 있는지 판정하려면 어떤 문서가 화면을 덮을 수 있는지
        // 알아야 한다. 연결을 빠뜨리면 UI를 눌렀는데 뒤의 단서까지 함께 눌린다.
        private static void LinkUIDocuments(
            ScenePointerInput pointerInput,
            MemoryRoomBootstrap memoryRoomScreen,
            OverlayPanelHost overlayHost,
            SceneSetupReport report)
        {
            var documents = new List<Object>();

            var hudDocument = memoryRoomScreen.GetComponent<UIDocument>();
            if (hudDocument != null)
                documents.Add(hudDocument);

            if (overlayHost != null)
            {
                foreach (OverlayPanel panel in System.Enum.GetValues(typeof(OverlayPanel)))
                {
                    var document = overlayHost.DocumentOf(panel);
                    if (document != null)
                        documents.Add(document);
                }
            }

            if (SerializedFieldBinder.BindObjectArray(pointerInput, "_uiDocuments", documents, report))
                report.Linked($"ScenePointerInput.UI Documents → {documents.Count}개");
        }

        // 카메라 크기와 위치를 방 치수에서 계산한다. 도구가 숫자를 스스로 정하면
        // 방 높이를 바꿨을 때 화면만 따로 어긋나므로, 여백까지 전부 에셋의
        // 값(CameraVerticalMargin)을 따른다.
        private static void SetUpCamera(
            ScenePointerInput pointerInput,
            MemoryRoomBootstrap memoryRoomScreen,
            MemoryRoomLayoutAsset layoutAsset,
            SceneSetupReport report)
        {
            var camera = Camera.main;
            if (camera == null)
            {
                report.Problem("Main Camera를 찾지 못했습니다. 카메라 설정과 마우스 입력 연결을 건너뜁니다.");
                return;
            }

            var layout = layoutAsset.ToLayout();

            // 방 내부는 y가 바닥(0)부터 RoomHeight까지다. 그 한가운데를 보게 하고,
            // 직교 카메라의 크기는 세로 절반이므로 방 높이의 절반에 여백을 더한다.
            var centerY = RoomGeometry.FloorTopY(layout) + layout.RoomHeight / 2f;
            var orthographicSize = layout.RoomHeight / 2f + layout.CameraVerticalMargin;

            var position = new Vector3(0f, centerY, camera.transform.position.z);
            var alreadyFramed =
                camera.orthographic &&
                Mathf.Approximately(camera.orthographicSize, orthographicSize) &&
                camera.transform.position == position;

            // 이미 맞춰져 있으면 손대지 않는다 — 도구를 다시 돌렸을 때 "이번에
            // 바뀐 것"만 보고되어야 두 번째 실행이 안전했는지 확인할 수 있다.
            if (!alreadyFramed)
            {
                Undo.RecordObject(camera.transform, "기억 방 씬 구성");
                Undo.RecordObject(camera, "기억 방 씬 구성");

                camera.orthographic = true;
                camera.orthographicSize = orthographicSize;
                camera.transform.position = position;

                report.Linked($"Main Camera: 직교, 크기 {orthographicSize:0.##}, 위치 y {centerY:0.##}");
            }

            if (SerializedFieldBinder.BindObject(pointerInput, "_camera", camera, report))
                report.Linked("ScenePointerInput.Camera → Main Camera");

            var cameraShake = FindOrAddCameraShake(camera, report);
            if (SerializedFieldBinder.BindObject(memoryRoomScreen, "_cameraShake", cameraShake, report))
                report.Linked($"{memoryRoomScreen.name}.Camera Shake → Main Camera의 CameraShake");
        }
    }
}
