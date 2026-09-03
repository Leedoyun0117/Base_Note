using System;
using System.Collections.Generic;
using GameName.Core.Dialogue;
using GameName.Core.MemoryRooms;

namespace GameName.Core.Authoring
{
    // 원문을 실제로 훑어 검열 토큰 목록을 만드는 구현.
    //
    // 방금 훑은 판 하나를 들고 있다가 같은 판을 다시 물으면 그대로 돌려준다.
    // 한 번의 Validate 안에서 여러 규칙이 차례로 묻기 때문이며, 그 이상은
    // 노리지 않는다 — 캐시가 아니라 "직전 것 한 개"다.
    //
    // 같은 판인지 값이 아니라 참조로 판단하는 이유: RunDefinition은 저작
    // 데이터에서 한 번 조립된 뒤 바뀌지 않으므로, 같은 객체라면 내용도 같다.
    // 값으로 비교하려면 판 전체를 비교해야 해서 훑는 것보다 비싸진다.
    public sealed class CensorTokenIndexSource : ICensorTokenIndexSource
    {
        private readonly ICensoredTextParser _parser;

        private RunDefinition _lastRun;
        private CensorTokenIndex _lastIndex;

        public CensorTokenIndexSource(ICensoredTextParser parser)
        {
            _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        }

        public CensorTokenIndex For(RunDefinition run)
        {
            if (run == null) throw new ArgumentNullException(nameof(run));

            if (ReferenceEquals(run, _lastRun))
                return _lastIndex;

            _lastIndex = Build(run);
            _lastRun = run;
            return _lastIndex;
        }

        private CensorTokenIndex Build(RunDefinition run)
        {
            var uses = new List<CensorTokenUse>();

            foreach (var room in run.Rooms)
            foreach (var line in room.DialogueLines)
            {
                Collect(uses, room.Id, line.AuthoredText, $"방 {room.Id}의 대사 {line.Id}");

                foreach (var choice in line.Choices)
                {
                    Collect(
                        uses, room.Id, choice.AuthoredText,
                        $"방 {room.Id}의 대사 {line.Id}에 달린 선택지 {choice.Id}");
                }
            }

            return new CensorTokenIndex(uses);
        }

        private void Collect(
            ICollection<CensorTokenUse> uses, MemoryRoomId roomId, string authoredText, string location)
        {
            foreach (var segment in _parser.Parse(authoredText).Segments)
            {
                if (segment.Key.HasValue)
                    uses.Add(new CensorTokenUse(segment.Key.Value, segment.Color.Value, roomId, location));
            }
        }
    }
}
