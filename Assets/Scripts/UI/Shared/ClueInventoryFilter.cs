using System.Collections.Generic;
using GameName.Core.Clues;
using GameName.Core.Inventory;

namespace GameName.UI.Shared
{
    // 인벤토리 항목 중 단서만 골라낸다. 분석 대상은 단서뿐이고 앰플은 분석기
    // 자체가 다루지 않으므로, 화면이 애초에 앰플을 분석 선택지로 보여주지
    // 않기 위한 순수 필터다. public인 이유는 이 필터 하나만 별도 테스트에서
    // 직접 검증할 수 있게 하기 위함이다(VisualElement를 구성하지 않고도
    // "앰플이 목록에서 빠지는가"를 증명할 수 있다).
    public static class ClueInventoryFilter
    {
        public static IReadOnlyList<ClueInfo> OnlyClues(IReadOnlyList<IInventoryItem> items)
        {
            var result = new List<ClueInfo>();
            foreach (var item in items)
            {
                if (item is ClueInfo clue)
                    result.Add(clue);
            }
            return result;
        }
    }
}
