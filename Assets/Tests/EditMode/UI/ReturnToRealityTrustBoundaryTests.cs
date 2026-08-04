using System;
using System.Linq;
using System.Reflection;
using GameName.Core.Clues;
using GameName.Core.Commissions;
using GameName.Core.MemoryRooms;
using GameName.UI.Flow;
using NUnit.Framework;

namespace GameName.UI.Tests.EditMode
{
    // 이탈 확인 화면 관련 공개 타입들이 진실 데이터(ClueDefinition,
    // MemoryRoomAnswer)를 시그니처에 절대 노출하지 않는지 리플렉션으로
    // 증명한다 — "지금 나가면 무엇을 잃는지"는 개수로만 보여줘야 하며, 어떤
    // 단서인지/어떤 방의 정답이 무엇인지는 이 경계를 절대 넘어오면 안 된다.
    public class ReturnToRealityTrustBoundaryTests
    {
        private static readonly Type[] ScreenTypes =
        {
            typeof(ReturnToRealityPanelView),
            typeof(ReturnToRealityPanelController),
            typeof(MemoryExitSummary),
            typeof(MemoryExitSummaryCalculator),
        };

        private static readonly Type[] ForbiddenTypes = { typeof(ClueDefinition), typeof(MemoryRoomAnswer) };

        [Test]
        public void 이탈_확인_화면_타입은_정답이나_단서_진실_타입을_시그니처에_노출하지_않는다()
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
