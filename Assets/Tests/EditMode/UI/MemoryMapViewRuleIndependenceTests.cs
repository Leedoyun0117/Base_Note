using System;
using System.Linq;
using System.Reflection;
using GameName.Core.Ampoules;
using GameName.Core.Clues;
using GameName.Core.Mentality;
using GameName.Core.MemoryRooms;
using GameName.Core.Validation;
using GameName.UI.Shared;
using NUnit.Framework;

namespace GameName.UI.Tests.EditMode
{
    // 지도(MemoryMapView)가 이동/조향 "규칙"에 접근하지 않는다는 것을
    // 리플렉션으로 증명한다 — 표시에 필요한 데이터(MemoryMapNodeData/
    // MemoryMapConnectionData)와 클릭 알림(MemoryGraphNodeId)만 시그니처에
    // 있어야 한다. 규칙 계산 자체(MemoryMapDataBuilder, 각 화면 컨트롤러)는
    // 지도 바깥에 있으므로 여기서 검사하지 않는다 — 오직 재사용 컴포넌트
    // 자체만 검증한다.
    public class MemoryMapViewRuleIndependenceTests
    {
        private static readonly Type[] MapTypes =
        {
            typeof(MemoryMapView),
            typeof(MemoryMapNodeData),
            typeof(MemoryMapConnectionData),
            typeof(MemoryMapLayout),
        };

        private static readonly Type[] ForbiddenRuleTypes =
        {
            typeof(MemoryRoomMovementProcessor),
            typeof(IMemoryRoomRestorationTracker),
            typeof(IMemoryRoomGraph),
            typeof(IMentalityGauge),
            typeof(AmpouleCraftingProcessor),
            typeof(IScentCompositionValidator),
            typeof(ClueAnalyzer),
        };

        [Test]
        public void 지도_타입은_이동이나_조향_규칙_타입을_시그니처에_노출하지_않는다()
        {
            foreach (var mapType in MapTypes)
            {
                foreach (var forbidden in ForbiddenRuleTypes)
                {
                    CollectionAssert.DoesNotContain(
                        CollectConstructorParameterTypes(mapType), forbidden,
                        $"{mapType.Name}의 생성자가 {forbidden.Name}을 받는다.");

                    CollectionAssert.DoesNotContain(
                        CollectMethodSignatureTypes(mapType), forbidden,
                        $"{mapType.Name}의 public 메서드(이벤트 포함)가 {forbidden.Name}을 다룬다.");

                    CollectionAssert.DoesNotContain(
                        CollectPropertyTypes(mapType), forbidden,
                        $"{mapType.Name}의 public 속성이 {forbidden.Name}을 노출한다.");
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
