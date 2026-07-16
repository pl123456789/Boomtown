using UnityEngine;

namespace Boomtown.WorldGeneration
{
    public sealed class BoomtownGeologyData : ScriptableObject
    {
        [Header("Generated Map Identity")]
        public string mapName;
        public int historicalYear;
        public string worldSeed;

        [Header("Grid")]
        public int resolutionX;
        public int resolutionZ;
        public Vector3 worldOrigin;
        public Vector3 worldSize;

        [Header("Hidden Geological Layers")]
        public float[] bedrockHardness;
        public float[] quartzPotential;
        public float[] placerConcentration;

        public float SamplePlacer(Vector3 worldPosition)
        {
            if (placerConcentration == null ||
                placerConcentration.Length == 0 ||
                resolutionX <= 1 ||
                resolutionZ <= 1)
            {
                return 0f;
            }

            float nx = Mathf.InverseLerp(
                worldOrigin.x,
                worldOrigin.x + worldSize.x,
                worldPosition.x);

            float nz = Mathf.InverseLerp(
                worldOrigin.z,
                worldOrigin.z + worldSize.z,
                worldPosition.z);

            int x = Mathf.Clamp(
                Mathf.RoundToInt(nx * (resolutionX - 1)),
                0,
                resolutionX - 1);

            int z = Mathf.Clamp(
                Mathf.RoundToInt(nz * (resolutionZ - 1)),
                0,
                resolutionZ - 1);

            return placerConcentration[z * resolutionX + x];
        }
    }
}