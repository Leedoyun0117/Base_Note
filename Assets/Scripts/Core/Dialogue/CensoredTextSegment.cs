using System;
using GameName.Core.Memories;

namespace GameName.Core.Dialogue
{
    // 대사 한 줄을 잘라 낸 조각. 그냥 보이는 부분이거나, 검열이 걸린 부분이다.
    //
    // 검열 조각은 두 가지를 함께 든다. Key는 "무엇이 가려져 있는가"이고 이것이
    // 해금의 단위다 — 같은 사실을 가리키는 조각들은 여러 대사에 흩어져 있어도
    // 한꺼번에 풀린다. Color는 "그것을 열려면 어느 색이 필요한가"이고, 가려진
    // 동안 무엇을 찾아야 할지 보여 주는 힌트로 화면에 나간다.
    //
    // 둘을 갈라 둔 대신, 같은 키가 대사마다 다른 색으로 적히면 어느 색을 모아야
    // 하는지가 대사마다 달라진다. 그 어긋남은 CensorKeyColorConsistencyRule이
    // 저작 시점에 잡는다.
    //
    // Text는 검열 구간이라도 "가려진 뒤의 원문"을 그대로 들고 있다. 파서가
    // 미리 지워 버리지 않는 이유는, 검열이 풀렸을 때 다시 원문을 찾아오는
    // 왕복을 없애기 위함이다 — 풀림 여부 판정(ICensorResolver)과 원문 보관을
    // 분리해 두면 같은 조각을 상태가 바뀔 때마다 다시 해석할 수 있다.
    //
    // 가려진 상태에서 대신 보여 줄 표기("[파란색]" 같은 것)는 여기 두지 않는다.
    // 그것은 화면에 나가는 문구이므로 ICensorMaskFormatter 너머의 저작 데이터에 있다.
    public readonly struct CensoredTextSegment
    {
        public string Text { get; }

        // 검열이 걸리지 않은 조각이면 둘 다 null이다. 걸린 조각이면 둘 다 값이 있다 —
        // 한쪽만 있는 상태는 생성자를 막아 두어 존재하지 않는다.
        public MemoryColor? Color { get; }
        public CensorKey? Key { get; }

        public bool IsCensored => Key.HasValue;

        private CensoredTextSegment(string text, MemoryColor? color, CensorKey? key)
        {
            Text = text ?? throw new ArgumentNullException(nameof(text));
            Color = color;
            Key = key;
        }

        public static CensoredTextSegment Plain(string text) =>
            new CensoredTextSegment(text, null, null);

        public static CensoredTextSegment Censored(string text, MemoryColor color, CensorKey key) =>
            new CensoredTextSegment(text, color, key);
    }
}
