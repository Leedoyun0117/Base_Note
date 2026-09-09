using System;
using System.Collections.Generic;
using GameName.Core.Clues;
using GameName.Core.Dialogue;
using GameName.Core.Events;
using GameName.Core.Extraction;
using GameName.Core.Memories;
using GameName.Core.MemoryRooms;

namespace GameName.UI.MemoryRoom.Dialogue
{
    // 대화 패널의 Core 연동. 화면 컨트롤러 패턴 그대로 — Core 처리기를 직접
    // 조작하지 않고, 눌림을 처리기 호출로 옮기고 이벤트를 듣고 다시 그린다.
    //
    // ClueSelection 줄에서는 텍스트 선택지 대신 손에 든 단서 목록을 띄워 고르게
    // 한다. 실제로 얼마나 맞는지(태그 등급)는 DialogueProgressor가 판정한다.
    // 이 줄에서 단서 하나를 골라 그 자리에서 기억을 추출할 수도 있다(히로민
    // 소비) — 추출해도 손에 남아 그대로 답으로 낼 수 있다.
    public sealed class DialoguePanelController : IDisposable
    {
        private readonly IDialoguePanelView _view;
        private readonly DialogueProgressor _progressor;
        private readonly ExtractionProcessor _extraction;
        private readonly IExtractedMemoryStore _memories;
        private readonly IEventBus _eventBus;
        private readonly IDisposable[] _subscriptions;

        // 대화가 끝난 방을 떠나는 "다음으로" 버튼의 식별자. 저작 데이터에는 없는
        // 예약 값이라 실제 선택지와 겹치지 않는다. 나중에 이 자리는 레버가 된다.
        private static readonly ChoiceId AdvanceChoiceId = new ChoiceId("__room-advance__");

        // 런이 끝났다. 이 뒤로는 어떤 이벤트가 와도 패널을 다시 그리지 않는다.
        private bool _runEnded;

        // 이 방의 대화가 끝나 "다음으로" 버튼만 남은 상태. 다음 방이 시작되거나
        // 런이 끝나면 풀린다.
        private bool _awaitingAdvance;

        // 지금 진행 중인 방 — "다음으로"를 누를 때 RoomClearedEvent에 실어야 한다.
        private MemoryRoomId _currentRoomId;

