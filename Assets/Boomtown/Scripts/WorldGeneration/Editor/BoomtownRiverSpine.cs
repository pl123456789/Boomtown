using UnityEngine;

namespace Boomtown.WorldGeneration.Editor
{
    /// <summary>
    /// Shared deterministic centreline used by terrain, river geometry,
    /// geology, forests, and spawn systems.
    /// </summary>
    public static class BoomtownRiverSpine
    {
        public static float GetNormalizedCentreX(
            float normalizedZ,
            string seedText)
        {
            int seed = StableHash((seedText ?? string.Empty) + "_RIVER_SPINE");

            float phaseA = Mathf.Abs(seed % 10000) * 0.00073f;
            float phaseB = Mathf.Abs((seed / 17) % 10000) * 0.00111f;
            float noiseOffset = Mathf.Abs((seed / 37) % 10000) * 0.0137f;

            // Amplitudes widened so the valley genuinely wanders across the
            // map instead of just wobbling near dead-centre -- the old
            // coefficients summed to well under the safety clamp below, so
            // the clamp was never actually being reached in practice.
            float broadBend =
                Mathf.Sin(
                    normalizedZ * Mathf.PI * 2.1f +
                    phaseA) *
                0.095f;

            float secondaryBend =
                Mathf.Sin(
                    normalizedZ * Mathf.PI * 5.3f +
                    phaseB) *
                0.032f;

            float naturalDrift =
                (Mathf.PerlinNoise(
                    noiseOffset,
                    normalizedZ * 2.2f + noiseOffset * 0.17f) - 0.5f) *
                0.06f;

            float longDrift =
                Mathf.Sin(
                    normalizedZ * Mathf.PI * 0.85f +
                    phaseB * 0.37f) *
                0.05f;

            return Mathf.Clamp(
                0.5f +
                broadBend +
                secondaryBend +
                naturalDrift +
                longDrift,
                0.20f,
                0.80f);
        }

        public static float GetWorldCentreX(
            Terrain terrain,
            float normalizedZ,
            string seedText)
        {
            return terrain.transform.position.x +
                   terrain.terrainData.size.x *
                   GetNormalizedCentreX(normalizedZ, seedText);
        }

        private static int StableHash(string text)
        {
            unchecked
            {
                int hash = 23;

                foreach (char character in text ?? string.Empty)
                {
                    hash = hash * 31 + character;
                }

                return hash;
            }
        }
    }
}
