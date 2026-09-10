using System.Collections.Generic;
using GameName.Core.Authoring;
using GameName.Core.Clues;
using GameName.Core.Complexes;
using GameName.Core.MemoryRooms;

namespace GameName.UI.Session
{
    // 실제 기획 데이터(레벨 에디터 산출물, 세이브 데이터 등)가 생기기 전까지
    // 쓰는 더미 데이터. 이 파일 하나만 실제 데이터 소스로 교체하면 GameSession
    // 이하 나머지 코드는 전혀 바뀌지 않는다.
    //
    // ── 서사 뼈대(재작성 예정) ────────────────────────────────────────────
    // 화자 POV는 나츠. 상대는 소꿉친구 유키.
    //   라운드1 가라앉다  — 유년기 옥상, 낡은 모포. (과거·후회·침체)
    //   라운드2 천사       — 청소년기 작별. (미화·이상화·낙관)
    //   라운드3 무제       — 사고, 전하지 못한 말. (모순·복합, 다중 컴플렉스)
    // 구조만 준비한다 — 콘텐츠 저작은 이번 범위 밖이다.
    internal static class DemoGameData
    {
        public static readonly MemoryRoomId Round1 = new MemoryRoomId("round-1");
        public static readonly MemoryRoomId Round2 = new MemoryRoomId("round-2");
        public static readonly MemoryRoomId Round3 = new MemoryRoomId("round-3");

        private const int RunSeed = 20260910;
        private const int StartingStability = 0;
        private const int StabilityMin = -100;
        private const int StabilityMax = 100;

        // 단서가 놓이는 가로 자리(비율). 종류별로 나눠 겹침 경고를 피한다.
        private static readonly float[] FloorSlots = { 0.18f, 0.50f, 0.82f };
        private static readonly float[] PosterSlots = { 0.30f, 0.70f };

        // 최종 태그의 감정 축 값 → 안정 축 이동량. 임시 튜닝치다.
        private static readonly Dictionary<string, int> EmotionShift = new Dictionary<string, int>
        {
            { "그리움", -2 },
            { "후회", -6 },
            { "평온", 3 },
            { "격정", 8 },
            { "분노", 10 },
        };

        // |안정 위치| → 그 턴에 새 컴플렉스가 생길 확률(계단식). 임시치.
        private static readonly Dictionary<int, float> SpawnChance = new Dictionary<int, float>
        {
            { 0, 0.00f },
            { 20, 0.10f },
            { 40, 0.35f },
            { 70, 0.60f },
        };

        public static GameSessionData CreateWorldData() => CreateWorldData(roomLimit: 0);

        // roomLimit: 앞에서부터 몇 개의 라운드만 살릴지. 0 이하거나 라운드 수
        // 이상이면 전부 쓴다.
        public static GameSessionData CreateWorldData(int roomLimit)
        {
            var rounds = new List<RoomDefinition> { BuildRound1(), BuildRound2(), BuildRound3() };
            var roomIds = new List<MemoryRoomId> { Round1, Round2, Round3 };

            if (roomLimit > 0 && roomLimit < rounds.Count)
            {
                rounds.RemoveRange(roomLimit, rounds.Count - roomLimit);
                roomIds.RemoveRange(roomLimit, roomIds.Count - roomLimit);
            }

            var placements = new List<CluePlacement>();
            foreach (var round in rounds)
            foreach (var clue in round.Clues)
                placements.Add(new CluePlacement(round.Id, clue));

            var run = new RunDefinition(
                rounds.ToArray(), RunSeed, StartingStability, StabilityMin, StabilityMax);

            return new GameSessionData(
                placements, roomIds, run, BuildComplexes(), EmotionShift, SpawnChance);
        }

        // ── 라운드 ────────────────────────────────────────────────────────

        private static RoomDefinition BuildRound1() =>
            new RoomDefinition(
                Round1,
                new[]
                {
                    Clue("clue-r1-blanket", "낡은 모포", ClueKind.FloorObject, FloorSlots[0],
                        "춥다고 하니까 네가 말없이 덮어 줬어.",
                        P("유키"), E("그리움"), T("유년기")),
                    Clue("clue-r1-book", "표지가 닳은 그림책", ClueKind.FloorObject, FloorSlots[1],
                        "같은 페이지만 몇 번이고 읽어 달라고 했지.",
                        P("나츠"), E("평온"), T("유년기")),
                    Clue("clue-r1-net", "부러진 매미채", ClueKind.FloorObject, FloorSlots[2],
                        "해 질 때까지 잡지도 못하면서 뛰어다녔어.",
                        P("나츠"), E("평온"), T("낮")),
                    Clue("clue-r1-drawing", "크레파스로 그린 그림", ClueKind.Poster, PosterSlots[0],
                        "둘이 나란히 선 그림. 내가 더 크게 그렸더라.",
                        P("유키"), E("그리움"), T("유년기")),
                    Clue("clue-r1-stars", "빛바랜 별자리 포스터", ClueKind.Poster, PosterSlots[1],
                        "네가 별자리 이름을 하나씩 짚어 줬어. 다 외우고 있었지.",
                        P("유키"), E("후회"), T("밤")),
                },
                turnsToSurvive: 4,
                startingComplexId: Id("complex-sink"),
                complexPoolIds: new[] { Id("complex-erase"), Id("complex-amplify") });

