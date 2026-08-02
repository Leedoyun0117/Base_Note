using System;
using System.Linq;
using System.Reflection;
using GameName.Core.Clues;
using GameName.Core.MemoryRooms;
using GameName.UI.MemoryRoom;
using NUnit.Framework;

namespace GameName.UI.Tests.EditMode
{
    // 기억 방 화면의 공개 타입들이 진실 데이터(ClueDefinition, MemoryRoomAnswer)를
    // 시그니처에 절대 노출하지 않는지 리플렉션으로 증명한다. 런타임 값이 아니라
    // 타입 자체를 검사하므로, 나중에 누가 실수로 필드/매개변수를 추가해도 이
    // 테스트가 즉시 잡아낸다 — 이 프로젝트에서 이미 여러 번 어긴 규칙이다.
    public class MemoryRoomScreenTrustBoundaryTests
    {
        private static readonly Type[] ScreenTypes =
        {
            typeof(RoomNavigationPanelView),
            typeof(RoomNavigationPanelController),
            typeof(ClueCollectionPanelView),
            typeof(ClueCollectionPanelController),
            typeof(MemoryRoomInventoryPanelView),
            typeof(MemoryRoomInventoryPanelController),
            typeof(ScentTestingPanelView),
            typeof(ScentTestingPanelController),
            typeof(MemoryRoomScreenController),
            typeof(NeighborRowData),
            typeof(AmpouleTestRowData),
        };

        private static readonly Type[] ForbiddenTypes = { typeof(ClueDefinition), typeof(MemoryRoomAnswer) };

        [Test]
        public void 기억_방_화면_타입은_정답이나_단서_진실_타입을_시그니처에_노출하지_않는다()
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

        // C# 이벤트는 add_X/remove_X 공개 메서드로 컴파일되므로 GetMethods만으로
        // 이벤트 델리게이트의 매개변수 타입(Action<T>의 T)까지 함께 잡힌다.
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
