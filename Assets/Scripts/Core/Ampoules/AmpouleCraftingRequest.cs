using System;
using GameName.Core.Emotions;
using GameName.Core.MemoryRooms;

namespace GameName.Core.Ampoules
{
    // 앰플 하나를 만들기 위한 요청 하나. 목표 방과 향만 담는다.
    //
    // 요구 총량은 여기 담지 않는다 — 호출부가 임의의 총량을 주장할 수 있게
    // 두면 배합 검증이 무력화되기 때문이다. 대신 조향 처리기가
    // IMemoryRoomPublicInfoRepository로 목표 방의 진짜 요구 총량을 직접
    // 조회한다.
    public sealed class AmpouleCraftingRequest
    {
        public MemoryRoomId TargetRoomId { get; }
        public Scent Scent { get; }

        public AmpouleCraftingRequest(MemoryRoomId targetRoomId, Scent scent)
        {
            TargetRoomId = targetRoomId;
            Scent = scent ?? throw new ArgumentNullException(nameof(scent));
        }
    }
}
