using System;

namespace GameName.Core.Dialogue
{
    // 대사 한 줄을 식별하는 값. 대사 본문이 아니라 키만 Core가 든다 —
    // 문장 자체는 저작 데이터에 있고 IDialogueScriptReader를 통해서만 들어온다.
    public readonly struct DialogueLineId : IEquatable<DialogueLineId>
    {
        public string Value { get; }

        public DialogueLineId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("대사 식별자는 비어 있을 수 없다.", nameof(value));

            Value = value;
        }

        public bool Equals(DialogueLineId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is DialogueLineId other && Equals(other);
        public override int GetHashCode() => Value?.GetHashCode() ?? 0;
        public override string ToString() => Value;

        public static bool operator ==(DialogueLineId left, DialogueLineId right) => left.Equals(right);
        public static bool operator !=(DialogueLineId left, DialogueLineId right) => !left.Equals(right);
    }
}
