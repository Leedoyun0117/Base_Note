using System;
using System.Text;

namespace GameName.Core.Dialogue
{
    // 풀린 구간은 원문으로, 아직 풀리지 않은 구간은 대체 표기로 채워 한 줄을
    // 완성한다.
    //
    // 푸는 것은 키가, 대신 그리는 것은 색이 정한다. 같은 키를 쓰는 구간들은 이
    // 줄에 있든 다른 줄에 있든 함께 열리고, 열리기 전까지는 그 키에 적힌 색으로
    // "무엇을 찾아야 하는지"만 보인다.
    //
    // 해금 판정(ICensorResolver)도 대체 표기(ICensorMaskFormatter)도 직접 알지
    // 않고 주입받는다. 전자는 진행 규칙이라 바뀌고, 후자는 화면에 나가는 글이라
    // 번역 대상이다 — 둘 다 이 클래스가 품으면 안 되는 것들이다.
    public sealed class CensorRenderer : ICensorRenderer
    {
        private readonly ICensorResolver _resolver;
        private readonly ICensorMaskFormatter _maskFormatter;

        public CensorRenderer(ICensorResolver resolver, ICensorMaskFormatter maskFormatter)
        {
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            _maskFormatter = maskFormatter ?? throw new ArgumentNullException(nameof(maskFormatter));
        }

        public string Render(CensoredText text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));

            var builder = new StringBuilder();
            foreach (var segment in text.Segments)
            {
                var revealed = !segment.Key.HasValue || _resolver.IsRevealed(segment.Key.Value);
                builder.Append(revealed ? segment.Text : _maskFormatter.FormatMask(segment.Color.Value));
            }

            return builder.ToString();
        }
    }
}
