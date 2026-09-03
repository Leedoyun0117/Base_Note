using System;
using System.Collections.Generic;
using GameName.Core.Dialogue;
using GameName.Core.Events;
using GameName.Core.Memories;

namespace GameName.UI.MemoryRoom.Dialogue
{
    // 대화 패널의 Core 연동. 화면 컨트롤러 패턴 그대로 — Core 처리기를 직접
    // 조작하지 않고, 눌림을 처리기 호출로 옮기고 이벤트를 듣고 다시 그린다.
    //
    // 마스크 구간 클릭은 대화를 막지 않는다. 기억제가 없으면 안내 한 줄만 띄우고
    // 끝이다 — 기억제는 필수 열쇠가 아니라 문맥 추론을 돕는 보조이기 때문이다.
    public sealed class DialoguePanelController : IDisposable
    {
        private readonly IDialoguePanelView _view;
        private readonly DialogueProgressor _progressor;
        private readonly CensorUnlockProcessor _censorUnlock;
        private readonly IMemoryColorWallet _wallet;
        private readonly ICensorKeyColorMap _keyColors;
        private readonly IDisposable[] _subscriptions;

        // 확인 팝업이 떠 있는 동안 어느 키를 풀지 붙들어 둔다.
        private CensorKey? _pendingKey;

        // 런이 끝났다. 이 뒤로는 어떤 이벤트가 와도 패널을 다시 그리지 않는다.
        private bool _runEnded;

        public DialoguePanelController(
            IDialoguePanelView view,
            DialogueProgressor progressor,
            CensorUnlockProcessor censorUnlock,
            IMemoryColorWallet wallet,
            ICensorKeyColorMap keyColors,
            IEventBus eventBus)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _progressor = progressor ?? throw new ArgumentNullException(nameof(progressor));
            _censorUnlock = censorUnlock ?? throw new ArgumentNullException(nameof(censorUnlock));
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _keyColors = keyColors ?? throw new ArgumentNullException(nameof(keyColors));
            if (eventBus == null) throw new ArgumentNullException(nameof(eventBus));

            _view.MaskClicked += OnMaskClicked;
            _view.ChoiceClicked += id => _progressor.Select(id);
            _view.ClueAnswerClicked += id => _progressor.SelectClue(id);
            _view.SkipClueAnswerClicked += () => _progressor.SkipClueSelection();
            _view.UnlockConfirmed += OnUnlockConfirmed;
            _view.UnlockCancelled += OnUnlockCancelled;

            _subscriptions = new[]
            {
                eventBus.Subscribe<RoomStartedEvent>(_ => Rerender()),
                eventBus.Subscribe<DialogueLineEnteredEvent>(_ => Rerender()),
                eventBus.Subscribe<ChoiceSelectedEvent>(_ => Rerender()),
                // 대화가 끝나면(마지막 방이면 뒤이어 RoomStartedEvent도 없다) 패널을 비운다.
                eventBus.Subscribe<DialogueEndedEvent>(_ => Rerender()),
                // 검열이 풀리면 이 줄에 있든 다른 줄에 있든 원문으로 돌아와야 한다.
                eventBus.Subscribe<CensorKeyUnlockedEvent>(_ => Rerender()),
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
            _pendingKey = null;
            _view.HideUnlockPrompt();
            _view.SetNotice(null);

            var summary =
                $"모은 기억색  —  R {_wallet.GetCount(MemoryColor.Red)}" +
                $"   G {_wallet.GetCount(MemoryColor.Green)}" +
                $"   B {_wallet.GetCount(MemoryColor.Blue)}";
            _view.SetLine(string.Empty, "기억이 여기서 끝난다.\n" + summary);
            _view.SetChoices(Array.Empty<KeyValuePair<ChoiceId, string>>());
        }

        // 단서 손 상태가 바뀌었을 때 — ClueSelection 줄에서만 다시 그린다.
        // 텍스트 선택지 줄에서는 안내 문구·확인 팝업을 괜히 지우지 않는다.
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

            _pendingKey = null;
            _view.HideUnlockPrompt();
            _view.SetNotice(null);

            var line = _progressor.CurrentLine;
            if (line == null)
            {
                // 대화가 끝났거나 방 사이다 — 패널을 비운다.
                _view.SetLine(string.Empty, string.Empty);
                _view.SetChoices(Array.Empty<KeyValuePair<ChoiceId, string>>());
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

        private void OnMaskClicked(CensorKey key)
        {
            if (!_keyColors.TryGetColor(key, out var color))
                return;

            if (_wallet.GetCount(color) <= 0)
            {
                _view.SetNotice("이 기억을 밝힐 기억제가 없습니다. (문맥으로도 진행할 수 있습니다)");
                return;
            }

            _pendingKey = key;
            _view.ShowUnlockPrompt("기억제 1개를 써서 이 구간을 밝힙니다.");
        }

        private void OnUnlockConfirmed()
        {
            if (!_pendingKey.HasValue)
                return;

            var result = _censorUnlock.Unlock(_pendingKey.Value);
            _pendingKey = null;

            if (!result.Succeeded)
            {
                _view.HideUnlockPrompt();
                _view.SetNotice("해금에 실패했습니다.");
                return;
            }

            // 성공(실제 소모 또는 멱등)이면 렌더가 원문을 드러낸다. 실제 소모였다면
            // CensorKeyUnlockedEvent가 이미 Rerender를 불렀겠지만, 멱등 성공까지
            // 덮도록 여기서 한 번 더 그린다.
            Rerender();
        }

        private void OnUnlockCancelled()
        {
            _pendingKey = null;
            _view.HideUnlockPrompt();
        }

        public void Dispose()
        {
            foreach (var subscription in _subscriptions)
                subscription.Dispose();
        }
    }
}
