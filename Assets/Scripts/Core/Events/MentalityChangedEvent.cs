namespace GameName.Core.Events
{
    // 정신력이 소모되거나 회복될 때마다 발행된다.
    public readonly struct MentalityChangedEvent
    {
        public int PreviousValue { get; }
        public int CurrentValue { get; }

        public MentalityChangedEvent(int previousValue, int currentValue)
        {
            PreviousValue = previousValue;
            CurrentValue = currentValue;
        }

        public int Delta => CurrentValue - PreviousValue;
    }
}
