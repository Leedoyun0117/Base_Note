using System;
using GameName.Core.Emotions;
using GameName.Core.MemoryRooms;

namespace GameName.Core.Ampoules
{
    // 앰플 하나의 식별자·목표 방·배합을 함께 담는 불변 값 타입.
    // 이벤트(AmpouleCraftedEvent) 소비자가 "무엇을, 어디에 쓰려고 만들었는지"를
    // 함께 알아야 하기 때문이다 — 향(Scent)만 넘기면 목표 방 정보가 유실된다.
    // Id도 함께 담는다 — 기록지가 이후의 시향 결과(ScentJudgedEvent)를 어느
    // 제작 기록과 연결할지 이 값으로 판단하기 때문이다. 앰플은 숨길 정보가
    // 없는 타입이므로(Ampoule 참고) Id를 공개해도 안전하다.
    public sealed class AmpouleRecipe
    {
        public AmpouleId Id { get; }
        public MemoryRoomId TargetRoomId { get; }
        public Scent Scent { get; }

        public AmpouleRecipe(AmpouleId id, MemoryRoomId targetRoomId, Scent scent)
        {
            Id = id;
            TargetRoomId = targetRoomId;
            Scent = scent ?? throw new ArgumentNullException(nameof(scent));
        }
    }
}
