using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GameName.Core.Clues;
using GameName.UI.MemoryRoom.Space;
using NUnit.Framework;

namespace GameName.UI.Tests.EditMode
{
    // 씬 오브젝트가 ClueDefinition에 닿을 수 없다는 것을 리플렉션으로 증명한다.
    //
    // 공개 표면만 보는 것으로는 부족하다 — 비공개 필드에 ClueDefinition을
    // 몰래 들고 있어도 규칙은 이미 깨진 것이기 때문이다. 그래서 여기서는
    // 비공개 필드까지 포함해 "이 타입이 Core의 어떤 타입과 닿아 있는가"를
    // 전부 훑고, 미리 정한 허용 목록에 없는 Core 타입이 하나라도 있으면
    // 실패시킨다.
    //
    // 허용하는 것은 식별자 하나뿐이다: 단서 오브젝트의 ClueId. "무엇을
    // 가리키는가"만 담을 뿐 게임 정보를 실어 나르지 않으므로, 씬이 이것만 알고
    // 나머지는 컨트롤러를 통하게 하는 것이 이번 설계의 경계다. Core
    // 처리기(ClueCollector 등)도 당연히 금지된다.
    public class MemoryRoomSceneObjectBoundaryTests
    {
        private static readonly Type[] SceneObjectTypes =
        {
            typeof(ClueSceneObject),
            typeof(PlayerCharacter),
            typeof(ScenePointerInput),
            typeof(CameraShake),
        };

        private static readonly HashSet<Type> AllowedCoreTypes = new HashSet<Type>
        {
            typeof(ClueId),
        };

        private const string CoreNamespacePrefix = "GameName.Core";

        [Test]
        public void 씬_오브젝트는_ClueDefinition에_접근할_수_없다()
        {
            foreach (var sceneType in SceneObjectTypes)
            {
                CollectionAssert.DoesNotContain(
                    CollectReferencedTypes(sceneType), typeof(ClueDefinition),
                    $"{sceneType.Name}이 ClueDefinition에 닿아 있다.");
            }
        }

        [Test]
        public void 씬_오브젝트가_아는_Core_타입은_식별자뿐이다()
        {
            foreach (var sceneType in SceneObjectTypes)
            {
                var leaked = CollectReferencedTypes(sceneType)
                    .Where(IsCoreType)
                    .Where(t => !AllowedCoreTypes.Contains(t))
                    .Distinct()
                    .ToArray();

                CollectionAssert.IsEmpty(
                    leaked,
                    $"{sceneType.Name}이 식별자가 아닌 Core 타입({string.Join(", ", leaked.Select(t => t.Name))})을 " +
                    "직접 들고 있다. 씬 오브젝트는 식별자만 알고 나머지는 컨트롤러를 통해야 한다.");
            }
        }

        private static bool IsCoreType(Type type) =>
            type.Namespace != null && type.Namespace.StartsWith(CoreNamespacePrefix, StringComparison.Ordinal);

        // 필드(비공개 포함), 속성, 메서드 매개변수/반환, 이벤트 델리게이트까지
        // 전부 모은다. 제네릭 인자도 펼쳐서 Action<ClueDefinition> 같은 우회를
        // 막는다.
        private static Type[] CollectReferencedTypes(Type type)
        {
            const BindingFlags flags =
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static |
                BindingFlags.DeclaredOnly;

            var fieldTypes = type.GetFields(flags).Select(f => f.FieldType);
            var propertyTypes = type.GetProperties(flags).Select(p => p.PropertyType);
            var methodTypes = type.GetMethods(flags)
                .SelectMany(m => m.GetParameters().Select(p => p.ParameterType).Concat(new[] { m.ReturnType }));

            return fieldTypes
                .Concat(propertyTypes)
                .Concat(methodTypes)
                .SelectMany(Unwrap)
                .ToArray();
        }

        private static IEnumerable<Type> Unwrap(Type type)
        {
            yield return type;

            if (type.HasElementType)
            {
                foreach (var inner in Unwrap(type.GetElementType()))
                    yield return inner;
            }

            if (!type.IsGenericType)
                yield break;

            foreach (var argument in type.GetGenericArguments())
            {
                foreach (var inner in Unwrap(argument))
                    yield return inner;
            }
        }
    }
}
