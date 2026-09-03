using System.Collections.Generic;
using System.Text;
using GameName.Core.Memories;

namespace GameName.Core.Dialogue
{
    // "우리가 [[B:beach-house:해변의 작은 집]]에서 보냈던 시절이 그리워." 형태의
    // 원문을 조각으로 자르는 파서. 토큰은 색, 키, 가려진 말 순서다.
    //
    // 키를 원문에 적게 하는 것은 저작자에게 부담이지만 대안이 없다. 같은 사실이
    // 여러 대사에 흩어져 있을 때 그것들이 한 사실이라는 것을 문장만 보고는 알
    // 수 없기 때문이다 — 표현이 조금씩 다를 수도 있고("그 집", "해변의 집"),
    // 같은 표현이 서로 다른 사실을 가리킬 수도 있다.
    //
    // 이 클래스가 아는 것은 표기 문법뿐이다. 무엇이 풀렸는지도, 가려진 자리에
    // 무엇을 대신 그릴지도 모른다 — 그래서 해금 상태가 바뀌어도 다시 파싱할
    // 이유가 없고, 파싱 결과를 그대로 재사용할 수 있다.
    //
    // 중첩은 지원하지 않는다. 토큰 안에서 다시 토큰을 여는 표기는 "가려진 것
    // 안의 가려진 것"이 무슨 뜻인지부터 정해야 하는데 그런 규칙이 없기 때문이다.
    // 열림 표시 다음 첫 닫힘 표시에서 무조건 끊으므로 결과는 항상 평평한 조각
    // 목록이다.
    //
    // 문법에 맞지 않는 입력은 예외가 아니라 그냥 글자로 흘려보낸다. 저작 중인
    // 원문에는 오타가 있게 마련이고, 그때 게임이 멈추는 것보다 화면에
    // "[[B:해변의 작은 집]]"이 그대로 보이는 편이 기획자에게 훨씬 빨리 발견된다.
    public sealed class CensoredTextParser : ICensoredTextParser
    {
        private const string TokenOpen = "[[";
        private const string TokenClose = "]]";
        private const char FieldSeparator = ':';

        public CensoredText Parse(string authoredText)
        {
            if (string.IsNullOrEmpty(authoredText))
                return CensoredText.Empty;

            var segments = new List<CensoredTextSegment>();
            var plain = new StringBuilder();
            var cursor = 0;

            while (cursor < authoredText.Length)
            {
                var open = authoredText.IndexOf(TokenOpen, cursor, System.StringComparison.Ordinal);
                if (open < 0)
                    break;

                var bodyStart = open + TokenOpen.Length;
                var close = authoredText.IndexOf(TokenClose, bodyStart, System.StringComparison.Ordinal);
                if (close < 0)
                    break;

                var body = authoredText.Substring(bodyStart, close - bodyStart);
                if (!TryReadToken(body, out var color, out var key, out var text))
                {
                    // 토큰으로 보였지만 아니었다. 열림 표시까지만 글자로 넘기고
                    // 그 뒤에서 다시 찾는다 — 깨진 토큰 하나가 뒤따르는 멀쩡한
                    // 토큰까지 통째로 삼키지 않게 하기 위해서다.
                    plain.Append(authoredText, cursor, bodyStart - cursor);
                    cursor = bodyStart;
                    continue;
                }

                plain.Append(authoredText, cursor, open - cursor);
                FlushPlain(segments, plain);
                segments.Add(CensoredTextSegment.Censored(text, color, key));
                cursor = close + TokenClose.Length;
            }

            plain.Append(authoredText, cursor, authoredText.Length - cursor);
            FlushPlain(segments, plain);

            return new CensoredText(segments);
        }

        private static void FlushPlain(ICollection<CensoredTextSegment> segments, StringBuilder plain)
        {
            if (plain.Length == 0)
                return;

            segments.Add(CensoredTextSegment.Plain(plain.ToString()));
            plain.Clear();
        }

        private static bool TryReadToken(string body, out MemoryColor color, out CensorKey key, out string text)
        {
            color = default;
            key = default;
            text = null;

            var afterColor = body.IndexOf(FieldSeparator);
            if (afterColor < 0)
                return false;

            var afterKey = body.IndexOf(FieldSeparator, afterColor + 1);
            if (afterKey < 0)
                return false;

            if (!TryReadColorCode(body.Substring(0, afterColor), out color))
                return false;

            var keyText = body.Substring(afterColor + 1, afterKey - afterColor - 1);
            if (string.IsNullOrWhiteSpace(keyText))
                return false;

            // 가려진 말에는 콜론이 들어갈 수 있으므로 남은 전부를 본문으로 삼는다.
            // 앞의 두 칸만 구분자로 쓰는 이유가 이것이다.
            text = body.Substring(afterKey + 1);
            if (text.Length == 0)
                return false;

            key = new CensorKey(keyText);
            return true;
        }

        // 색을 한 글자로 줄여 쓰는 것은 표기 문법이지 화면에 나가는 글이 아니다.
        // 원문 안에서 눈에 덜 걸리적거리게 하려는 것이라 여기 두어도 무방하다.
        private static bool TryReadColorCode(string code, out MemoryColor color)
        {
            switch (code)
            {
                case "R":
                    color = MemoryColor.Red;
                    return true;
                case "G":
                    color = MemoryColor.Green;
                    return true;
                case "B":
                    color = MemoryColor.Blue;
                    return true;
                default:
                    color = default;
                    return false;
            }
        }
    }
}
