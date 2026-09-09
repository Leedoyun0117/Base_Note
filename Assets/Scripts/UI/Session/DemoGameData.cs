using System.Collections.Generic;
using GameName.Core.Authoring;
using GameName.Core.Clues;
using GameName.Core.Dialogue;
using GameName.Core.Inventory;
using GameName.Core.Memories;
using GameName.Core.MemoryRooms;
using GameName.Core.Mind;

namespace GameName.UI.Session
{
    // 실제 기획 데이터(레벨 에디터 산출물, 세이브 데이터 등)가 생기기 전까지
    // 쓰는 더미 데이터. 이 파일 하나만 실제 데이터 소스로 교체하면 GameSession
    // 이하 나머지 코드는 전혀 바뀌지 않는다.
    //
    // ── 서사 뼈대 ──────────────────────────────────────────────────────
    // 화자 POV는 나츠. 상대는 소꿉친구 유키(대사의 Speaker는 대부분 유키다).
    //   방1(B, 유년기)  — 그 여름밤 옥상, 낡은 모포.        검열 키: rooftop-blanket
    //   방2(G, 청소년기) — 유키가 전학 가던 날의 작별.       검열 키: rooftop-goodbye
    //   방3(R, 트라우마) — 사고, 전하지 못한 마지막 말.      검열 키: missed-words
    // 색의 관성: B·G는 같은 장소(옥상)로, G·R은 유키와의 마지막 순간으로 이어진다.
    // 방3은 방1·2의 사건을 다시 언급하지 않는다 — 쌓인 정서적 맥락만으로 추론한다.
    //
    // 검열 토큰 포맷은 파서 그대로: [[색:키:실제텍스트]] (색은 R/G/B).
    internal static class DemoGameData
    {
        public static readonly MemoryRoomId Room1 = new MemoryRoomId("room-1");
        public static readonly MemoryRoomId Room2 = new MemoryRoomId("room-2");
        public static readonly MemoryRoomId Room3 = new MemoryRoomId("room-3");

        private const string Yuki = "유키";

        // 방 사양: 시작 신뢰도 3, 시작 히로민 15(추출 9, 이동 문턱과 같은 값이라
        // 런 시작 시엔 항상 곧장 이동할 수 있다), 대화 1회 +3, 런당 기회 2.
        private const int StartingTrust = 3;
        private const int StartingHiromi = 15;
        private const int StartingChance = 2;
        private const int MoveHiromiCost = 15;

        // 안정 축: 침체 -100 ~ 안정 0 ~ 흥분 +100. 런 시작 시 안정(0)에서 출발한다.
        // 피드백 대사가 이 값을 움직이는 것은 후속 단계에서 붙는다.
        private const int StartingStability = 0;
        private const int StabilityMin = -100;
        private const int StabilityMax = 100;

        // 신뢰(유키의 인내심)가 안정 축 이탈로 깎이는 규칙: |안정 위치| 가 20 이내면
        // 깎이지 않고, 넘어서면 매 답변마다 round((|위치| - 20) / 10) 만큼 깎인다.
        private const int TrustErosionFreeBand = 20;
        private const int TrustErosionDivisor = 10;

        // 심리 상태 시작값. 기억 해석 방식을 결정한다(후속 단계).
        private const PsychologyState StartingPsychology = PsychologyState.Optimism;

        // 랜덤 분기 풀 확정 시드. 데모 데이터엔 아직 풀이 없어 쓰이지 않지만,
        // 실제 데이터 소스가 생기기 전까지 고정값을 박아 둔다.
        private const int RunSeed = 20260903;

        // 신뢰도 → 방 가시 비율. 신뢰로 방을 좁히던 마스크 연출은 이번 개편에서
        // 꺼 둔다([G] 시청각 피드백은 범위 밖) — 배관(IVisibilityPolicy·
        // IClueAccessPolicy·MemoryRoomMaskController)은 그대로 두고 표를 전부
        // 1.0으로 채워 항상 방 전체가 보이게만 한다. 조사 페이즈/대화 페이즈가
        // 갈리면 조사 제한은 별도 축(조사 횟수)이 맡는다.
        private static readonly Dictionary<int, float> VisibilityByTrust = new Dictionary<int, float>
        {
            { 3, 1.0f },
            { 2, 1.0f },
            { 1, 1.0f },
            { 0, 1.0f },
        };

