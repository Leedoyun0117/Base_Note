using GameName.Core.Dialogue;

namespace GameName.UI.Authoring
{
    // 인스펙터에 적힌 식별자 문자열을 Core의 식별자 타입으로 옮기는 자리.
    //
    // 인스펙터에서 "비어 있음"은 빈 문자열이지만 Core에서는 null이다 — 식별자
    // 타입들이 빈 값을 아예 거부하기 때문이다(그래야 "id가 있는데 비어 있는"
    // 상태가 존재하지 않는다). 그 변환을 에셋마다 따로 적으면 한 군데서만
    // 빠뜨려도 저작 실수가 예외로 바뀐다.
    internal static class AuthoredIds
    {
        public static DialogueLineId? OptionalLine(string value) =>
            string.IsNullOrWhiteSpace(value) ? (DialogueLineId?)null : new DialogueLineId(value);
    }
}
