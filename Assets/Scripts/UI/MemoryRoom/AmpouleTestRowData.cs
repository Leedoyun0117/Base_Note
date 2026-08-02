using GameName.Core.Ampoules;

namespace GameName.UI.MemoryRoom
{
    // 시향 패널이 앰플 한 줄을 그리는 데 필요한 정보만 모은 UI 전용 DTO.
    public readonly struct AmpouleTestRowData
    {
        public Ampoule Ampoule { get; }
        public bool Eligible { get; }
        public bool IsArmed { get; }

        public AmpouleTestRowData(Ampoule ampoule, bool eligible, bool isArmed)
        {
            Ampoule = ampoule;
            Eligible = eligible;
            IsArmed = isArmed;
        }
    }
}