        // 검열 키 하나를 풀려면 제시할 기억이 어느 태그를 가져야 하는지.
        // 색과 달리 대사 원문에서 저절로 나오지 않아 따로 적는다 — 각 키를
        // 실제로 푸는 핵심 단서에 매겨 둔 태그와 짝을 맞춘다.
        private static readonly CensorKeyTagRequirement[] CensorKeyTagRequirements =
        {
            new CensorKeyTagRequirement(
                new CensorKey("rooftop-blanket"), new[] { new ClueTag("room1.blanket") }),
            new CensorKeyTagRequirement(
                new CensorKey("rooftop-goodbye"), new[] { new ClueTag("room2.goodbye") }),
            new CensorKeyTagRequirement(
                new CensorKey("missed-words"), new[] { new ClueTag("room3.missedWords") }),
        };

        // 단서가 놓이는 가로 자리(비율). 종류별로 나눠 두어 겹침 경고를 피한다.
        private static readonly float[] FloorSlots = { 0.18f, 0.50f, 0.82f };
        private static readonly float[] PosterSlots = { 0.30f, 0.70f };

        public static GameSessionData CreateWorldData() => CreateWorldData(roomLimit: 0);

        // roomLimit: 앞에서부터 몇 개의 방만 살릴지. 0 이하거나 방 수 이상이면
        // 전부 쓴다. 방 3(R)을 손대지 않고 "앞 두 방만 도는 판"을 만드는 데 쓴다 —
        // 마지막 방을 마치면 RunProgressor가 그대로 RunCompletedEvent를 낸다.
        public static GameSessionData CreateWorldData(int roomLimit)
        {
            var rooms = new List<RoomDefinition> { BuildRoom1(), BuildRoom2(), BuildRoom3() };
            var roomIds = new List<MemoryRoomId> { Room1, Room2, Room3 };

            if (roomLimit > 0 && roomLimit < rooms.Count)
            {
                rooms.RemoveRange(roomLimit, rooms.Count - roomLimit);
                roomIds.RemoveRange(roomLimit, roomIds.Count - roomLimit);
            }

            var cluePlacements = new List<CluePlacement>();
            foreach (var room in rooms)
                AddPlacements(cluePlacements, room);

            // 검열 키별 요구 태그는 목록 그대로 넘긴다 — 살아 있는 방이 쓰지 않는
            // 키의 항목은 검증기도 런타임도 참조하지 않아 무해하다.
            var run = new RunDefinition(
                rooms.ToArray(), StartingTrust, StartingHiromi, RunSeed,
                CensorKeyTagRequirements, StartingChance, MoveHiromiCost,
                StartingStability, StabilityMin, StabilityMax,
                TrustErosionFreeBand, TrustErosionDivisor, StartingPsychology);

            return new GameSessionData(cluePlacements, roomIds, run, VisibilityByTrust);
        }

        // 방 단서는 5개까지 놓이지만 가방은 3칸이다 — 한 방의 단서를 전부
        // 담을 수 없으므로 "무엇을 들고 나갈지" 고르는 선택이 강제된다.
        public static GameSessionSettings CreateSettings() =>
            new GameSessionSettings(new InventorySettings(initialCapacity: 3));

        // ── 대화는 전부 "가진 물건으로 답하기"다 ──────────────────────────
        // 텍스트 선택지는 없다. 매 줄에서 유키가 뭔가를 묻고, 플레이어는 손에
        // 든 단서(물건) 하나를 골라 답한다. 그 물건의 태그가 정답 태그와 걸치면
        // CorrectNext로, 아니면 IncorrectNext로 간다. 분기가 비어 있으면(null)
        // 그 답으로 방의 대화가 끝난다.
        //
        // 단서마다 태그를 하나씩 매겨 "어느 물건이 어느 질문의 답인지"를 정한다.
        // 검열 토큰([[색:키:원문]])은 그대로 둔다 — 추출한 기억을 제시해 푸는
        // 별개 상호작용이고, 답하기와 함께 걸려도 무해하다.

