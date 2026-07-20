using UnityEngine;

namespace Boomtown.WorldGeneration
{
    /// <summary>
    /// Runtime entry point for panning and later mining systems.
    ///
    /// This service never creates gold. It removes finite ounces from the
    /// generated BoomtownGeologyData asset.
    /// </summary>
    public static class BoomtownGoldExtractionService
    {
        public static GoldExtractionResult Pan(
            BoomtownGeologyData geology,
            Vector3 worldPosition,
            float materialProcessed = 1f,
            float recoveryEfficiency = 0.65f)
        {
            if (geology == null ||
                !geology.IsValid ||
                materialProcessed <= 0f)
            {
                return new GoldExtractionResult(
                    false,
                    0f,
                    0f,
                    0f,
                    0f);
            }

            float grade =
                geology.GetGrade(
                    worldPosition);

            float remainingBefore =
                geology.GetRemainingOunces(
                    worldPosition);

            if (grade <= 0f ||
                remainingBefore <= 0f)
            {
                return new GoldExtractionResult(
                    false,
                    0f,
                    0f,
                    remainingBefore,
                    grade);
            }

            recoveryEfficiency =
                Mathf.Clamp01(
                    recoveryEfficiency);

            // Prototype scale: a pan samples only a very small portion of a
            // deposit. Better equipment can later process more material.
            float requestedOunces =
                materialProcessed *
                Mathf.Lerp(
                    0.0002f,
                    0.018f,
                    grade) *
                recoveryEfficiency;

            float extracted =
                geology.Extract(
                    worldPosition,
                    requestedOunces);

            float remainingAfter =
                geology.GetRemainingOunces(
                    worldPosition);

            return new GoldExtractionResult(
                extracted > 0f,
                requestedOunces,
                extracted,
                remainingAfter,
                grade);
        }

        public static GoldExtractionResult Mine(
            BoomtownGeologyData geology,
            Vector3 worldPosition,
            float requestedOunces)
        {
            if (geology == null ||
                !geology.IsValid ||
                requestedOunces <= 0f)
            {
                return new GoldExtractionResult(
                    false,
                    requestedOunces,
                    0f,
                    0f,
                    0f);
            }

            float grade =
                geology.GetGrade(
                    worldPosition);

            float extracted =
                geology.Extract(
                    worldPosition,
                    requestedOunces);

            return new GoldExtractionResult(
                extracted > 0f,
                requestedOunces,
                extracted,
                geology.GetRemainingOunces(
                    worldPosition),
                grade);
        }
    }
}
