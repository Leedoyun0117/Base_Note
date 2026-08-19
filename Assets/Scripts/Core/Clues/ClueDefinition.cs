using System;
using GameName.Core.Emotions;

namespace GameName.Core.Clues
{
    // 기억 방에 배치되는 단서의 전체 데이터 — 겉보기 구성과 실제 구성을 모두
    // 담는다(정답 데이터와 같은 층위의 "진실" 데이터).
    //
    // 이 타입에 직접 접근해도 되는 쪽은 방 데이터 검증기와, 나중에 만들
    // 거짓말 탐지기, 그리고 분석기뿐이다. 그 외의 모든 소비자(UI, 인벤토리,
    // 단서 습득)는 ToInfo()로 변환한 ClueInfo만 받는다 — MemoryRoomAnswer가
    // MemoryRoomPublicInfo를 내보내는 것과 같은 패턴이다. IInventoryItem을
    // 구현하지 않는 이유도 같다: 인벤토리에 이 타입이 그대로 담기면 인벤토리를
    // 들여다보는 모든 코드가 진실에 접근할 길이 열려 버린다.
    //
    // RoomId는 더 이상 여기 없다. 단서를 아무 방에나 버릴 수 있게 되면서
    // "이 단서가 어느 방에 있는가"는 기획자가 적어 넣는 고정 데이터가 아니라
    // 플레이 중에 바뀌는 상태가 되었기 때문이다. 그 상태는 습득 여부와 똑같은
    // 층위이므로 습득 여부를 이미 들고 있던 IMemoryRoomClueTracker가 함께
    // 관리한다. 최초로 어느 방에 있었는지는 CluePlacement가 표현한다.
    //
    // 다만 "방 안 어디쯤에 놓여 있는가"(AuthoredPosition)는 여기 남는다 —
    // 어느 방인지와 달리 이 값은 플레이 중에 바뀌지 않는 저작 사실이고,
    // 창가의 사진처럼 자리 자체가 기억을 읽는 실마리가 될 수 있는 콘텐츠이기
    // 때문이다. 플레이어가 다른 자리에 버렸을 때의 위치는 이 값을 덮지 않고
    // 표시 계층이 따로 기억한다.
    public sealed class ClueDefinition
    {
        public ClueId Id { get; }
        public ClueKind Kind { get; }

        // 기획이 정한 방 안 가로 자리. 세로는 여기 없다 — 포스터는 벽, 바닥
        // 물건은 바닥이라는 것이 종류만으로 이미 정해지므로 기획이 고를 값이
        // 아니기 때문이다.
        public CluePositionRatio AuthoredPosition { get; }

        public EmotionBlend ApparentComposition { get; }
        public EmotionBlend TrueComposition { get; }

        public ClueDefinition(
            ClueId id,
            ClueKind kind,
            CluePositionRatio authoredPosition,
            EmotionBlend apparentComposition,
            EmotionBlend trueComposition)
        {
            Id = id;
            Kind = kind;
            AuthoredPosition = authoredPosition;
            ApparentComposition = apparentComposition ?? throw new ArgumentNullException(nameof(apparentComposition));
            TrueComposition = trueComposition ?? throw new ArgumentNullException(nameof(trueComposition));
        }

        public bool IsDeceptive => !ApparentComposition.Equals(TrueComposition);

        public ClueInfo ToInfo() => new ClueInfo(Id, Kind, AuthoredPosition, ApparentComposition);
    }
}
