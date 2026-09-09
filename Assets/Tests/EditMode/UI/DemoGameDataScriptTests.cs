using System.Linq;
using GameName.Core.Authoring;
using GameName.UI.Session;
using NUnit.Framework;

namespace GameName.UI.Tests.EditMode
{
    // 데모 저작 데이터(대사·단서)가 검증기를 그대로 통과하는지 붙든다.
    //
    // 검증기가 잡는 문제들은 전부 "플레이하다 어느 순간 막힌다" 부류라, 대사가
    // 채워진 뒤에도 규칙을 계속 지키는지 자동으로 확인해 둔다 — 에디터 메뉴는
    // RunDefinitionAsset만 검사하므로 DemoGameData는 그 그물에 걸리지 않는다.
    public class DemoGameDataScriptTests
    {
        // 검증기 조립은 팩토리 한 곳을 그대로 쓴다(에디터/테스트가 갈리지 않게).
        // 세 방, 겹침 경고 기준은 실제 포스터 최소 간격에 가까운 값.
        private static DialogueScriptValidator Validator() =>
            DialogueScriptValidatorFactory.Create(expectedRoomCount: 3, minimumClueSeparation: 0.14f);

        [Test]
        public void 데모_저작_데이터는_검증_오류가_없다()
        {
            var run = DemoGameData.CreateWorldData().Run;

            var errors = Validator().Validate(run)
                .Where(i => i.Severity == ScriptIssueSeverity.Error)
                .Select(i => i.Description)
                .ToArray();

            Assert.IsEmpty(errors, "검증 오류:\n- " + string.Join("\n- ", errors));
        }

        [Test]
        public void 데모_저작_데이터는_겹침_경고도_없다()
        {
            var run = DemoGameData.CreateWorldData().Run;

            var warnings = Validator().Validate(run)
                .Where(i => i.Severity == ScriptIssueSeverity.Warning)
                .Select(i => i.Description)
                .ToArray();

            Assert.IsEmpty(warnings, "검증 경고:\n- " + string.Join("\n- ", warnings));
        }
    }
}
