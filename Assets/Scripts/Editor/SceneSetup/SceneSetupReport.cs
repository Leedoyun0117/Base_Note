using System.Collections.Generic;
using System.Text;

namespace GameName.UI.Editor.SceneSetup
{
    // 씬 구성 도구가 무엇을 만들고 무엇을 이었는지 모아 두는 자리.
    //
    // 도구가 곧바로 Debug.Log를 뿌리지 않고 이렇게 모으는 이유: 이 도구는 여러
    // 번 다시 돌려도 되는 것이 목적이라(이미 있는 것은 만들지 않고 잇기만 한다),
    // "이번에 실제로 바뀐 것이 무엇인가"를 한눈에 보여줘야 두 번째 실행이
    // 안전했는지 확인할 수 있기 때문이다.
    public sealed class SceneSetupReport
    {
        private readonly List<string> _created = new List<string>();
        private readonly List<string> _linked = new List<string>();
        private readonly List<string> _problems = new List<string>();

        public bool HasProblems => _problems.Count > 0;

        // 새로 만든 것(오브젝트, 에셋).
        public void Created(string what) => _created.Add(what);

        // 이미 있던 것에 참조를 이어 준 것.
        public void Linked(string what) => _linked.Add(what);

        // 도구가 대신 해결할 수 없어 사람이 봐야 하는 것.
        public void Problem(string what) => _problems.Add(what);

        public string Summarize()
        {
            var builder = new StringBuilder();

            Append(builder, "만든 것", _created, "새로 만든 것이 없습니다(이미 구성되어 있었습니다).");
            Append(builder, "이어 준 것", _linked, "새로 이어 준 참조가 없습니다.");

            if (_problems.Count > 0)
                Append(builder, "확인이 필요한 것", _problems, string.Empty);

            return builder.ToString().TrimEnd();
        }

        private static void Append(StringBuilder builder, string title, IReadOnlyList<string> lines, string emptyText)
        {
            builder.Append("[").Append(title).Append("]\n");

            if (lines.Count == 0)
            {
                if (!string.IsNullOrEmpty(emptyText))
                    builder.Append("  ").Append(emptyText).Append('\n');
            }
            else
            {
                foreach (var line in lines)
                    builder.Append("  · ").Append(line).Append('\n');
            }

            builder.Append('\n');
        }
    }
}
