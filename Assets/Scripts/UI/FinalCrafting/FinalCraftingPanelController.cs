using System;
using System.Collections.Generic;
using GameName.Core.Ampoules;
using GameName.Core.Emotions;
using GameName.Core.FinalCrafting;
using GameName.Core.MemoryRooms;
using GameName.Core.Validation;
using GameName.UI.Perfumery;

namespace GameName.UI.FinalCrafting
{
    // 최종 조향 화면의 중앙(조향) 패널 컨트롤러.
    //
    // View는 조향실의 PerfumeryCompositionPanelView를 그대로 재사용한다 — 새로
    // 만들지 않는다. 다만 그 View가 원래 뜻하던 "대기열에 담기 -> 나중에 한꺼번에
    // 제작"이라는 흐름은 여기서는 성립하지 않는다: 이 화면에서 "담기" 버튼을
    // 누르는 순간이 곧 그 방의 최종 향을 확정(덮어쓰기)하는 행위이고, 되돌릴
    // 수 없다. 그래서 대기열 목록은 "쌓여 가는 목록"이 아니라 "지금 이 방에
    // 확정되어 있는 최종 향 하나(있다면)"만 보여주는 용도로 재해석해서 쓴다.
    // "대기열 일괄 제작" 버튼은 이 화면에서 쓰이지 않으므로 항상 비활성 상태로
    // 둔다 — View/UXML을 새로 만들지 않고 재사용하는 데 따르는 자연스러운
    // 제약이다.
    //
    // 정신력 비용을 알리지 않는다 — FinalCraftingProcessor 자체가
    // IMentalityGauge를 참조하지 않으므로 여기도 그 사실을 그대로 반영해
    // "정신력 소모 없음"이라는 문구만 안내한다.
    public sealed class FinalCraftingPanelController : IDisposable
    {
        private static readonly EmotionType[] AllEmotions =
        {
            EmotionType.Joy, EmotionType.Love, EmotionType.Anger, EmotionType.Sadness, EmotionType.Fear
        };

        private readonly PerfumeryCompositionPanelView _view;
        private readonly IScentCompositionValidator _compositionValidator;
        private readonly FinalCraftingProcessor _processor;
        private readonly IFinalCraftingBoard _board;
        private readonly Dictionary<EmotionType, int> _supportingIntensities = new Dictionary<EmotionType, int>();

        private EmotionType? _baseEmotion;
        private MemoryRoomId? _targetRoomId;
        private int? _requiredTotal;
        private Scent _currentScent;

        // 방이 바뀌거나 이 방의 최종 향이 확정될 때마다 알린다 — 제출 패널이
        // "모든 방이 채워졌는가"를 다시 확인할 수 있게 한다.
        public event Action RoomFinalized;

        public FinalCraftingPanelController(
            PerfumeryCompositionPanelView view,
            IScentCompositionValidator compositionValidator,
            FinalCraftingProcessor processor,
            IFinalCraftingBoard board)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _compositionValidator =
                compositionValidator ?? throw new ArgumentNullException(nameof(compositionValidator));
            _processor = processor ?? throw new ArgumentNullException(nameof(processor));
            _board = board ?? throw new ArgumentNullException(nameof(board));

            foreach (var emotion in AllEmotions)
                _supportingIntensities[emotion] = 0;

            _view.BaseEmotionSelected += OnBaseEmotionSelected;
            _view.SupportingIntensityStepRequested += OnSupportingIntensityStepRequested;
            _view.AddToQueueRequested += OnConfirmRequested;

            _view.SetCraftCostNotice(
                "정신력 소모 없음 · \"대기열에 담기\"를 누르면 이 방의 최종 향으로 즉시 확정(덮어쓰기)되며 되돌릴 수 없습니다.");
            _view.SetCraftQueueButtonEnabled(false);
            _view.SetSupportingIntensities(_supportingIntensities);
            RefreshConfirmedDisplay();
            Revalidate();
        }

        public void SetTargetRoom(MemoryRoomId roomId, int requiredTotal)
        {
            _targetRoomId = roomId;
            _requiredTotal = requiredTotal;
            RefreshConfirmedDisplay();
            Revalidate();
        }

        private void OnBaseEmotionSelected(EmotionType emotion)
        {
            _baseEmotion = emotion;
            _view.SetBaseEmotion(emotion);
            Revalidate();
        }

        private void OnSupportingIntensityStepRequested(EmotionType emotion, int delta)
        {
            var newValue = _supportingIntensities[emotion] + delta;
            if (newValue < 0)
                return;

            _supportingIntensities[emotion] = newValue;
            _view.SetSupportingIntensities(_supportingIntensities);
            Revalidate();
        }

        private void OnConfirmRequested()
        {
            if (_currentScent == null || _targetRoomId == null)
                return;

            var result = _processor.Craft(_targetRoomId.Value, _currentScent);
            if (!result.Succeeded)
            {
                _view.SetCraftResultMessage(DescribeFailure(result.FailureReason.Value));
                return;
            }

            _view.SetCraftResultMessage(null);
            ResetDraftComposition();
            RefreshConfirmedDisplay();
            RoomFinalized?.Invoke();
        }

        private void RefreshConfirmedDisplay()
        {
            if (_targetRoomId.HasValue && _board.TryGet(_targetRoomId.Value, out var confirmedScent))
                _view.SetQueue(new[] { new AmpouleCraftingRequest(_targetRoomId.Value, confirmedScent) });
            else
                _view.SetQueue(Array.Empty<AmpouleCraftingRequest>());
        }

        private void ResetDraftComposition()
        {
            _baseEmotion = null;
            foreach (var emotion in AllEmotions)
                _supportingIntensities[emotion] = 0;

            _view.SetBaseEmotion(null);
            _view.SetSupportingIntensities(_supportingIntensities);
            Revalidate();
        }

        private void Revalidate()
        {
            var entries = new List<EmotionBlendEntry>();
            foreach (var pair in _supportingIntensities)
            {
                if (pair.Value > 0)
                    entries.Add(new EmotionBlendEntry(pair.Key, pair.Value));
            }

            var blend = new EmotionBlend(entries);
            _view.SetTotalAndRequired(blend.Total, _requiredTotal);

            if (_baseEmotion == null || _requiredTotal == null)
            {
                _currentScent = null;
                _view.SetViolations(new[] { "방을 먼저 선택하고 바탕 감정을 고르세요." });
                _view.SetAddToQueueButtonEnabled(false);
                return;
            }

            var scent = new Scent(_baseEmotion.Value, blend);
            var result = _compositionValidator.Validate(scent, _requiredTotal.Value);

            _currentScent = result.IsValid ? scent : null;
            _view.SetViolations(result.IsValid ? Array.Empty<string>() : result.Violations);
            _view.SetAddToQueueButtonEnabled(_currentScent != null);
        }

        private static string DescribeFailure(FinalCraftingFailureReason reason)
        {
            switch (reason)
            {
                case FinalCraftingFailureReason.InvalidComposition: return "배합이 유효하지 않습니다.";
                default: return "확정에 실패했습니다.";
            }
        }

        public void Dispose()
        {
            _view.BaseEmotionSelected -= OnBaseEmotionSelected;
            _view.SupportingIntensityStepRequested -= OnSupportingIntensityStepRequested;
            _view.AddToQueueRequested -= OnConfirmRequested;
        }
    }
}
