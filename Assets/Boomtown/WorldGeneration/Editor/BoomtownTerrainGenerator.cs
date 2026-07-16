using System;
using UnityEditor;
using UnityEngine;

namespace Boomtown.WorldGeneration.Editor
{
    /// <summary>
    /// Creates the playable Boomtown terrain and runs world-generation modules
    /// in dependency order.
    ///
    /// Current order:
    /// 1. Terrain relief
    /// 2. River carving, mesh, and RiverData
    /// 3. RiverData-driven terrain painting
    /// 4. Forest generation
    /// 5. Player, camera, prospecting references, and NavMesh
    /// </summary>
    public static class BoomtownTerrainGenerator
    {
        private const string GeneratedFolder =
            "Assets/Boomtown/WorldGeneration/Generated";

        private const int HeightmapResolution = 513;
        private const float TerrainWidth = 2000f;
        private const float TerrainLength = 2000f;
        private const float TerrainHeight = 220f;

        public static Terrain Generate(
            BoomtownMapDefinition mapDefinition)
        {
            if (mapDefinition == null)
            {
                Debug.LogError(
                    "[Boomtown Terrain Generator] " +
                    "No map definition selected.");

                return null;
            }

            EnsureGeneratedFolderExists();

            string safeMapName =
                MakeSafeFileName(mapDefinition.mapName);

            string terrainDataPath =
                $"{GeneratedFolder}/" +
                $"{safeMapName}_{mapDefinition.year}_" +
                $"TerrainData.asset";

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

            AssetDatabase.CreateAsset(
                terrainData,
                terrainDataPath);

            AssetDatabase.SaveAssets();

            GameObject oldTerrain =
                GameObject.Find(
                    $"GeneratedTerrain_{safeMapName}");

            if (oldTerrain != null)
            {
                UnityEngine.Object.DestroyImmediate(
                    oldTerrain);
            }

            GameObject terrainObject =
                Terrain.CreateTerrainGameObject(
                    terrainData);

            terrainObject.name =
                $"GeneratedTerrain_{safeMapName}";

            terrainObject.transform.position =
                new Vector3(
                    -TerrainWidth * 0.5f,
                    0f,
                    -TerrainLength * 0.5f);

            Terrain terrain =
                terrainObject.GetComponent<Terrain>();

            terrain.drawInstanced = true;
            terrain.heightmapPixelError = 8f;
            terrain.basemapDistance = 1000f;
            terrain.treeDistance = 800f;
            terrain.treeBillboardDistance = 250f;
            terrain.treeCrossFadeLength = 30f;
            terrain.treeMaximumFullLODCount = 200;

            RiverData riverData = null;

            // MODULE 1: Carve and generate the river first.
            if (mapDefinition.generateRivers)
            {
                BoomtownRiverGenerator.Generate(
                    terrain,
                    mapDefinition);

                riverData =
                    LoadGeneratedRiverData(
                        mapDefinition);
            }

            // MODULE 2: Paint after carving so bank textures match RiverData.
            BoomtownTerrainPainter.Paint(
                terrain,
                riverData);

            // MODULE 3: Seed the deterministic prototype forest.
            if (mapDefinition.generateForests)
            {
                BoomtownForestGenerator.Generate(
                    terrain,
                    mapDefinition);
            }

            // MODULE 4: Position gameplay objects and rebuild navigation.
            BoomtownWorldSpawnGenerator.Generate(
                terrain,
                mapDefinition);

            Selection.activeGameObject =
                terrainObject;

            EditorUtility.SetDirty(terrainData);
            AssetDatabase.SaveAssets();

            Debug.Log(
                $"[Boomtown Terrain Generator] Generated world modules for " +
                $"{mapDefinition.mapName}, {mapDefinition.region}, " +
                $"{mapDefinition.year} using seed " +
                $"{mapDefinition.worldSeed}.");

            return terrain;
        }

        private static RiverData LoadGeneratedRiverData(
            BoomtownMapDefinition mapDefinition)
        {
            string safeMapName =
                MakeSafeFileName(
                    mapDefinition.mapName);

            string riverDataPath =
                $"{GeneratedFolder}/" +
                $"{safeMapName}_{mapDefinition.year}_" +
                $"RiverData.asset";

            RiverData riverData =
                AssetDatabase.LoadAssetAtPath<RiverData>(
                    riverDataPath);

            if (riverData == null)
            {
                Debug.LogWarning(
                    "[Boomtown Terrain Generator] RiverData was not found at " +
                    riverDataPath +
                    ". Terrain will use normal slope/elevation painting.");
            }

            return riverData;
        }

        private static float[,] CreateHeightmap(
            string seedText)
        {
            float[,] heights =
                new float[
                    HeightmapResolution,
                    HeightmapResolution];

            int seed = StableHash(seedText);

            System.Random random =
                new System.Random(seed);

            float offsetX =
                random.Next(-100000, 100000);

            float offsetY =
                random.Next(-100000, 100000);

            for (int y = 0;
                 y < HeightmapResolution;
                 y++)
            {
                for (int x = 0;
                     x < HeightmapResolution;
                     x++)
                {
                    float normalizedX =
                        x /
                        (float)(HeightmapResolution - 1);

                    float normalizedY =
                        y /
                        (float)(HeightmapResolution - 1);

                    float broadNoise =
                        Mathf.PerlinNoise(
                            offsetX +
                            normalizedX * 2.1f,
                            offsetY +
                            normalizedY * 2.1f);

                    float detailNoise =
                        Mathf.PerlinNoise(
                            offsetX + 500f +
                            normalizedX * 7.5f,
                            offsetY + 500f +
                            normalizedY * 7.5f);

                    float distanceFromValley =
                        Mathf.Abs(
                            normalizedX - 0.5f) *
                        2f;

                    float valleyWalls =
                        Mathf.Pow(
                            distanceFromValley,
                            1.7f);

                    float finalHeight =
                        0.025f +
                        broadNoise * 0.10f +
                        detailNoise * 0.025f +
                        valleyWalls * 0.30f;

                    heights[y, x] =
                        Mathf.Clamp01(finalHeight);
                }
            }

            return heights;
        }

        private static int StableHash(string text)
        {
            unchecked
            {
                int hash = 23;

                foreach (char character
                         in text ?? string.Empty)
                {
                    hash =
                        hash * 31 +
                        character;
                }

                return hash;
            }
        }

        private static string MakeSafeFileName(
            string value)
        {
            string result =
                string.IsNullOrWhiteSpace(value)
                    ? "Map"
                    : value.Trim();

            foreach (char invalidCharacter
                     in System.IO.Path
                         .GetInvalidFileNameChars())
            {
                result =
                    result.Replace(
                        invalidCharacter.ToString(),
                        string.Empty);
            }

            return result.Replace(
                " ",
                string.Empty);
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

            if (!AssetDatabase.IsValidFolder(
                    GeneratedFolder))
            {
                AssetDatabase.CreateFolder(
                    root,
                    "Generated");
            }
        }
    }
}