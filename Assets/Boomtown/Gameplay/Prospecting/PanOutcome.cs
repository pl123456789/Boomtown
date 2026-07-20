namespace Boomtown.Gameplay.Prospecting
{
    public readonly struct PanOutcome
    {
        public PanOutcome(
            PanResult result,
            float goldOunces,
            float sampledGrade,
            string description)
        {
            Result = result;
            GoldOunces = goldOunces;
            SampledGrade = sampledGrade;
            Description = description;
        }

        public PanResult Result { get; }
        public float GoldOunces { get; }
        public float SampledGrade { get; }
        public string Description { get; }
    }
}
