using System;
using System.Collections.Generic;
using GameName.Core.Complexes;

namespace GameName.Core.Clues
{
    // 라운드에 배치되는 단서의 전체 데이터.
    //
    // 이 타입에 직접 접근해도 되는 쪽은 단서 추적기(카탈로그)와 단서 사용
    // 처리기뿐이다. 그 외의 모든 소비자(씬 렌더링 등)는 ToInfo()로 변환한
    // ClueInfo만 받는다.
    //
    // RoomId는 여기 없다 — 최초 배치는 CluePlacement가 표현한다. "방 안 어디쯤에
    // 놓여 있는가"(AuthoredPosition)는 여기 남는다: 자리 자체가 서사를 읽는
    // 실마리가 될 수 있는 저작 사실이기 때문이다.
    //
    // 3차 개편: 색·즉석 추출이 사라지면서 HiddenColor가 빠졌고, 정답 판정용
    // 중심축/곁축 태그(ClueTag)는 인물×감정×시간대 3축의 StoryTag로 바뀌었다.
    // Story는 단서를 클릭했을 때 UI에 뜨는 짧은 서사다(액자식 서사).
    public sealed class ClueDefinition
    {
        public ClueId Id { get; }
        public ClueKind Kind { get; }

        // 플레이어에게 보이는 이름("낡은 모포"). 방에 들어서면 눈에 보이는
        // 저작 사실이라 ClueInfo에도 그대로 실린다.
        public string DisplayName { get; }

        // 기획이 정한 방 안 가로 자리(0~1 비율).
        public CluePositionRatio AuthoredPosition { get; }

        // 단서를 클릭했을 때 뜨는 짧은 서사. 화면에 그대로 나가는 원문이다 —
        // Core에 문장이 들어오는 건 규칙 위반이 아니다(금지된 것은 "코드에 적힌
        // 대사"이고 이 값은 저작 데이터에서 흘러 들어온다).
        public string Story { get; }

        // 이 단서가 무엇에 대한 것인지를 인물·감정·시간대 세 축으로 적은 태그.
        // 클릭 시 이 태그가 활성 컴플렉스 체인을 통과해 최종 태그가 나오고,
        // 그 최종 태그의 감정 축이 안정 축을 움직인다. ClueInfo로 새어 나가지
        // 않는다 — 태그를 그대로 보여 주면 해석의 여지가 사라진다.
        public IReadOnlyList<StoryTag> Tags { get; }

        public ClueDefinition(
            ClueId id,
            ClueKind kind,
            string displayName,
            CluePositionRatio authoredPosition,
            string story = null,
            IReadOnlyList<StoryTag> tags = null)
        {
            Id = id;
            Kind = kind;
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            AuthoredPosition = authoredPosition;
            Story = story ?? string.Empty;
            Tags = tags ?? Array.Empty<StoryTag>();
        }

        public ClueInfo ToInfo() => new ClueInfo(Id, Kind, DisplayName, AuthoredPosition);
    }
}
