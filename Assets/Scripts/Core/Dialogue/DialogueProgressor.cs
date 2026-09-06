using System;
using System.Collections.Generic;
using GameName.Core.Authoring;
using GameName.Core.Clues;
using GameName.Core.Events;
using GameName.Core.MemoryRooms;
using GameName.Core.Trust;

namespace GameName.Core.Dialogue
{
    // 방의 대화를 굴리는 처리기 — 현재 라인을 들고, 표시 가능한 선택지를
    // 걸러 내고, 선택을 처리한다.
    //
    // 조건 판정을 직접 구현하지 않는다. 검열 조건은 ICensorResolver.IsRevealed에,
    // 단서 사용 조건은 IClueStateReader 조회에 그대로 위임한다 — 대사 속 검열과
    // 선택지 조건이 같은 판정을 써야 "대사에서는 드러난 사실인데 선택지만 잠겨
    // 있는" 어긋남이 생기지 않기 때문이다.
    //
    // 오답 선택 시 신뢰도를 직접 만지지 않고 ITrustGauge.Decrease에 위임한다.
    // 얼마를 깎을지(여기서는 1)는 이 처리기의 판단이지만, 0 하한과 변화 이벤트는
    // 게이지의 몫이다.
    //
    // 신뢰도가 0이면 방은 이미 실패로 닫힌 뒤다(RoomCompletionArbiter가 처리).
    // 그 뒤로 들어오는 선택 입력은 조용히 무시한다.
    public sealed class DialogueProgressor
    {
        private readonly IReadOnlyList<RoomDefinition> _rooms;
        private readonly ICensorResolver _censorResolver;

        // 읽기가 아니라 쓰기 경계인 이유: ClueSelection 줄에서 고른 단서를 곧바로
        // UsedInDialogue로 전이시켜야 한다. 별도 처리기를 두지 않고 여기서 하는
        // 것은, 이 전이가 "그 답으로 대화가 갈렸다"는 이 처리기의 결정과 한
        // 덩어리이기 때문이다(오답 선택의 신뢰 감소를 이 처리기가 하는 것과 같다).
        private readonly IClueStateMutator _clueState;
        private readonly ITrustGauge _trust;
        private readonly IEventBus _eventBus;

        private readonly Dictionary<DialogueLineId, DialogueLineDefinition> _linesById =
            new Dictionary<DialogueLineId, DialogueLineDefinition>();

        // 런에 등장하는 모든 단서의 정의 — 방을 가리지 않는다. 단서 상태가 런
        // 전체에 걸쳐 누적되면서(ClueStateStore) 다른 방에서 집은 단서도 지금
        // 방 대화에 답으로 낼 수 있어야 하고, 이 처리기가 그 단서의 태그를
        // 알려면 방 하나가 아니라 런 전체의 저작 데이터를 봐야 한다.
        private readonly Dictionary<ClueId, ClueDefinition> _clueDefinitionsById =
            new Dictionary<ClueId, ClueDefinition>();

        private MemoryRoomId _roomId;
        private DialogueLineId? _currentLineId;

        public DialogueProgressor(
            IReadOnlyList<RoomDefinition> rooms,
            ICensorResolver censorResolver,
            IClueStateMutator clueState,
            ITrustGauge trust,
            IEventBus eventBus)
        {
            _rooms = rooms ?? throw new ArgumentNullException(nameof(rooms));
            _censorResolver = censorResolver ?? throw new ArgumentNullException(nameof(censorResolver));
            _clueState = clueState ?? throw new ArgumentNullException(nameof(clueState));
            _trust = trust ?? throw new ArgumentNullException(nameof(trust));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));

            foreach (var room in _rooms)
            foreach (var clue in room.Clues)
                _clueDefinitionsById[clue.Id] = clue;

