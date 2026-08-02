using System;
using System.Collections.Generic;

namespace GameName.Core.MemoryRooms
{
    // IMemoryRoomAnswerRepository / IMemoryRoomPublicInfoRepository 기본 구현.
    // 정답 전체와 공개 정보를 같은 저장소에서 내어주되, 어느 인터페이스로
    // 주입받았는지에 따라 소비자가 받을 수 있는 정보의 범위가 갈린다 —
    // MemoryRoomAnswer.ToPublicInfo()가 이미 보장하는 단방향 변환을 그대로
    // 재사용한다.
    public sealed class MemoryRoomAnswerRepository : IMemoryRoomAnswerRepository, IMemoryRoomPublicInfoRepository
    {
        private readonly Dictionary<MemoryRoomId, MemoryRoomAnswer> _answersByRoomId;

        public MemoryRoomAnswerRepository(IReadOnlyList<MemoryRoomAnswer> answers)
        {
            if (answers == null) throw new ArgumentNullException(nameof(answers));

            _answersByRoomId = new Dictionary<MemoryRoomId, MemoryRoomAnswer>(answers.Count);
            foreach (var answer in answers)
            {
                if (_answersByRoomId.ContainsKey(answer.RoomId))
                    throw new ArgumentException($"방 식별자가 중복되었다({answer.RoomId}).", nameof(answers));

                _answersByRoomId.Add(answer.RoomId, answer);
            }
        }

        public bool TryGetAnswer(MemoryRoomId roomId, out MemoryRoomAnswer answer) =>
            _answersByRoomId.TryGetValue(roomId, out answer);

        public bool TryGetPublicInfo(MemoryRoomId roomId, out MemoryRoomPublicInfo info)
        {
            if (!_answersByRoomId.TryGetValue(roomId, out var answer))
            {
                info = default;
                return false;
            }

            info = answer.ToPublicInfo();
            return true;
        }
    }
}
