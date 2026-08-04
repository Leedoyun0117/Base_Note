using System;
using System.Linq;
using System.Reflection;
using GameName.Core.Clues;
using GameName.Core.MemoryRooms;
using GameName.UI.AnalysisRoom;
using NUnit.Framework;

namespace GameName.UI.Tests.EditMode
{
    // 분석실 화면의 공개 타입들이 진실 데이터(ClueDefinition, MemoryRoomAnswer)를
    // 시그니처에 절대 노출하지 않는지 리플렉션으로 증명한다. 거짓 단서의 진실이
    // 새면 게임의 핵심 장치가 무너지므로, 런타임 값이 아니라 타입 자체를 검사해
    // 나중에 누가 실수로 필드/매개변수를 추가해도 즉시 잡아낸다.
    //
    // RoomNavigationPanelView/MemoryRoomMapNavigationController는 기억 방 화면
    // 쪽 테스트(MemoryRoomScreenTrustBoundaryTests)가 이미 검증하므로 여기서
    // 다시 검사하지 않는다 — 이 화면은 그 타입을 재사용할 뿐 새로 만들지
    // 않았다.
    public class AnalysisRoomScreenTrustBoundaryTests
    {
        private static readonly Type[] ScreenTypes =
        {
            typeof(AnalysisPanelView),
            typeof(AnalysisPanelController),
            typeof(ClueAnalysisRowData),
            typeof(ClueStoragePanelView),
            typeof(ClueStoragePanelController),
            typeof(ClueStorageRowData),
            typeof(AnalysisRoomScreenController),
        };

        private static readonly Type[] ForbiddenTypes = { typeof(ClueDefinition), typeof(MemoryRoomAnswer) };

        [Test]
        public void 분석실_화면_타입은_정답이나_단서_진실_타입을_시그니처에_노출하지_않는다()
        {
            foreach (var screenType in ScreenTypes)
            {
                foreach (var forbidden in ForbiddenTypes)
                {
                    CollectionAssert.DoesNotContain(
                        CollectConstructorParameterTypes(screenType), forbidden,
                        $"{screenType.Name}의 생성자가 {forbidden.Name}을 받는다.");

                    CollectionAssert.DoesNotContain(
                        CollectMethodSignatureTypes(screenType), forbidden,
                        $"{screenType.Name}의 public 메서드(이벤트 포함)가 {forbidden.Name}을 다룬다.");

                    CollectionAssert.DoesNotContain(
                        CollectPropertyTypes(screenType), forbidden,
                        $"{screenType.Name}의 public 속성이 {forbidden.Name}을 노출한다.");
                }
            }
        }

        private static Type[] CollectConstructorParameterTypes(Type type) =>
            type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .SelectMany(c => c.GetParameters())
                .Select(p => p.ParameterType)
                .ToArray();

        private static Type[] CollectMethodSignatureTypes(Type type) =>
            type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .SelectMany(m => m.GetParameters().Select(p => p.ParameterType).Concat(new[] { m.ReturnType }))
                .ToArray();

        private static Type[] CollectPropertyTypes(Type type) =>
            type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => p.PropertyType)
                .ToArray();
    }
}