            _eventBus.Subscribe<RoomStartedEvent>(e => LoadRoom(e.RoomIndex));
        }

        public DialogueLineId? CurrentLineId => _currentLineId;

        // 지금 라인의 저작 데이터(화자·원문·선택지 원문). 대화 패널이 화면에
        // 그릴 것을 여기서 얻는다 — CurrentLineId만으로는 매번 방 목록을 다시
        // 훑어야 하는데, 그 훑기를 이 처리기가 이미 _linesById로 하고 있다.
        // 대화가 끝났거나 아직 시작 전이면 null이다.
        public DialogueLineDefinition CurrentLine =>
            _currentLineId.HasValue && _linesById.TryGetValue(_currentLineId.Value, out var line)
                ? line
                : null;

        // ClueSelection 줄에서 답으로 낼 수 있는 단서 — 지금 들고 있는 것(Collected)만.
        // 방을 가리지 않는다 — 다른 방에서 집은 단서도 손에 있으면 답으로 낼 수
        // 있다. 추출했거나 이미 대화에 쓰거나 버린 단서는 손에 없으므로 제외한다.
        // 이름을 함께 실어 화면이 단서 정의를 다시 뒤지지 않게 한다.
        public IReadOnlyList<KeyValuePair<ClueId, string>> SelectableClues()
        {
            var result = new List<KeyValuePair<ClueId, string>>();

            var line = CurrentLine;
            if (line == null || line.PromptKind != DialoguePromptKind.ClueSelection)
                return result;

            foreach (var clue in _clueDefinitionsById.Values)
            {
                if (_clueState.TryGetState(clue.Id, out var state) && state == ClueState.Collected)
                    result.Add(new KeyValuePair<ClueId, string>(clue.Id, clue.DisplayName));
            }

            return result;
        }

        // 현재 라인에서 지금 조건이 맞아 화면에 내보낼 수 있는 선택지들.
        public IReadOnlyList<ChoiceDefinition> VisibleChoices()
        {
            var result = new List<ChoiceDefinition>();
            if (!_currentLineId.HasValue || !_linesById.TryGetValue(_currentLineId.Value, out var line))
                return result;

            foreach (var choice in line.Choices)
            {
                if (IsChoiceVisible(choice))
                    result.Add(choice);
            }

            return result;
        }

        public ChoiceSelectionResult Select(ChoiceId choiceId)
        {
            if (_trust.Current == 0)
                return ChoiceSelectionResult.Ignored();

            if (!_currentLineId.HasValue || !_linesById.TryGetValue(_currentLineId.Value, out var line))
                return ChoiceSelectionResult.Rejected();

            ChoiceDefinition selected = null;
            foreach (var choice in line.Choices)
            {
                if (choice.Id.Equals(choiceId) && IsChoiceVisible(choice))
                {
                    selected = choice;
                    break;
                }
            }

            if (selected == null)
                return ChoiceSelectionResult.Rejected();

            var roomAtSelection = _roomId;
            _eventBus.Publish(new ChoiceSelectedEvent(_currentLineId.Value, choiceId));

            // 오답이면 신뢰도를 깎는다. 이 한 줄이 동기 연쇄를 일으킬 수 있다 —
            // 신뢰도가 0이 되면 RoomCompletionArbiter가 RoomFailedEvent를 내고,
            // RunProgressor가 그 자리에서 우리를 다음 방으로 옮겨 버린다(LoadRoom이
            // 재진입으로 실행된다). 그래서 신뢰도 조작은 이 처리기가 자기 상태를
            // 더 건드리기 전에 마지막으로 한다.
            if (!selected.IsCorrect)
                _trust.Decrease(1);

            // 신뢰도가 0으로 떨어져 방이 실패로 닫혔다면(그리고 다음 방으로
            // 옮겨졌을 수 있다면) 이 선택의 대화 이동은 없던 일이 된다.
            if (_trust.Current == 0 || !_roomId.Equals(roomAtSelection))
                return ChoiceSelectionResult.DialogueEnded();

            if (selected.Next.HasValue)
            {
                EnterLine(selected.Next.Value);
                return ChoiceSelectionResult.Advanced();
            }

            _currentLineId = null;
            _eventBus.Publish(new DialogueEndedEvent(roomAtSelection));
            return ChoiceSelectionResult.DialogueEnded();
        }

        // ClueSelection 줄에 단서로 답한다. 정답 집합에 속하면 CorrectNext로,
        // 아니면 IncorrectNext(오답 서브체인)로 간다.
        //
        // 오답이어도 신뢰는 깎지 않는다 — 신뢰 감소는 텍스트 선택지의 오답에서만
        // 일어난다(잘못 말한 것과 잘못 답한 것을 같은 무게로 두지 않는다). 대신
        // 고른 단서는 어느 쪽이든 UsedInDialogue로 소모되어 다시 답으로 낼 수 없다.
        public ChoiceSelectionResult SelectClue(ClueId clueId)
        {
            if (_trust.Current == 0)
                return ChoiceSelectionResult.Ignored();

            var line = CurrentLine;
            if (line == null || line.PromptKind != DialoguePromptKind.ClueSelection
                || !line.CorrectNext.HasValue || !line.IncorrectNext.HasValue)
            {
                return ChoiceSelectionResult.Rejected();
            }

            // 지금 들고 있는 단서만 답이 될 수 있다 — 어느 방에서 집었는지는 안 본다.
            if (!_clueState.TryGetState(clueId, out var state) || state != ClueState.Collected
                || !_clueDefinitionsById.TryGetValue(clueId, out var clueDefinition))
            {
                return ChoiceSelectionResult.Rejected();
            }

            _clueState.SetState(clueId, ClueState.UsedInDialogue);

            var correct = HasAnyMatchingTag(clueDefinition.Tags, line.RequiredTags);
            _eventBus.Publish(new ClueAnsweredEvent(correct));
            EnterLine(correct ? line.CorrectNext.Value : line.IncorrectNext.Value);
            return ChoiceSelectionResult.Advanced();
        }

        // 단서로 답하지 않고 넘어간다 — 답으로 낼 단서가 없거나, 그냥 넘기고
        // 싶을 때. 오답 서브체인(IncorrectNext)으로 간다. 단서는 소모하지 않는다.
        // 이 줄이 하드 블록이 되지 않게 하는 안전판이다.
        public ChoiceSelectionResult SkipClueSelection()
        {
            if (_trust.Current == 0)
                return ChoiceSelectionResult.Ignored();

            var line = CurrentLine;
            if (line == null || line.PromptKind != DialoguePromptKind.ClueSelection
                || !line.IncorrectNext.HasValue)
            {
                return ChoiceSelectionResult.Rejected();
            }

            _eventBus.Publish(new ClueAnsweredEvent(false));
            EnterLine(line.IncorrectNext.Value);
            return ChoiceSelectionResult.Advanced();
        }

        private void LoadRoom(int roomIndex)
        {
            _linesById.Clear();
            _currentLineId = null;

            if (roomIndex < 0 || roomIndex >= _rooms.Count)
                return;

            var room = _rooms[roomIndex];
            _roomId = room.Id;

            foreach (var line in room.DialogueLines)
                _linesById[line.Id] = line;

            if (room.StartLineId.HasValue && _linesById.ContainsKey(room.StartLineId.Value))
                EnterLine(room.StartLineId.Value);
        }

        private void EnterLine(DialogueLineId lineId)
        {
            _currentLineId = lineId;
            _eventBus.Publish(new DialogueLineEnteredEvent(lineId));
        }

        // 단서 하나가 여러 태그를 가질 수 있고 정답 태그도 여러 개일 수 있어
        // (예: 방을 옮겨 다니며 같은 사실을 가리키는 단서가 늘어나는 경우),
        // 하나라도 걸치면 정답으로 본다 — 태그 집합끼리의 교집합 유무만 본다.
        private static bool HasAnyMatchingTag(IReadOnlyList<ClueTag> clueTags, IReadOnlyList<ClueTag> requiredTags)
        {
            for (var i = 0; i < clueTags.Count; i++)
            {
                for (var j = 0; j < requiredTags.Count; j++)
                {
                    if (clueTags[i].Equals(requiredTags[j]))
                        return true;
                }
            }

            return false;
        }

        private bool IsChoiceVisible(ChoiceDefinition choice)
        {
            switch (choice.Condition.Kind)
            {
                case ChoiceConditionKind.None:
                    return true;

                case ChoiceConditionKind.CensorKeyRevealed:
                    return choice.Condition.RequiredCensorKey.HasValue
                        && _censorResolver.IsRevealed(choice.Condition.RequiredCensorKey.Value);

                case ChoiceConditionKind.ClueUsed:
                    return choice.Condition.RequiredClue.HasValue
                        && _clueState.TryGetState(choice.Condition.RequiredClue.Value, out var state)
                        && state == ClueState.UsedInDialogue;

                default:
                    return false;
            }
        }
    }
}
