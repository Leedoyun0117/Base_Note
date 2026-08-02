using System;
using GameName.Core.Journal;

namespace GameName.UI.Journal
{
    // 기록지 화면 하나를 통째로 조립한다. IJournalReader만 알며(쓰기 인터페이스는
    // 전혀 몰라야 읽기 전용 화면이라는 사실이 타입으로도 보장된다), 화면이
    // 열릴 때마다 Refresh()로 다시 그린다.
    //
    // 실시간 구독으로 자동 갱신하지 않는 이유: 이 화면은 전체 화면을 덮는
    // 오버레이라 열려 있는 동안에는 다른 화면과 상호작용할 수 없다 — 즉 화면이
    // 떠 있는 동안 새 기록이 생길 수 없으므로, "열릴 때 한 번 새로 읽기"만으로
    // 항상 최신 상태를 보여줄 수 있다. 불필요한 이벤트 구독을 늘리지 않는다.
    public sealed class JournalScreenController
    {
        private readonly JournalScreenView _view;
        private readonly IJournalReader _reader;
        private JournalCategory _selectedCategory = JournalCategory.Dialogue;

        public JournalScreenController(JournalScreenView view, IJournalReader reader)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));

            _view.CategorySelected += OnCategorySelected;
        }

        public void Refresh()
        {
            _view.SetCommissionLabel(_reader.ActiveCommissionId);
            RenderSelectedCategory();
        }

        private void OnCategorySelected(JournalCategory category)
        {
            _selectedCategory = category;
            RenderSelectedCategory();
        }

        private void RenderSelectedCategory()
        {
            _view.SetSelectedCategory(_selectedCategory);

            var commissionId = _reader.ActiveCommissionId;
            if (commissionId == null)
            {
                _view.ShowNoActiveCommission();
                return;
            }

            switch (_selectedCategory)
            {
                case JournalCategory.Dialogue:
                    _view.SetDialogue(_reader.GetDialogue(commissionId.Value));
                    break;
                case JournalCategory.Analysis:
                    _view.SetAnalyses(_reader.GetAnalyses(commissionId.Value));
                    break;
                case JournalCategory.Ampoules:
                    _view.SetAmpoules(_reader.GetAmpoules(commissionId.Value));
                    break;
            }
        }

        public void Dispose()
        {
            _view.CategorySelected -= OnCategorySelected;
        }
    }
}
