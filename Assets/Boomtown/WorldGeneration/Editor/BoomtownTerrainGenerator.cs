using System;
using UnityEditor;
using UnityEngine;

namespace Boomtown.WorldGeneration.Editor
{
    public static class BoomtownTerrainGenerator
    {
        private const string GeneratedFolder =
            "Assets/Boomtown/WorldGeneration/Generated";

        private const int HeightmapResolution = 513;
        private const float TerrainWidth = 2000f;
        private const float TerrainLength = 2000f;
        private const float TerrainHeight = 220f;

        public static Terrain Generate(BoomtownMapDefinition mapDefinition)
        {
            if (mapDefinition == null)
            {
                Debug.LogError(
                    "[Boomtown Terrain Generator] No map definition selected.");
                return null;
            }

            EnsureGeneratedFolderExists();
            BoomtownWorldHierarchy.Rebuild(mapDefinition);

            string safeMapName = MakeSafeFileName(mapDefinition.mapName);
            string terrainDataPath =
                $"{GeneratedFolder}/{safeMapName}_{mapDefinition.year}_TerrainData.asset";

            AssetDatabase.DeleteAsset(terrainDataPath);

            TerrainData terrainData = new TerrainData
            {
                heightmapResolution = HeightmapResolution,
                alphamapResolution = 512,
                size = new Vector3(
                    TerrainWidth,
                    TerrainHeight,
                    TerrainLength)
            };

            terrainData.SetHeights(
                0,
                0,
                CreateHeightmap(mapDefinition.worldSeed));

            AssetDatabase.CreateAsset(terrainData, terrainDataPath);
            AssetDatabase.SaveAssets();

            GameObject terrainObject =
                Terrain.CreateTerrainGameObject(terrainData);

            terrainObject.name = $"GeneratedTerrain_{safeMapName}";
            terrainObject.transform.position =
                new Vector3(
                    -TerrainWidth * 0.5f,
                    0f,
                    -TerrainLength * 0.5f);

            terrainObject.transform.SetParent(
                BoomtownWorldHierarchy.GetTerrainContainer(),
                true);

            Terrain terrain = terrainObject.GetComponent<Terrain>();

            terrain.drawInstanced = true;
            terrain.heightmapPixelError = 8f;
            terrain.basemapDistance = 1000f;
            terrain.treeDistance = 800f;
            terrain.treeBillboardDistance = 250f;
            terrain.treeCrossFadeLength = 30f;
            terrain.treeMaximumFullLODCount = 200;

            RiverData riverData = null;

            if (mapDefinition.generateRivers)
            {
                BoomtownRiverGenerator.Generate(terrain, mapDefinition);
                riverData = LoadGeneratedRiverData(mapDefinition);
            }

            BoomtownTerrainPainter.Paint(terrain, riverData);

            if (mapDefinition.generateForests)
            {
                BoomtownForestGenerator.Generate(terrain, mapDefinition);
            }

            BoomtownWorldHierarchy.GetSettlementsContainer();
            BoomtownWorldSpawnGenerator.Generate(terrain, mapDefinition);

            Selection.activeGameObject = terrainObject;

            EditorUtility.SetDirty(terrainData);
            AssetDatabase.SaveAssets();

            Debug.Log(
                $"[Boomtown Terrain Generator] Generated world modules for " +
                $"{mapDefinition.mapName}, {mapDefinition.region}, " +
                $"{mapDefinition.year} using seed {mapDefinition.worldSeed}.");

            return terrain;
        }

        private static RiverData LoadGeneratedRiverData(
            BoomtownMapDefinition mapDefinition)
        {
            string safeMapName = MakeSafeFileName(mapDefinition.mapName);
            string riverDataPath =
                $"{GeneratedFolder}/{safeMapName}_{mapDefinition.year}_RiverData.asset";

            RiverData riverData =
                AssetDatabase.LoadAssetAtPath<RiverData>(riverDataPath);

            if (riverData == null)
            {
                Debug.LogWarning(
                    "[Boomtown Terrain Generator] RiverData was not found at " +
                    riverDataPath +
                    ". Terrain will use normal slope/elevation painting.");
            }

            return riverData;
        }

        private static float[,] CreateHeightmap(string seedText)
        {
            float[,] heights =
                new float[HeightmapResolution, HeightmapResolution];

            int seed = StableHash(seedText);
            System.Random random = new System.Random(seed);

            float offsetX = random.Next(-100000, 100000);
            float offsetY = random.Next(-100000, 100000);

            for (int y = 0; y < HeightmapResolution; y++)
            {
                for (int x = 0; x < HeightmapResolution; x++)
                {
                    float normalizedX =
                        x / (float)(HeightmapResolution - 1);

                    float normalizedY =
                        y / (float)(HeightmapResolution - 1);

                    float broadNoise =
                        Mathf.PerlinNoise(
                            offsetX + normalizedX * 2.1f,
                            offsetY + normalizedY * 2.1f);

                    float detailNoise =
                        Mathf.PerlinNoise(
                            offsetX + 500f + normalizedX * 7.5f,
                            offsetY + 500f + normalizedY * 7.5f);

                    float distanceFromValley =
                        Mathf.Abs(normalizedX - 0.5f) * 2f;

                    float valleyWalls =
                        Mathf.Pow(distanceFromValley, 1.7f);

                    float finalHeight =
                        0.025f +
                        broadNoise * 0.10f +
                        detailNoise * 0.025f +
                        valleyWalls * 0.30f;

                    heights[y, x] = Mathf.Clamp01(finalHeight);
                }
            }

            return heights;
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

        private static string MakeSafeFileName(string value)
        {
            string result =
                string.IsNullOrWhiteSpace(value)
                    ? "Map"
                    : value.Trim();

            foreach (char invalidCharacter
                     in System.IO.Path.GetInvalidFileNameChars())
            {
                result = result.Replace(
                    invalidCharacter.ToString(),
                    string.Empty);
            }

            return result.Replace(" ", string.Empty);
        }

        private static void EnsureGeneratedFolderExists()
        {
            const string root = "Assets/Boomtown/WorldGeneration";

            if (!AssetDatabase.IsValidFolder(root))
            {
                AssetDatabase.CreateFolder(
                    "Assets/Boomtown",
                    "WorldGeneration");
            }

            if (!AssetDatabase.IsValidFolder(GeneratedFolder))
            {
                AssetDatabase.CreateFolder(root, "Generated");
            }
        }
    }
}
