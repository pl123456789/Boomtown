using UnityEngine;

namespace Boomtown.Gameplay.Prospecting
{
    [System.Serializable]
    public sealed class PanResultEvaluator
    {
        [SerializeField, Range(0f, 0.5f)]
        private float samplingVariation = 0.18f;

        [SerializeField, Range(0f, 0.1f)]
        private float nuggetChanceAtMaximumGrade = 0.025f;

        public PanOutcome Evaluate(float placerGrade)
        {
            float sampledGrade = Mathf.Clamp01(
                placerGrade +
                Random.Range(
                    -samplingVariation,
                    samplingVariation));

            float nuggetChance =
                nuggetChanceAtMaximumGrade *
                Mathf.Pow(sampledGrade, 3f);

            if (Random.value < nuggetChance)
            {
                float nuggetOunces =
                    Random.Range(0.015f, 0.08f) *
                    Mathf.Lerp(0.65f, 1.35f, sampledGrade);

                return new PanOutcome(
                    PanResult.Nugget,
                    nuggetOunces,
                    sampledGrade,
                    "Picker nugget!");
            }

            if (sampledGrade < 0.12f)
            {
                return new PanOutcome(
                    PanResult.Nothing,
                    0f,
                    sampledGrade,
                    "No colour");
            }

            if (sampledGrade < 0.28f)
            {
                return Create(
                    PanResult.Trace,
                    sampledGrade,
                    0.00015f,
                    0.0008f,
                    "Trace colour");
            }

            if (sampledGrade < 0.45f)
            {
                return Create(
                    PanResult.Fine,
                    sampledGrade,
                    0.0008f,
                    0.0035f,
                    "Fine gold");
            }

            if (sampledGrade < 0.62f)
            {
                return Create(
                    PanResult.Good,
                    sampledGrade,
                    0.0035f,
                    0.009f,
                    "Good colour");
            }

            if (sampledGrade < 0.80f)
            {
                return Create(
                    PanResult.Rich,
                    sampledGrade,
                    0.009f,
                    0.02f,
                    "Rich colour");
            }

            return Create(
                PanResult.Rich,
                sampledGrade,
                0.02f,
                0.045f,
                "Very rich colour");
        }

        private static PanOutcome Create(
            PanResult result,
            float grade,
            float minimum,
            float maximum,
            string description)
        {
            float ounces =
                Random.Range(minimum, maximum) *
                Mathf.Lerp(0.75f, 1.25f, grade);

            return new PanOutcome(
                result,
                ounces,
                grade,
                description);
        }
    }
}
