using System.Collections.Generic;
using GameName.Core.Authoring;
using GameName.Core.Clues;
using GameName.Core.Dialogue;
using GameName.Core.Inventory;
using GameName.Core.Memories;
using GameName.Core.MemoryRooms;

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

        // 방 사양: 시작 신뢰도 3, 한 판 추출 자원 5.
        private const int StartingTrust = 3;
        private const int ExtractionBudget = 5;

        // 신뢰도 → 방 가시 비율. 3에서 방 전체가 보이고, 깎일수록 가운데만 남는다.
        private static readonly Dictionary<int, float> VisibilityByTrust = new Dictionary<int, float>
        {
            { 3, 1.0f },
            { 2, 0.75f },
            { 1, 0.5f },
            { 0, 0.5f },
        };

        // 판을 시작할 때 지갑에 들고 있는 색. R·G·B 하나씩 쥐고 출발한다 —
        // 첫 방부터 마스크 구간을 하나는 풀어 볼 수 있게 하는 시작 밑천이다.
        private static readonly Dictionary<MemoryColor, int> StartingMemoryColors =
            new Dictionary<MemoryColor, int>
            {
                { MemoryColor.Red, 1 },
                { MemoryColor.Green, 1 },
                { MemoryColor.Blue, 1 },
            };

        // 단서가 놓이는 가로 자리(비율). 종류별로 나눠 두어 겹침 경고를 피한다.
        private static readonly float[] FloorSlots = { 0.18f, 0.50f, 0.82f };
        private static readonly float[] PosterSlots = { 0.30f, 0.70f };

        public static GameSessionData CreateWorldData()
        {
            var room1 = BuildRoom1();
            var room2 = BuildRoom2();
            var room3 = BuildRoom3();

            var cluePlacements = new List<CluePlacement>();
            AddPlacements(cluePlacements, room1);
            AddPlacements(cluePlacements, room2);
            AddPlacements(cluePlacements, room3);

            var run = new RunDefinition(new[] { room1, room2, room3 }, StartingTrust, ExtractionBudget);
            var roomIds = new List<MemoryRoomId> { Room1, Room2, Room3 };

            return new GameSessionData(
                cluePlacements, roomIds, run, VisibilityByTrust, StartingMemoryColors);
        }

        // 방 단서는 5개까지 놓이지만 가방은 3칸이다 — 한 방의 단서를 전부
        // 담을 수 없으므로 "무엇을 들고 나갈지" 고르는 선택이 강제된다.
        public static GameSessionSettings CreateSettings() =>
            new GameSessionSettings(new InventorySettings(initialCapacity: 3));

        // ── 방1 — B, 유년기, 옥상·모포 ─────────────────────────────────────
        // 튜토리얼 성격: 정답 근거를 대화 안에서 직접 준다. 검열된 "옥상"과
        // 나란히 "모포"·"여름밤"·"옥상"이 검열 없이 등장해, 기억제 없이 문맥만으로
        // 유추할 수 있다. 정답 선택지 둘 다 IsCorrect지만 구체적인 쪽이 더 깊은
        // 대사로 이어지고, 오답 하나는 명백히 다른 사건을 가리켜 신뢰를 깎는다.
        // 전부 텍스트 선택지다(ClueSelection 없음).
        private static RoomDefinition BuildRoom1()
        {
            var clues = new[]
            {
                // 핵심 단서 — 파란 기억을 품고 있어 추출하면 rooftop-blanket 검열이 풀린다.
                Clue("clue-r1-blanket", "낡은 모포", ClueKind.FloorObject, FloorSlots[0], MemoryColor.Blue),
                Clue("clue-r1-picturebook", "표지가 닳은 그림책", ClueKind.FloorObject, FloorSlots[1], MemoryColor.Red),
                Clue("clue-r1-cicada-net", "부러진 매미채", ClueKind.FloorObject, FloorSlots[2], MemoryColor.Green),
                Clue("clue-r1-drawing", "크레파스로 그린 그림", ClueKind.Poster, PosterSlots[0], MemoryColor.Green),
                Clue("clue-r1-star-poster", "빛바랜 별자리 포스터", ClueKind.Poster, PosterSlots[1], MemoryColor.Red),
            };

            var lines = new[]
            {
                Line("r1-a", Yuki,
                    "우리, [[B:rooftop-blanket:그 여름밤 옥상]] 기억나? 네가 모포를 들고 올라왔었잖아.",
                    Choice("r1-a-vague", "응... 그랬던 것 같아.", isCorrect: true, next: "r1-b-vague"),
                    Choice("r1-a-specific", "그날 옥상에서 네가 춥다길래 모포 덮어줬지.", isCorrect: true, next: "r1-b-specific"),
                    Choice("r1-a-wrong", "아, 너희 집 마당에서 불꽃놀이 하던 날?", isCorrect: false, next: "r1-a")),

                Line("r1-b-vague", Yuki,
                    "그 정도로만 남았구나. ...뭐, 오래된 일이니까.",
                    Choice("r1-b-vague-end", "미안, 잘 안 떠올라.", isCorrect: true)),

                Line("r1-b-specific", Yuki,
                    "맞아. 여름밤이었고 별이 잘 보였어. 넌 아무 말 없이 옆에 있어줬지.",
                    Choice("r1-b-specific-end", "그 밤은 나도 기억해.", isCorrect: true)),
            };

            return new RoomDefinition(Room1, clues, new DialogueLineId("r1-a"), lines);
        }

        // ── 방2 — G, 청소년기, 옥상·작별 ──────────────────────────────────
        // "그날"이 방1의 옥상과 같은 자리라는 걸 플레이어가 스스로 잇는다. 이번엔
        // 대사에 "옥상"을 직접 반복하지 않는다. 구체적 선택지는 검열 키가 풀린
        // 뒤에만 보인다.
        //
        // 중간에 ClueSelection 줄(r2-q)을 하나 둔다 — "그때 뭘 쥐고 있었어?"에
        // 리본이나 편지로 답하면 정답, 엉뚱한 물건이면 오답 서브체인 2줄을 거쳐
        // 메인 줄기(r2-end)로 합류한다. 나머지 줄은 그대로 텍스트 선택지다.
        private static RoomDefinition BuildRoom2()
        {
            var clues = new[]
            {
                Clue("clue-r2-photo", "빛바랜 사진 한 장", ClueKind.Poster, PosterSlots[0], MemoryColor.Green),
                // 이 방에서도 예전 색(B)이 나올 수 있다 — 색은 시간순 방과 1:1이 아니다.
                Clue("clue-r2-ribbon", "교복 리본", ClueKind.FloorObject, FloorSlots[0], MemoryColor.Blue),
                Clue("clue-r2-letter", "부치지 못한 편지", ClueKind.FloorObject, FloorSlots[1], MemoryColor.Green),
                Clue("clue-r2-tape", "이름 없는 카세트테이프", ClueKind.FloorObject, FloorSlots[2], MemoryColor.Red),
                Clue("clue-r2-band-poster", "귀퉁이가 찢어진 밴드 포스터", ClueKind.Poster, PosterSlots[1], MemoryColor.Red),
            };

            var lines = new[]
            {
                Line("r2-a", Yuki,
                    "나 전학 가던 날 말이야. [[G:rooftop-goodbye:그날의 작별]], 넌 끝내 안 왔잖아.",
                    Choice("r2-a-vague", "그때 좀 일이 있었어.", isCorrect: true, next: "r2-b"),
                    GatedChoice(
                        "r2-a-specific", "그날 옥상에 안 나가서 미안했어.", isCorrect: true, next: "r2-c",
                        ChoiceCondition.RequiresCensorKeyRevealed(new CensorKey("rooftop-goodbye")))),

                Line("r2-b", Yuki,
                    "'일이 있었다'는 말로 덮는구나. ...늘 그런 식이었지.",
                    Choice("r2-b-continue", "…", isCorrect: true, next: "r2-q")),

                Line("r2-c", Yuki,
                    "기억하고 있었구나. 난 한참을 서 있었어. 네가 올 줄 알고.",
                    Choice("r2-c-continue", "늦었지만, 지금이라도 말할게. 미안했어.", isCorrect: true, next: "r2-q")),

                // ── 단서로 답하는 줄 ──
                DialogueLineDefinition.ClueSelection(
                    new DialogueLineId("r2-q"), Yuki,
                    "그때 넌 손에 뭔가를 꼭 쥐고 있었어. 만지작거리면서. ...그게 뭐였는지 기억나?",
                    new[] { new ClueId("clue-r2-ribbon"), new ClueId("clue-r2-letter") },
                    correctNext: new DialogueLineId("r2-q-right"),
                    incorrectNext: new DialogueLineId("r2-q-wrong-1")),

                Line("r2-q-right", Yuki,
                    "맞아. 그거였어. 손에서 안 놓더라.",
                    Choice("r2-q-right-continue", "…", isCorrect: true, next: "r2-end")),

                // 오답 서브체인 2줄 — 여기서는 목록 UI 없이 계속하기로만 진행한다.
                Line("r2-q-wrong-1", Yuki,
                    "아니. 그건 아니었어.",
                    Choice("r2-q-wrong-1-continue", "…", isCorrect: true, next: "r2-q-wrong-2")),
                Line("r2-q-wrong-2", Yuki,
                    "됐어. 사실 그렇게 중요한 것도 아니야.",
                    Choice("r2-q-wrong-2-continue", "…", isCorrect: true, next: "r2-end")),

                // 메인 줄기 합류점.
                Line("r2-end", Yuki,
                    "그래도, 물어봐 줘서 좋았어.",
                    Choice("r2-end-done", "나도.", isCorrect: true)),
            };

            return new RoomDefinition(Room2, clues, new DialogueLineId("r2-a"), lines);
        }

        // ── 방3 — R, 트라우마, 사고·전하지 못한 말 ────────────────────────
        // 방1·2의 사건은 절대 언급하지 않는다. 나츠·유키의 정서적 맥락만으로
        // "하지 못한 말"의 내용을 유추한다. 같은 키(missed-words)를 여러 줄에
        // 나눠 심어 한 번에 함께 풀리게 한다. 정답은 가장 짧고 담백한 문장이다.
        private static RoomDefinition BuildRoom3()
        {
            var clues = new[]
            {
                // 핵심 단서 — 빨간 기억을 품고 있어 추출하면 missed-words 검열이 풀린다.
                Clue("clue-r3-watch", "깨진 손목시계", ClueKind.FloorObject, FloorSlots[0], MemoryColor.Red),
                Clue("clue-r3-keychain", "낡은 열쇠고리", ClueKind.FloorObject, FloorSlots[1], MemoryColor.Blue),
                Clue("clue-r3-coin", "구부러진 동전", ClueKind.FloorObject, FloorSlots[2], MemoryColor.Green),
                Clue("clue-r3-xray-poster", "엑스레이 필름", ClueKind.Poster, PosterSlots[0], MemoryColor.Red),
                Clue("clue-r3-notice-poster", "떼어낸 게시판 안내문", ClueKind.Poster, PosterSlots[1], MemoryColor.Green),
            };

            var lines = new[]
            {
                Line("r3-a", Yuki,
                    "결국 [[R:missed-words:그때 하지 못한 말]]은 못 들었네.",
                    Choice("r3-a-continue", "......", isCorrect: true, next: "r3-b"),
                    Choice("r3-a-wrong",
                        "그날 병원 복도가 유난히 길었던 거, 기억나. 형광등이 하나 깜빡였고—",
                        isCorrect: false, next: "r3-a")),

                Line("r3-b", Yuki,
                    "[[R:missed-words:그 말]], 아직도 안에 담아두고 있지. 얼굴에 다 쓰여 있어.",
                    Choice("r3-b-short", "보고 싶었어.", isCorrect: true, next: "r3-c"),
                    Choice("r3-b-long",
                        "그때 네가 얼마나 힘들었을지 생각하면 나는 아직도 잠이 안 오고, 그날 이후로 계속—",
                        isCorrect: false, next: "r3-b")),

                Line("r3-c", Yuki,
                    "그 말이었구나. ...나도. 나도 그랬어.",
                    Choice("r3-c-end", "이제 됐어.", isCorrect: true)),
            };

            return new RoomDefinition(Room3, clues, new DialogueLineId("r3-a"), lines);
        }

        // ── 조립 헬퍼 ────────────────────────────────────────────────────

        private static ClueDefinition Clue(
            string id, string displayName, ClueKind kind, float ratio, MemoryColor hiddenColor) =>
            new ClueDefinition(new ClueId(id), kind, displayName, new CluePositionRatio(ratio), hiddenColor);

        private static DialogueLineDefinition Line(
            string id, string speaker, string authoredText, params ChoiceDefinition[] choices) =>
            new DialogueLineDefinition(new DialogueLineId(id), speaker, authoredText, choices);

        private static ChoiceDefinition Choice(string id, string text, bool isCorrect, string next = null) =>
            new ChoiceDefinition(
                new ChoiceId(id), text, isCorrect,
                next == null ? (DialogueLineId?)null : new DialogueLineId(next),
                ChoiceCondition.None);

        private static ChoiceDefinition GatedChoice(
            string id, string text, bool isCorrect, string next, ChoiceCondition condition) =>
            new ChoiceDefinition(
                new ChoiceId(id), text, isCorrect,
                next == null ? (DialogueLineId?)null : new DialogueLineId(next),
                condition);

        private static void AddPlacements(ICollection<CluePlacement> into, RoomDefinition room)
        {
            foreach (var clue in room.Clues)
                into.Add(new CluePlacement(room.Id, clue));
        }
    }
}
