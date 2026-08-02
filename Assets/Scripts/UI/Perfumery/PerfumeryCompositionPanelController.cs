using System;
using System.Collections.Generic;
using GameName.Core.Ampoules;
using GameName.Core.Emotions;
using GameName.Core.Mentality;
using GameName.Core.MemoryRooms;
using GameName.Core.Validation;

namespace GameName.UI.Perfumery
{
    // 조향 화면에서 "지금 만들고 있는 배합"이라는 임시 상태(바탕 감정, 보조
    // 감정별 세기)와 "제작 대기열"을 들고 있다. 유효성 판단은 전부
    // IScentCompositionValidator에, 대기열에 더 담을 수 있는지는 전부
    // IAmpouleStorage.CanAccept에 위임한다 — 이 컨트롤러는 그 결론을 화면에
    // 옮기고 대기열이라는 목록 자체만 관리한다.
    public sealed class PerfumeryCompositionPanelController : IDisposable
    {
        private static readonly EmotionType[] AllEmotions =
        {
            EmotionType.Joy, EmotionType.Love, EmotionType.Anger, EmotionType.Sadness, EmotionType.Fear
        };

        private readonly PerfumeryCompositionPanelView _view;
        private readonly IScentCompositionValidator _compositionValidator;
        private readonly IAmpouleStorage _storage;
        private readonly IAmpouleCraftingQueue _queue;
        private readonly Dictionary<EmotionType, int> _supportingIntensities = new Dictionary<EmotionType, int>();

        private EmotionType? _baseEmotion;
        private MemoryRoomId? _targetRoomId;
        private int? _requiredTotal;
        private Scent _currentScent;

        public IReadOnlyList<AmpouleCraftingRequest> Queue => _queue.Items;

        // 대기열 일괄 제작은 화면 컨트롤러가 AmpouleCraftingProcessor를 호출해
        // 처리한다 — 여기서는 "제작해 달라"는 요청만 밖으로 알린다.
        public event Action CraftQueueRequested;

        public PerfumeryCompositionPanelController(
            PerfumeryCompositionPanelView view,
            IScentCompositionValidator compositionValidator,
            IAmpouleStorage storage,
            IAmpouleCraftingQueue queue,
            IMentalityCostSettings costSettings)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _compositionValidator =
                compositionValidator ?? throw new ArgumentNullException(nameof(compositionValidator));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _queue = queue ?? throw new ArgumentNullException(nameof(queue));
            if (costSettings == null) throw new ArgumentNullException(nameof(costSettings));

            foreach (var emotion in AllEmotions)
                _supportingIntensities[emotion] = 0;

            _view.BaseEmotionSelected += OnBaseEmotionSelected;
            _view.SupportingIntensityStepRequested += OnSupportingIntensityStepRequested;
            _view.AddToQueueRequested += OnAddToQueueRequested;
            _view.QueueItemRemoveRequested += OnQueueItemRemoveRequested;
            _view.CraftQueueRequested += OnCraftQueueRequested;

            // 정신력 8이 개수와 무관하게 1회만 소모된다는 사실을 플레이어가
            // 몰라서는 판단할 수 없으므로 화면에 명시한다. 값은 IMentalityCostSettings
            // 에서 그대로 읽어와 매직 넘버로 박아두지 않는다.
            _view.SetCraftCostNotice(
                $"제작 비용: 정신력 {costSettings.AmpouleCraftingCost} (몇 개를 만들든 1회만 소모)");
            _view.SetSupportingIntensities(_supportingIntensities);
            _view.SetQueue(_queue.Items);
            RefreshCraftQueueButtonEnabled();
            Revalidate();
        }

        public void SetTargetRoom(MemoryRoomId roomId, int requiredTotal)
        {
            _targetRoomId = roomId;
            _requiredTotal = requiredTotal;
            Revalidate();
        }

