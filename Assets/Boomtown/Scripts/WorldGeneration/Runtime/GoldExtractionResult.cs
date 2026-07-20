using System;

namespace Boomtown.WorldGeneration
{
    [Serializable]
    public readonly struct GoldExtractionResult
    {
        public readonly bool foundGold;
        public readonly float requestedOunces;
        public readonly float extractedOunces;
        public readonly float remainingOunces;
        public readonly float grade;

        public GoldExtractionResult(
            bool foundGold,
            float requestedOunces,
            float extractedOunces,
            float remainingOunces,
            float grade)
        {
            this.foundGold = foundGold;
            this.requestedOunces = requestedOunces;
            this.extractedOunces = extractedOunces;
            this.remainingOunces = remainingOunces;
            this.grade = grade;
        }
    }
}
