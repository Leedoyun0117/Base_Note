using System;
using System.Linq;
using System.Reflection;
using GameName.Core.Ampoules;
using GameName.Core.Clues;
using GameName.Core.Commissions;
using GameName.Core.Dialogue;
using GameName.Core.FinalCrafting;
using GameName.Core.Inventory;
using GameName.Core.Mentality;
using GameName.Core.MemoryRooms;
using NUnit.Framework;

namespace GameName.Core.Tests.EditMode
{
    // 0-2 검증: "상태를 통째로 초기화/삭제하는 권한"이 각 시스템의 정상 동작
    // 인터페이스에는 없고, 오직 IResettable로만 노출되는지 리플렉션으로
    // 증명한다. IPlayerLocation/IPlayerLocationMover를 나눴던 것과 같은
    // 검증 방식이다 — 런타임 값이 아니라 타입 계약 자체를 검사해, 나중에
    // 누가 실수로 정상 인터페이스에 Reset/Clear를 다시 추가해도 즉시 잡아낸다.
    public class ResettableBoundaryTests
    {
        // 0-2가 지목한 7개 인터페이스 — 전부 "상태를 통째로 비운다"는 뜻의
        // Reset/Clear를 원래 가지고 있었고, 지금은 그 권한이 완전히 빠져 있어야
        // 한다.
        private static readonly Type[] NormalOperationInterfaces =
        {
            typeof(IMentalityGauge),
            typeof(IPlayerInventory),
            typeof(IAmpouleStorage),
            typeof(IMemoryRoomRestorationTracker),
            typeof(IClueAnalysisProgress),
            typeof(IDialogueProgressor),
            typeof(IMemoryRoomClueTracker),
        };

        private static readonly Type[] ResettableImplementations =
        {
            typeof(MentalityGauge),
            typeof(PlayerInventory),
            typeof(AmpouleStorage),
            typeof(MemoryRoomRestorationTracker),
            typeof(ClueAnalysisProgress),
            typeof(AmpouleCraftingQueue),
            typeof(FinalCraftingBoard),
        };

        [Test]
        public void 정상_동작_인터페이스는_Reset이나_Clear를_선언하지_않는다()
        {
            foreach (var type in NormalOperationInterfaces)
            {
                var destructiveMembers = type.GetMethods()
                    .Where(m => m.Name == "Reset" || m.Name == "Clear")
                    .ToArray();

                CollectionAssert.IsEmpty(
                    destructiveMembers, $"{type.Name}이 파괴적 초기화 메서드를 정상 인터페이스에 노출한다.");
            }
        }

        // IAmpouleCraftingQueue.Clear()는 예외다 — 대기열을 제작 처리기에 넘겨
        // 실제로 소비한 뒤 비우는 것은 이 타입 자신의 정상적인 사용 흐름이지,
        // 다른 의뢰의 상태를 침범하는 파괴적 초기화가 아니다(자세한 근거는 그
        // 인터페이스 파일 주석 참고). 그래도 "Reset"이라는 이름의 범용 초기화
        // 권한만큼은 정상 인터페이스에 없어야 한다.
        [Test]
        public void 대기열_인터페이스는_Clear는_허용하되_Reset은_선언하지_않는다()
        {
            var resetMembers = typeof(IAmpouleCraftingQueue).GetMethods().Where(m => m.Name == "Reset");

            CollectionAssert.IsEmpty(resetMembers);
        }

        [Test]
        public void 초기화_가능한_구현체는_IResettable로만_초기화_권한을_노출한다()
        {
            foreach (var type in ResettableImplementations)
            {
                Assert.IsTrue(
                    typeof(IResettable).IsAssignableFrom(type), $"{type.Name}은 IResettable을 구현해야 한다.");

                // Reset()은 IResettable의 명시적 계약으로만 존재해야 한다 — 같은
                // 이름의 메서드가 그 타입의 "정상 동작" 인터페이스에도 우연히
                // 선언되어 있지 않은지, 위 테스트가 이미 인터페이스 목록으로
                // 확인한다. 여기서는 구현체가 실제로 인터페이스를 만족하는지만
                // 추가로 본다.
                var resetMethod = type.GetMethod("Reset", BindingFlags.Public | BindingFlags.Instance);
                Assert.IsNotNull(resetMethod, $"{type.Name}에 공개 Reset() 메서드가 있어야 한다.");
            }
        }

        [Test]
        public void CommissionSession은_IResettable_목록으로만_초기화_대상을_받는다()
        {
            var usesGenericResettableList = typeof(CommissionSession)
                .GetConstructors()
                .Single()
                .GetParameters()
                .Any(p => p.ParameterType == typeof(System.Collections.Generic.IReadOnlyList<IResettable>));

            Assert.IsTrue(usesGenericResettableList, "CommissionSession 생성자가 IReadOnlyList<IResettable>을 받지 않는다.");
        }
    }
}