        public DialoguePanelController(
            IDialoguePanelView view,
            DialogueProgressor progressor,
            ExtractionProcessor extraction,
            IExtractedMemoryStore memories,
            MemoryRoomId initialRoomId,
            IEventBus eventBus)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _progressor = progressor ?? throw new ArgumentNullException(nameof(progressor));
            _extraction = extraction ?? throw new ArgumentNullException(nameof(extraction));
            _memories = memories ?? throw new ArgumentNullException(nameof(memories));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));

            // 첫 RoomStartedEvent는 이 패널이 만들어지기 전(세션 조립 중)에
            // 지나갔으므로, 지금 진행 중인 방을 인자로 받아 둔다.
            _currentRoomId = initialRoomId;

            _view.ChoiceClicked += OnChoiceClicked;
            _view.ClueAnswerClicked += id => _progressor.SelectClue(id);
            _view.ExtractClueClicked += OnExtractClue;
            _view.SkipClueAnswerClicked += () => _progressor.SkipClueSelection();

            _subscriptions = new[]
            {
                eventBus.Subscribe<RoomStartedEvent>(OnRoomStarted),
                eventBus.Subscribe<DialogueLineEnteredEvent>(_ => Rerender()),
                eventBus.Subscribe<ChoiceSelectedEvent>(_ => Rerender()),
                // 대화가 끝나면 패널에 "다음으로" 버튼만 남긴다 — 방을 떠나는 것은
                // 자동이 아니라 플레이어가 그 버튼을 눌러야 한다.
                eventBus.Subscribe<DialogueEndedEvent>(_ => OnDialogueEnded()),
                // ClueSelection 줄에 머무는 동안 방에서 단서를 집거나(손에 들어옴)
                // 추출하면(손에서 나감) 답 목록이 바뀐다 — 그때 다시 그린다.
                eventBus.Subscribe<ClueCollectedEvent>(_ => RerenderIfClueSelection()),
                eventBus.Subscribe<ClueExtractedEvent>(_ => RerenderIfClueSelection()),
                // TEMP: 정식 결과 화면이 없어, 런이 끝났다는 것만 이 패널에 표시한다.
                //       (모은 색 요약 + 짧은 문구). 별도 엔딩 화면은 후속 작업.
                eventBus.Subscribe<RunCompletedEvent>(_ => ShowRunEnded()),
            };

            Rerender();
        }

        // TEMP 종료 표시. 정식 결과 화면(모은 기억·해금 요약 등)은 별도 작업이고,
        // 지금은 "여기서 런이 끝났다"만 알린다. 대화 패널을 그대로 재사용한다.
        private void ShowRunEnded()
        {
            _runEnded = true;
            _awaitingAdvance = false;
            _view.SetNotice(null);

            var red = 0;
            var green = 0;
            var blue = 0;
            foreach (var memory in _memories.All)
            {
                switch (memory.Color)
                {
                    case MemoryColor.Red: red++; break;
                    case MemoryColor.Green: green++; break;
                    default: blue++; break;
                }
            }

            var summary = $"모은 기억색  —  R {red}   G {green}   B {blue}";
            _view.SetLine(string.Empty, "기억이 여기서 끝난다.\n" + summary);
            _view.SetChoices(Array.Empty<KeyValuePair<ChoiceId, string>>());
        }

        private void OnRoomStarted(RoomStartedEvent e)
        {
            _currentRoomId = e.RoomId;
            _awaitingAdvance = false;
            Rerender();
        }

        // 이 방의 대화가 끝났다 — CurrentLine은 이제 null이다. 패널을 비우는
        // 대신 "다음으로" 버튼을 남긴다(Rerender가 _awaitingAdvance를 보고 그린다).
        private void OnDialogueEnded()
        {
            if (_runEnded)
                return;

            _awaitingAdvance = true;
            Rerender();
        }

        private void OnChoiceClicked(ChoiceId id)
        {
            if (id.Equals(AdvanceChoiceId))
            {
                if (!_awaitingAdvance)
                    return;

                _awaitingAdvance = false;
                _eventBus.Publish(new RoomClearedEvent(_currentRoomId));
                return;
            }

            _progressor.Select(id);
        }

        // ClueSelection 줄에서 단서 하나의 기억을 그 자리에서 추출한다. 성공하면
        // ClueExtractedEvent가 RerenderIfClueSelection을 불러 목록이 다시 그려지고
        // (추출 버튼이 사라진다), 실패하면 이유만 안내한다.
        private void OnExtractClue(ClueId clueId)
        {
            var result = _extraction.Extract(clueId);
            if (!result.Succeeded)
                _view.SetNotice(ExtractionFailureMessage(result.FailureReason));
        }

        private static string ExtractionFailureMessage(ExtractionFailureReason? reason)
        {
            switch (reason)
            {
                case ExtractionFailureReason.ResourceExhausted:
                    return "히로민이 모자라 지금은 추출할 수 없습니다.";
                case ExtractionFailureReason.AlreadyExtracted:
                    return "이미 이 단서의 기억을 추출했습니다.";
                default:
                    return "이 단서는 지금 추출할 수 없습니다.";
            }
        }

        // 단서 손 상태가 바뀌었을 때 — ClueSelection 줄에서만 다시 그린다.
        // 텍스트 선택지 줄에서는 안내 문구를 괜히 지우지 않는다.
        private void RerenderIfClueSelection()
        {
            if (!_runEnded && _progressor.CurrentLine != null
                && _progressor.CurrentLine.PromptKind == DialoguePromptKind.ClueSelection)
            {
                Rerender();
            }
        }

        private void Rerender()
        {
            if (_runEnded)
                return;

            _view.SetNotice(null);

            var line = _progressor.CurrentLine;
            if (line == null)
            {
                _view.SetLine(string.Empty, string.Empty);

                // 이 방의 대화가 끝났으면 방을 떠나는 버튼을 남기고, 아직
                // 시작 전이거나 방 사이면 비운다.
                _view.SetChoices(_awaitingAdvance
                    ? new[] { new KeyValuePair<ChoiceId, string>(AdvanceChoiceId, "다음으로") }
                    : Array.Empty<KeyValuePair<ChoiceId, string>>());
                return;
            }

            _view.SetLine(line.Speaker, line.AuthoredText);

            // ClueSelection 줄은 텍스트 선택지 대신 단서 목록을 그린다.
            if (line.PromptKind == DialoguePromptKind.ClueSelection)
            {
                _view.SetClueSelection(_progressor.SelectableClues());
                return;
            }

            var choices = new List<KeyValuePair<ChoiceId, string>>();
            foreach (var choice in _progressor.VisibleChoices())
                choices.Add(new KeyValuePair<ChoiceId, string>(choice.Id, choice.AuthoredText));

            _view.SetChoices(choices);
        }

        public void Dispose()
        {
            foreach (var subscription in _subscriptions)
                subscription.Dispose();
        }
    }
}
