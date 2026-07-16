using UnityEditor;
using UnityEngine;

namespace Boomtown.WorldGeneration.Editor
{
    /// <summary>
    /// Generates hidden bedrock, quartz, and placer-gold layers.
    /// </summary>
    public static class BoomtownGeologyGenerator
    {
        private const string GeneratedFolder =
            "Assets/Boomtown/WorldGeneration/Generated";

        private const int GridResolution = 128;

        public static BoomtownGeologyData Generate(
            Terrain terrain,
            BoomtownMapDefinition mapDefinition)
        {
            if (terrain == null || terrain.terrainData == null)
            {
                Debug.LogError(
                    "[Boomtown Geology Generator] No valid Terrain supplied.");

                return null;
            }

            if (mapDefinition == null)
            {
                Debug.LogError(
                    "[Boomtown Geology Generator] No map definition supplied.");

                return null;
            }

            EnsureGeneratedFolderExists();

            string safeMapName =
                MakeSafeName(mapDefinition.mapName);

            string assetPath =
                $"{GeneratedFolder}/" +
                $"{safeMapName}_{mapDefinition.year}_Geology.asset";

            AssetDatabase.DeleteAsset(assetPath);

            BoomtownGeologyData geology =
                ScriptableObject.CreateInstance<BoomtownGeologyData>();

            geology.mapName = mapDefinition.mapName;
            geology.historicalYear = mapDefinition.year;
            geology.worldSeed = mapDefinition.worldSeed;
            geology.resolutionX = GridResolution;
            geology.resolutionZ = GridResolution;
            geology.worldOrigin = terrain.transform.position;
            geology.worldSize = terrain.terrainData.size;

            int sampleCount =
                GridResolution * GridResolution;

            geology.bedrockHardness =
                new float[sampleCount];

            geology.quartzPotential =
                new float[sampleCount];

            geology.placerConcentration =
                new float[sampleCount];

            int seed =
                StableHash(mapDefinition.worldSeed + "_GEOLOGY");

            TerrainData terrainData =
                terrain.terrainData;

            for (int z = 0; z < GridResolution; z++)
            {
                for (int x = 0; x < GridResolution; x++)
                {
                    float nx =
                        x / (float)(GridResolution - 1);

                    float nz =
                        z / (float)(GridResolution - 1);

                    int index =
                        z * GridResolution + x;

                    float normalizedHeight =
                        terrainData.GetInterpolatedHeight(nx, nz) /
                        terrainData.size.y;

                    float slope =
                        terrainData.GetSteepness(nx, nz);

                    float bedrockNoise =
                        Mathf.PerlinNoise(
                            nx * 4.5f + seed * 0.0001f,
                            nz * 4.5f + seed * 0.0001f);

                    float quartzNoise =
                        Mathf.PerlinNoise(
                            nx * 10f + 300f + seed * 0.00013f,
                            nz * 10f + 300f + seed * 0.00013f);

                    float faultBand =
                        1f - Mathf.Clamp01(
                            Mathf.Abs(
                                Mathf.Sin(
                                    nx * 13f +
                                    nz * 7f +
                                    seed * 0.0002f)));

                    float bedrockHardness =
                        Mathf.Clamp01(
                            bedrockNoise * 0.75f +
                            normalizedHeight * 0.25f);

                    float quartzPotential =
                        Mathf.Clamp01(
                            quartzNoise * 0.72f +
                            faultBand * 0.28f);

                    float riverCentreX =
                        0.5f +
                        Mathf.Sin(
                            nz * Mathf.PI * 2.1f) *
                        0.035f;

                    float riverDistance =
                        Mathf.Abs(nx - riverCentreX);

                    float nearRiver =
                        1f - Mathf.InverseLerp(
                            0.01f,
                            0.14f,
                            riverDistance);

                    float lowGround =
                        1f - Mathf.InverseLerp(
                            0.16f,
                            0.42f,
                            normalizedHeight);

                    float gentleSlope =
                        1f - Mathf.InverseLerp(
                            8f,
                            34f,
                            slope);

                    float placer =
                        quartzPotential *
                        nearRiver *
                        Mathf.Lerp(0.35f, 1f, lowGround) *
                        Mathf.Lerp(0.45f, 1f, gentleSlope);

                    geology.bedrockHardness[index] =
                        bedrockHardness;

                    geology.quartzPotential[index] =
                        quartzPotential;

                    geology.placerConcentration[index] =
                        Mathf.Clamp01(placer);
                }
            }

            AssetDatabase.CreateAsset(
                geology,
                assetPath);

            EditorUtility.SetDirty(geology);
            AssetDatabase.SaveAssets();

            Debug.Log(
                $"[Boomtown Geology Generator] Generated hidden geology for " +
                $"{mapDefinition.mapName}, {mapDefinition.year}. " +
                $"Grid: {GridResolution} x {GridResolution}.");

            return geology;
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

        private static string MakeSafeName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "Map";
            }

            return value.Trim().Replace(" ", string.Empty);
        }

        private static void EnsureGeneratedFolderExists()
        {
            const string root =
                "Assets/Boomtown/WorldGeneration";

            if (!AssetDatabase.IsValidFolder(root))
            {
                AssetDatabase.CreateFolder(
                    "Assets/Boomtown",
                    "WorldGeneration");
            }

            if (!AssetDatabase.IsValidFolder(GeneratedFolder))
            {
                AssetDatabase.CreateFolder(
                    root,
                    "Generated");
            }
        }
    }
}