        // ── 방1 — B, 유년기, 옥상·모포 ─────────────────────────────────────
        private static RoomDefinition BuildRoom1()
        {
            var clues = new[]
            {
                // 핵심 단서 — rooftop-blanket 검열을 여는 것도 room1.blanket 태그다.
                Clue("clue-r1-blanket", "낡은 모포", ClueKind.FloorObject, FloorSlots[0], MemoryColor.Blue,
                    "room1.blanket"),
                Clue("clue-r1-picturebook", "표지가 닳은 그림책", ClueKind.FloorObject, FloorSlots[1], MemoryColor.Red,
                    "room1.book"),
                Clue("clue-r1-cicada-net", "부러진 매미채", ClueKind.FloorObject, FloorSlots[2], MemoryColor.Green,
                    "room1.daytime"),
                Clue("clue-r1-drawing", "크레파스로 그린 그림", ClueKind.Poster, PosterSlots[0], MemoryColor.Green,
                    "room1.drawing"),
                Clue("clue-r1-star-poster", "빛바랜 별자리 포스터", ClueKind.Poster, PosterSlots[1], MemoryColor.Red,
                    "room1.stars"),
            };

            var lines = new[]
            {
                ClueLine("r1-a", Yuki,
                    "우리, [[B:rooftop-blanket:그 여름밤 옥상]]에서 있었던 일 말이야. 네가 뭘 하나 들고 올라왔었잖아. ...그게 뭐였어?",
                    "room1.blanket", correctNext: "r1-b", incorrectNext: "r1-a-miss"),
                ClueLine("r1-a-miss", Yuki,
                    "아니. 그건 아니었어. ...춥다길래 네가 덮어 준 거.",
                    "room1.blanket", correctNext: "r1-b", incorrectNext: "r1-b"),

                ClueLine("r1-b", Yuki,
                    "낮엔 해 질 때까지 밖에 있었잖아. 뭐 하고 놀았더라?",
                    "room1.daytime", correctNext: "r1-c", incorrectNext: "r1-c"),

                ClueLine("r1-c", Yuki,
                    "밤엔 나란히 누워서 위를 봤지. 뭘 보고 있었어?",
                    "room1.stars", correctNext: "r1-close-warm", incorrectNext: "r1-close-plain"),

                ClueLine("r1-close-warm", Yuki,
                    "맞아. 넌 별자리 이름을 다 외우고 있었어. 하나씩 알려 줬잖아.",
                    "room1.book", correctNext: null, incorrectNext: null),
                ClueLine("r1-close-plain", Yuki,
                    "뭐, 됐어. 오래된 일이니까.",
                    "room1.book", correctNext: null, incorrectNext: null),
            };

            return new RoomDefinition(Room1, clues, new DialogueLineId("r1-a"), lines);
        }

        // ── 방2 — G, 청소년기, 옥상·작별 ──────────────────────────────────
        private static RoomDefinition BuildRoom2()
        {
            var clues = new[]
            {
                // 핵심 단서 — rooftop-goodbye 검열을 여는 것도 room2.goodbye 태그다.
                Clue("clue-r2-photo", "빛바랜 사진 한 장", ClueKind.Poster, PosterSlots[0], MemoryColor.Green,
                    "room2.goodbye"),
                // 리본과 편지 둘 다 "그때 손에 쥐고 있던 것"이라 같은 태그를 갖는다.
                Clue("clue-r2-ribbon", "교복 리본", ClueKind.FloorObject, FloorSlots[0], MemoryColor.Blue,
                    "room2.heldItem"),
                Clue("clue-r2-letter", "부치지 못한 편지", ClueKind.FloorObject, FloorSlots[1], MemoryColor.Green,
                    "room2.heldItem"),
                Clue("clue-r2-tape", "이름 없는 카세트테이프", ClueKind.FloorObject, FloorSlots[2], MemoryColor.Red,
                    "room2.mixtape"),
                Clue("clue-r2-band-poster", "귀퉁이가 찢어진 밴드 포스터", ClueKind.Poster, PosterSlots[1], MemoryColor.Red,
                    "room2.band"),
            };

            var lines = new[]
            {
                ClueLine("r2-a", Yuki,
                    "나 전학 가던 날, [[G:rooftop-goodbye:그날의 작별]] 말이야. 그때 우리가 뭘 두고 얘기했는지 기억나?",
                    "room2.goodbye", correctNext: "r2-b", incorrectNext: "r2-a-miss"),
                ClueLine("r2-a-miss", Yuki,
                    "아니. ...그 사진. 둘이 찍은 거. 결국 나만 갖고 갔지.",
                    "room2.goodbye", correctNext: "r2-b", incorrectNext: "r2-b"),

                ClueLine("r2-b", Yuki,
                    "그때 넌 손에 뭔가를 꼭 쥐고 있었어. 만지작거리면서. ...그게 뭐였어?",
                    "room2.heldItem", correctNext: "r2-c", incorrectNext: "r2-c"),

                ClueLine("r2-c", Yuki,
                    "나한테 주려던 거였잖아. 근데 끝내 안 줬어. 대신 뭘 건넸지?",
                    "room2.mixtape", correctNext: "r2-close-warm", incorrectNext: "r2-close-plain"),

                ClueLine("r2-close-warm", Yuki,
                    "그 테이프, 아직 갖고 있어. 늘어질 때까지 들었어.",
                    "room2.band", correctNext: null, incorrectNext: null),
                ClueLine("r2-close-plain", Yuki,
                    "됐어. 그래도, 물어봐 줘서 좋았어.",
                    "room2.band", correctNext: null, incorrectNext: null),
            };

            return new RoomDefinition(Room2, clues, new DialogueLineId("r2-a"), lines);
        }

