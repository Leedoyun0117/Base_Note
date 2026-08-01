using System;
using GameName.Core.Emotions;

namespace GameName.Core.MemoryRooms
{
    // 기억 방에 설정된 정답 향.
    // 필요 세기 총량은 정답 향의 배분 총합에서 그대로 파생시킨다(별도로 들고 다니면
    // 정답 배분과 총량이 서로 어긋나는 이중 진실 원천 문제가 생기기 때문).
    //
    // struct였을 때는 참조 타입 필드(Scent)를 가지면서 struct 기본값(default,
    // 배열 할당 등)이 생성자 검증을 우회해 CorrectScent가 null인 채로 존재할 수
    // 있었다 — Scent를 class로 바꿨던 것과 같은 문제가 한 단계 위로 전이된 것이다.
    // 이 타입도 class로 바꿔 같은 방식으로 해결한다.
    public sealed class MemoryRoomAnswer
    {
        public MemoryRoomId RoomId { get; }
        public Scent CorrectScent { get; }

        public MemoryRoomAnswer(MemoryRoomId roomId, Scent correctScent)
        {
            RoomId = roomId;
            CorrectScent = correctScent ?? throw new ArgumentNullException(nameof(correctScent));
        }

        public int RequiredSupportingIntensityTotal => CorrectScent.SupportingBlend.Total;

        // 조향실 지도 등 플레이어에게 노출 가능한 화면으로 내보낼 공개 정보.
        // 정답 향(CorrectScent)은 담지 않으므로 이 결과로부터 정답을 역추적할 수 없다.
        public MemoryRoomPublicInfo ToPublicInfo() =>
            new MemoryRoomPublicInfo(RoomId, RequiredSupportingIntensityTotal);
    }
}