        private static RoomDefinition BuildRound2() =>
            new RoomDefinition(
                Round2,
                new[]
                {
                    Clue("clue-r2-photo", "빛바랜 사진 한 장", ClueKind.Poster, PosterSlots[0],
                        "둘이 찍은 유일한 사진. 결국 너만 갖고 갔어.",
                        P("유키"), E("그리움"), T("청소년기")),
                    Clue("clue-r2-ribbon", "교복 리본", ClueKind.FloorObject, FloorSlots[0],
                        "그날 손에 꼭 쥐고 있던 것.",
                        P("유키"), E("후회"), T("청소년기")),
                    Clue("clue-r2-letter", "부치지 못한 편지", ClueKind.FloorObject, FloorSlots[1],
                        "주려다 못 준 채로, 아직 봉해져 있어.",
                        P("나츠"), E("후회"), T("청소년기")),
                    Clue("clue-r2-tape", "이름 없는 카세트테이프", ClueKind.FloorObject, FloorSlots[2],
                        "대신 이걸 건넸지. 늘어질 때까지 들었대.",
                        P("유키"), E("평온"), T("청소년기")),
                    Clue("clue-r2-poster", "귀퉁이가 찢어진 밴드 포스터", ClueKind.Poster, PosterSlots[1],
                        "둘 다 좋아하던 밴드. 이제 이름도 가물가물해.",
                        P("나츠"), E("그리움"), T("청소년기")),
                },
                turnsToSurvive: 4,
                startingComplexId: Id("complex-idealize"),
                complexPoolIds: new[] { Id("complex-amplify") });

        private static RoomDefinition BuildRound3() =>
            new RoomDefinition(
                Round3,
                new[]
                {
                    Clue("clue-r3-watch", "깨진 손목시계", ClueKind.FloorObject, FloorSlots[0],
                        "멈춘 채로 네가 계속 쥐고 있었어.",
                        P("유키"), E("분노"), T("사고")),
                    Clue("clue-r3-keychain", "낡은 열쇠고리", ClueKind.FloorObject, FloorSlots[1],
                        "언제부터 갖고 있었는지도 모르겠어.",
                        P("나츠"), E("그리움"), T("사고")),
                    Clue("clue-r3-coin", "구부러진 동전", ClueKind.FloorObject, FloorSlots[2],
                        "그때 하려던 말은, 끝내 못 들었네.",
                        P("나츠"), E("후회"), T("사고")),
                    Clue("clue-r3-xray", "엑스레이 필름", ClueKind.Poster, PosterSlots[0],
                        "하얗게 갈라진 선. 오래 못 봤어.",
                        P("유키"), E("분노"), T("사고")),
                    Clue("clue-r3-notice", "떼어낸 게시판 안내문", ClueKind.Poster, PosterSlots[1],
                        "누가 떼어 뒀더라. 읽고 싶지 않았어.",
                        P("나츠"), E("후회"), T("사고")),
                },
                turnsToSurvive: 5,
                startingComplexId: Id("complex-deny"),
                complexPoolIds: new[]
                {
                    Id("complex-sink"), Id("complex-idealize"), Id("complex-amplify"),
                });

        // ── 컴플렉스 (임시) ───────────────────────────────────────────────
        // 종류·규칙은 골조만 — 밸런싱·콘텐츠는 이번 범위 밖이다.
        private static IReadOnlyList<ComplexDefinition> BuildComplexes() => new[]
        {
            // 가라앉다: 어떤 감정이든 후회로 끌어내린다.
            Complex("complex-sink", "가라앉다", priority: 10, durationTurns: 3, ComplexKind.Transform,
                TagTransformRule.Transform(TagPattern.AnyOf(StoryTagAxis.Emotion), E("후회"))),

            // 천사: 후회를 그리움으로 미화한다.
            Complex("complex-idealize", "천사", priority: 10, durationTurns: 3, ComplexKind.Transform,
                TagTransformRule.Transform(new TagPattern(StoryTagAxis.Emotion, "후회"), E("그리움"))),

            // 증폭: 감정이 있으면 격정을 함께 얹는다.
            Complex("complex-amplify", "증폭", priority: 20, durationTurns: 2, ComplexKind.Amplify,
                TagTransformRule.Amplify(TagPattern.AnyOf(StoryTagAxis.Emotion), E("격정"))),

            // 삭제: 시간 감각을 지운다.
            Complex("complex-erase", "삭제", priority: 5, durationTurns: 2, ComplexKind.Remove,
                TagTransformRule.Remove(TagPattern.AnyOf(StoryTagAxis.Time))),

            // 거부: 분노를 지우고, 그 뒤 체인이 다시 만들어도 막는다.
            Complex("complex-deny", "거부", priority: 1, durationTurns: 4, ComplexKind.Reject,
                TagTransformRule.Reject(new TagPattern(StoryTagAxis.Emotion, "분노"))),
        };

        // ── 조립 헬퍼 ────────────────────────────────────────────────────

        private static ComplexId Id(string value) => new ComplexId(value);
        private static StoryTag P(string value) => StoryTag.Person(value);
        private static StoryTag E(string value) => StoryTag.Emotion(value);
        private static StoryTag T(string value) => StoryTag.Time(value);

        private static ClueDefinition Clue(
            string id, string displayName, ClueKind kind, float ratio, string story,
            params StoryTag[] tags) =>
            new ClueDefinition(
                new ClueId(id), kind, displayName, new CluePositionRatio(ratio), story, tags);

        private static ComplexDefinition Complex(
            string id, string displayName, int priority, int durationTurns, ComplexKind kind,
            params TagTransformRule[] rules) =>
            new ComplexDefinition(new ComplexId(id), priority, durationTurns, kind, rules, displayName);
    }
}