        // ── 방3 — R, 트라우마, 사고·전하지 못한 말 ────────────────────────
        private static RoomDefinition BuildRoom3()
        {
            var clues = new[]
            {
                // 핵심 단서 — missed-words 검열을 여는 것도 room3.missedWords 태그다.
                Clue("clue-r3-watch", "깨진 손목시계", ClueKind.FloorObject, FloorSlots[0], MemoryColor.Red,
                    "room3.missedWords"),
                Clue("clue-r3-keychain", "낡은 열쇠고리", ClueKind.FloorObject, FloorSlots[1], MemoryColor.Blue,
                    "room3.keychain"),
                Clue("clue-r3-coin", "구부러진 동전", ClueKind.FloorObject, FloorSlots[2], MemoryColor.Green,
                    "room3.coin"),
                Clue("clue-r3-xray-poster", "엑스레이 필름", ClueKind.Poster, PosterSlots[0], MemoryColor.Red,
                    "room3.xray"),
                Clue("clue-r3-notice-poster", "떼어낸 게시판 안내문", ClueKind.Poster, PosterSlots[1], MemoryColor.Green,
                    "room3.notice"),
            };

            var lines = new[]
            {
                ClueLine("r3-a", Yuki,
                    "결국 [[R:missed-words:그때 하지 못한 말]]은 못 들었네. ...그날, 뭐가 네 손에 있었지?",
                    "room3.missedWords", correctNext: "r3-b", incorrectNext: "r3-a-miss"),
                ClueLine("r3-a-miss", Yuki,
                    "아니야. ...그 시계. 멈춘 채로 네가 계속 쥐고 있었어.",
                    "room3.missedWords", correctNext: "r3-b", incorrectNext: "r3-b"),

                ClueLine("r3-b", Yuki,
                    "[[R:missed-words:그 말]], 아직도 안에 담아두고 있지. 얼굴에 다 쓰여 있어.",
                    "room3.missedWords", correctNext: "r3-close-warm", incorrectNext: "r3-close-plain"),

                ClueLine("r3-close-warm", Yuki,
                    "그 말이었구나. ...나도. 나도 그랬어.",
                    "room3.coin", correctNext: null, incorrectNext: null),
                ClueLine("r3-close-plain", Yuki,
                    "이제 됐어. 늦었지만.",
                    "room3.coin", correctNext: null, incorrectNext: null),
            };

            return new RoomDefinition(Room3, clues, new DialogueLineId("r3-a"), lines);
        }

        // ── 조립 헬퍼 ────────────────────────────────────────────────────

        // 첫 태그가 이 단서의 중심축, 뒤따르는 것들이 곁축(장소·시간 등)이다.
        // 지금 데모는 단서마다 중심축 하나씩만 매긴다 — 곁축 콘텐츠는 등급
        // 판정기가 실제로 소비하는 단계에서 서사 맥락과 함께 붙인다.
        private static ClueDefinition Clue(
            string id, string displayName, ClueKind kind, float ratio, MemoryColor hiddenColor,
            string centerTag, params string[] subTags) =>
            new ClueDefinition(
                new ClueId(id), kind, displayName, new CluePositionRatio(ratio), hiddenColor,
                BuildTags(centerTag, subTags));

        // "가진 물건으로 답하는" 줄. 중심축 정답 태그 하나(+ 필요하면 곁축),
        // 정답/오답 다음 줄(비우면 대화 종료).
        private static DialogueLineDefinition ClueLine(
            string id, string speaker, string authoredText, string requiredCenterTag,
            string correctNext, string incorrectNext, params string[] requiredSubTags) =>
            DialogueLineDefinition.ClueSelection(
                new DialogueLineId(id), speaker, authoredText,
                BuildTags(requiredCenterTag, requiredSubTags),
                correctNext == null ? (DialogueLineId?)null : new DialogueLineId(correctNext),
                incorrectNext == null ? (DialogueLineId?)null : new DialogueLineId(incorrectNext));

        private static ClueTag[] BuildTags(string centerTag, string[] subTags)
        {
            var tags = new ClueTag[1 + (subTags?.Length ?? 0)];
            tags[0] = ClueTag.Center(centerTag);
            for (var i = 0; i < (subTags?.Length ?? 0); i++)
                tags[i + 1] = ClueTag.Sub(subTags[i]);
            return tags;
        }

        private static void AddPlacements(ICollection<CluePlacement> into, RoomDefinition room)
        {
            foreach (var clue in room.Clues)
                into.Add(new CluePlacement(room.Id, clue));
        }
    }
}