        public void ClearQueue()
        {
            _queue.Clear();
            _view.SetQueue(_queue.Items);
            RefreshAddToQueueButtonEnabled();
            RefreshCraftQueueButtonEnabled();
        }

        public void ShowCraftFailure(AmpouleCraftingFailureReason reason) =>
            _view.SetCraftResultMessage(DescribeFailure(reason));

        public void ClearCraftResultMessage() => _view.SetCraftResultMessage(null);

        private void OnBaseEmotionSelected(EmotionType emotion)
        {
            _baseEmotion = emotion;
            _view.SetBaseEmotion(emotion);
            Revalidate();
        }

        private void OnSupportingIntensityStepRequested(EmotionType emotion, int delta)
        {
            // 0 아래로 못 내려가게만 막는다 — 게임 규칙 판단이 아니라 "세기는
            // 음수일 수 없다"는 스테퍼 입력 자체의 물리적 한계다.
            var newValue = _supportingIntensities[emotion] + delta;
            if (newValue < 0)
                return;

            _supportingIntensities[emotion] = newValue;
            _view.SetSupportingIntensities(_supportingIntensities);
            Revalidate();
        }

        private void OnAddToQueueRequested()
        {
            if (_currentScent == null || _targetRoomId == null)
                return;
            if (!_storage.CanAccept(_queue.Items.Count + 1))
                return;

            _queue.Add(new AmpouleCraftingRequest(_targetRoomId.Value, _currentScent));
            _view.SetQueue(_queue.Items);

            ResetDraftComposition();
            RefreshCraftQueueButtonEnabled();
        }

        private void OnQueueItemRemoveRequested(int index)
        {
            if (index < 0 || index >= _queue.Items.Count)
                return;

            _queue.RemoveAt(index);
            _view.SetQueue(_queue.Items);
            RefreshAddToQueueButtonEnabled();
            RefreshCraftQueueButtonEnabled();
        }

        private void OnCraftQueueRequested() => CraftQueueRequested?.Invoke();

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
                _view.SetViolations(new[] { "목표 방과 바탕 감정을 먼저 선택하세요." });
                RefreshAddToQueueButtonEnabled();
                return;
            }

            var scent = new Scent(_baseEmotion.Value, blend);
            var result = _compositionValidator.Validate(scent, _requiredTotal.Value);

            _currentScent = result.IsValid ? scent : null;
            _view.SetViolations(result.IsValid ? Array.Empty<string>() : result.Violations);
            RefreshAddToQueueButtonEnabled();
        }

        private void RefreshAddToQueueButtonEnabled()
        {
            var canQueue = _currentScent != null && _storage.CanAccept(_queue.Items.Count + 1);
            _view.SetAddToQueueButtonEnabled(canQueue);
        }

        private void RefreshCraftQueueButtonEnabled() => _view.SetCraftQueueButtonEnabled(_queue.Items.Count > 0);

        private static string DescribeFailure(AmpouleCraftingFailureReason reason)
        {
            switch (reason)
            {
                case AmpouleCraftingFailureReason.NotInPerfumeryRoom: return "조향실에서만 만들 수 있습니다.";
                case AmpouleCraftingFailureReason.InvalidComposition: return "배합이 유효하지 않습니다.";
                case AmpouleCraftingFailureReason.StorageFull: return "보관함에 자리가 없습니다.";
                case AmpouleCraftingFailureReason.InsufficientMentality: return "정신력이 부족합니다.";
                default: return "제작에 실패했습니다.";
            }
        }

        public void Dispose()
        {
            _view.BaseEmotionSelected -= OnBaseEmotionSelected;
            _view.SupportingIntensityStepRequested -= OnSupportingIntensityStepRequested;
            _view.AddToQueueRequested -= OnAddToQueueRequested;
            _view.QueueItemRemoveRequested -= OnQueueItemRemoveRequested;
            _view.CraftQueueRequested -= OnCraftQueueRequested;
        }
    }
}
