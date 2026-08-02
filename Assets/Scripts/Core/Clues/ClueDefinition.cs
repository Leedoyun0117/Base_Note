using System;
using GameName.Core.Emotions;
using GameName.Core.MemoryRooms;

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
    public sealed class ClueDefinition
    {
        public ClueId Id { get; }
        public MemoryRoomId RoomId { get; }
        public EmotionBlend ApparentComposition { get; }
        public EmotionBlend TrueComposition { get; }

        public ClueDefinition(
            ClueId id, MemoryRoomId roomId, EmotionBlend apparentComposition, EmotionBlend trueComposition)
        {
            Id = id;
            RoomId = roomId;
            ApparentComposition = apparentComposition ?? throw new ArgumentNullException(nameof(apparentComposition));
            TrueComposition = trueComposition ?? throw new ArgumentNullException(nameof(trueComposition));
        }

        public bool IsDeceptive => !ApparentComposition.Equals(TrueComposition);

        public ClueInfo ToInfo() => new ClueInfo(Id, RoomId, ApparentComposition);
    }
}
