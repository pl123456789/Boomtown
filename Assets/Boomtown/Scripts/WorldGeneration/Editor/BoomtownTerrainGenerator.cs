using System;
using UnityEditor;
using UnityEngine;

namespace Boomtown.WorldGeneration.Editor
{
    public static class BoomtownTerrainGenerator
    {
        private const string GeneratedFolder =
            "Assets/Boomtown/Scripts/WorldGeneration/Generated";

        private const int HeightmapResolution = 513;
        private const float TerrainWidth = 2000f;
        private const float TerrainLength = 2000f;
        private const float TerrainHeight = 520f;

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

            string safeMapName =
                MakeSafeFileName(mapDefinition.mapName);

            string terrainDataPath =
                $"{GeneratedFolder}/" +
                $"{safeMapName}_{mapDefinition.year}_TerrainData.asset";

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

            float[,] heightmap =
                CreateHeightmap(mapDefinition.worldSeed);

            terrainData.SetHeights(0, 0, heightmap);

            AssetDatabase.CreateAsset(
                terrainData,
                terrainDataPath);

            AssetDatabase.SaveAssets();

            GameObject terrainObject =
                Terrain.CreateTerrainGameObject(terrainData);

            terrainObject.name =
                $"GeneratedTerrain_{safeMapName}";

            terrainObject.transform.position =
                new Vector3(
                    -TerrainWidth * 0.5f,
                    0f,
                    -TerrainLength * 0.5f);

            terrainObject.transform.SetParent(
                BoomtownWorldHierarchy.GetTerrainContainer(),
                true);

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

            if (mapDefinition.generateRivers)
            {
                BoomtownRiverGenerator.Generate(
                    terrain,
                    mapDefinition);

                riverData =
                    LoadGeneratedRiverData(mapDefinition);
            }

            BoomtownTerrainPainter.Paint(
                terrain,
                riverData);

            if (mapDefinition.generateForests)
            {
                BoomtownForestGenerator.Generate(
                    terrain,
                    mapDefinition);
            }

            BoomtownWorldHierarchy
                .GetSettlementsContainer();

            BoomtownWorldSpawnGenerator.Generate(
                terrain,
                mapDefinition);

            Selection.activeGameObject =
                terrainObject;

            EditorUtility.SetDirty(terrainData);
            AssetDatabase.SaveAssets();

            Debug.Log(
                "[Boomtown Terrain Generator] Generated world modules for " +
                $"{mapDefinition.mapName}, " +
                $"{mapDefinition.region}, " +
                $"{mapDefinition.year} using seed " +
                $"{mapDefinition.worldSeed}.");

            return terrain;
        }

        private static RiverData LoadGeneratedRiverData(
            BoomtownMapDefinition mapDefinition)
        {
            string safeMapName =
                MakeSafeFileName(mapDefinition.mapName);

            string riverDataPath =
                $"{GeneratedFolder}/" +
                $"{safeMapName}_{mapDefinition.year}_RiverData.asset";

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

            int seed =
                StableHash(seedText);

            System.Random random =
                new System.Random(seed);

            TerrainGenerationContext context =
                new TerrainGenerationContext
                {
                    seedText = seedText,

                    offsetX =
                        random.Next(-100000, 100000),

                    offsetY =
                        random.Next(-100000, 100000),

                    ridgeOffsetX =
                        random.Next(-100000, 100000),

                    ridgeOffsetY =
                        random.Next(-100000, 100000),

                    detailOffsetX =
                        random.Next(-100000, 100000),

                    detailOffsetY =
                        random.Next(-100000, 100000)
                };

            GenerateBaseHeight(heights);
            GenerateBroadLandforms(heights, context);
            GenerateValleyWalls(heights, context);
            GenerateMountainRidges(heights, context);
            GenerateBrokenRidges(heights, context);
            GenerateTributaryGullies(heights, context);
            GenerateRockShelves(heights, context);
            ApplyNaturalErosion(heights, context, 5);
            GenerateTerrainDetail(heights, context);
            GenerateRiverTerraces(heights, context);
            ApplyEdgeMountains(heights);
            SmoothHeightmap(heights, 1);
            NormalizeHeightmap(heights);

            return heights;
        }

        private static void GenerateBaseHeight(
            float[,] heights)
        {
            for (int y = 0;
                 y < HeightmapResolution;
                 y++)
            {
                for (int x = 0;
                     x < HeightmapResolution;
                     x++)
                {
                    heights[y, x] = 0.02f;
                }
            }
        }

        private static void GenerateBroadLandforms(
            float[,] heights,
            TerrainGenerationContext context)
        {
            for (int y = 0;
                 y < HeightmapResolution;
                 y++)
            {
                for (int x = 0;
                     x < HeightmapResolution;
                     x++)
                {
                    float normalizedX =
                        GetNormalizedCoordinate(x);

                    float normalizedY =
                        GetNormalizedCoordinate(y);

                    float broadNoise =
                        Mathf.PerlinNoise(
                            context.offsetX +
                            normalizedX * 1.8f,

                            context.offsetY +
                            normalizedY * 1.8f);

                    broadNoise =
                        Mathf.SmoothStep(
                            0f,
                            1f,
                            broadNoise);

                    heights[y, x] +=
                        broadNoise * 0.09f;
                }
            }
        }

        private static void GenerateValleyWalls(
            float[,] heights,
            TerrainGenerationContext context)
        {
            for (int y = 0;
                 y < HeightmapResolution;
                 y++)
            {
                float normalizedY =
                    GetNormalizedCoordinate(y);

                float valleyCentre =
                    GetValleyCentre(
                        normalizedY,
                        context);

                for (int x = 0;
                     x < HeightmapResolution;
                     x++)
                {
                    float normalizedX =
                        GetNormalizedCoordinate(x);

                    float distanceFromValley =
                        Mathf.Abs(
                            normalizedX -
                            valleyCentre);

                    // Keep a narrow playable river floor, then rise quickly
                    // into steep Fraser Canyon walls.
                    float wallT =
                        Mathf.InverseLerp(
                            0.035f,
                            0.34f,
                            distanceFromValley);

                    wallT =
                        wallT * wallT *
                        (3f - 2f * wallT);

                    // Power below 1 makes the walls rise quickly from the
                    // narrow river floor instead of forming a broad basin.
                    float canyonWall =
                        Mathf.Pow(
                            wallT,
                            0.66f);

                    heights[y, x] +=
                        canyonWall * 0.47f;
                }
            }
        }

        private static void GenerateMountainRidges(
            float[,] heights,
            TerrainGenerationContext context)
        {
            for (int y = 0;
                 y < HeightmapResolution;
                 y++)
            {
                float normalizedY =
                    GetNormalizedCoordinate(y);

                float valleyCentre =
                    GetValleyCentre(
                        normalizedY,
                        context);

                for (int x = 0;
                     x < HeightmapResolution;
                     x++)
                {
                    float normalizedX =
                        GetNormalizedCoordinate(x);

                    float distanceFromValley =
                        Mathf.Abs(
                            normalizedX -
                            valleyCentre);

                    float mountainMask =
                        Mathf.InverseLerp(
                            0.075f,
                            0.42f,
                            distanceFromValley);

                    float ridgeNoise =
                        Mathf.PerlinNoise(
                            context.ridgeOffsetX +
                            normalizedX * 4.2f,

                            context.ridgeOffsetY +
                            normalizedY * 4.2f);

                    ridgeNoise =
                        Mathf.Abs(
                            ridgeNoise * 2f -
                            1f);

                    ridgeNoise =
                        1f - ridgeNoise;

                    ridgeNoise =
                        Mathf.Pow(
                            ridgeNoise,
                            2.2f);

                    heights[y, x] +=
                        ridgeNoise *
                        mountainMask *
                        0.145f;
                }
            }
        }


        private static void GenerateBrokenRidges(
            float[,] heights,
            TerrainGenerationContext context)
        {
            for (int y = 0;
                 y < HeightmapResolution;
                 y++)
            {
                float normalizedY =
                    GetNormalizedCoordinate(y);

                float valleyCentre =
                    GetValleyCentre(
                        normalizedY,
                        context);

                for (int x = 0;
                     x < HeightmapResolution;
                     x++)
                {
                    float normalizedX =
                        GetNormalizedCoordinate(x);

                    float distanceFromValley =
                        Mathf.Abs(
                            normalizedX -
                            valleyCentre);

                    float mountainMask =
                        Mathf.InverseLerp(
                            0.10f,
                            0.43f,
                            distanceFromValley);

                    if (mountainMask <= 0f)
                    {
                        continue;
                    }

                    float fractureNoise =
                        Mathf.PerlinNoise(
                            context.ridgeOffsetX +
                            1200f +
                            normalizedX * 9.5f,

                            context.ridgeOffsetY +
                            1200f +
                            normalizedY * 7.5f);

                    float ridgeBands =
                        Mathf.Abs(
                            Mathf.Sin(
                                normalizedX * 22f +
                                normalizedY * 11f +
                                fractureNoise * 4f));

                    float brokenRidge =
                        Mathf.Pow(
                            Mathf.Clamp01(
                                1f - ridgeBands),
                            2.6f);

                    float interruption =
                        Mathf.SmoothStep(
                            0f,
                            1f,
                            Mathf.PerlinNoise(
                                context.detailOffsetX +
                                1600f +
                                normalizedX * 5.5f,

                                context.detailOffsetY +
                                1600f +
                                normalizedY * 5.5f));

                    heights[y, x] +=
                        brokenRidge *
                        interruption *
                        mountainMask *
                        0.070f;
                }
            }
        }

        private static void GenerateTributaryGullies(
            float[,] heights,
            TerrainGenerationContext context)
        {
            const int gullyCountPerSide = 7;

            for (int sideIndex = 0;
                 sideIndex < 2;
                 sideIndex++)
            {
                float sideSign =
                    sideIndex == 0
                        ? -1f
                        : 1f;

                for (int gullyIndex = 0;
                     gullyIndex < gullyCountPerSide;
                     gullyIndex++)
                {
                    float randomA =
                        Hash01(
                            context.seedText,
                            100 +
                            sideIndex * 50 +
                            gullyIndex * 3);

                    float randomB =
                        Hash01(
                            context.seedText,
                            101 +
                            sideIndex * 50 +
                            gullyIndex * 3);

                    float randomC =
                        Hash01(
                            context.seedText,
                            102 +
                            sideIndex * 50 +
                            gullyIndex * 3);

                    float mouthZ =
                        Mathf.Lerp(
                            0.08f,
                            0.92f,
                            (gullyIndex +
                             0.30f +
                             randomA * 0.40f) /
                            gullyCountPerSide);

                    float outerX =
                        sideSign < 0f
                            ? Mathf.Lerp(
                                0.04f,
                                0.20f,
                                randomB)
                            : Mathf.Lerp(
                                0.80f,
                                0.96f,
                                randomB);

                    float width =
                        Mathf.Lerp(
                            0.010f,
                            0.024f,
                            randomC);

                    float depth =
                        Mathf.Lerp(
                            0.035f,
                            0.075f,
                            randomA);

                    CarveSingleGully(
                        heights,
                        context,
                        mouthZ,
                        outerX,
                        width,
                        depth);
                }
            }
        }

        private static void CarveSingleGully(
            float[,] heights,
            TerrainGenerationContext context,
            float mouthZ,
            float outerX,
            float width,
            float depth)
        {
            for (int y = 0;
                 y < HeightmapResolution;
                 y++)
            {
                float normalizedY =
                    GetNormalizedCoordinate(y);

                float alongDistance =
                    Mathf.Abs(
                        normalizedY -
                        mouthZ);

                if (alongDistance > 0.18f)
                {
                    continue;
                }

                float alongMask =
                    1f -
                    Mathf.InverseLerp(
                        0.02f,
                        0.18f,
                        alongDistance);

                alongMask =
                    Mathf.SmoothStep(
                        0f,
                        1f,
                        alongMask);

                float valleyCentre =
                    GetValleyCentre(
                        normalizedY,
                        context);

                float progressToMouth =
                    1f -
                    Mathf.Clamp01(
                        alongDistance /
                        0.18f);

                float centreX =
                    Mathf.Lerp(
                        outerX,
                        valleyCentre,
                        progressToMouth);

                centreX +=
                    Mathf.Sin(
                        normalizedY * 37f +
                        mouthZ * 13f) *
                    0.012f *
                    (1f - progressToMouth);

                for (int x = 0;
                     x < HeightmapResolution;
                     x++)
                {
                    float normalizedX =
                        GetNormalizedCoordinate(x);

                    float crossDistance =
                        Mathf.Abs(
                            normalizedX -
                            centreX);

                    float crossMask =
                        1f -
                        Mathf.InverseLerp(
                            width * 0.20f,
                            width,
                            crossDistance);

                    if (crossMask <= 0f)
                    {
                        continue;
                    }

                    crossMask =
                        crossMask *
                        crossMask *
                        (3f - 2f * crossMask);

                    float wallDistance =
                        Mathf.Abs(
                            normalizedX -
                            valleyCentre);

                    float wallMask =
                        Mathf.InverseLerp(
                            0.07f,
                            0.36f,
                            wallDistance);

                    heights[y, x] -=
                        depth *
                        alongMask *
                        crossMask *
                        wallMask;
                }
            }
        }

        private static void GenerateRockShelves(
            float[,] heights,
            TerrainGenerationContext context)
        {
            const int shelfCount = 8;

            for (int shelfIndex = 0;
                 shelfIndex < shelfCount;
                 shelfIndex++)
            {
                float randomA =
                    Hash01(
                        context.seedText,
                        300 +
                        shelfIndex * 3);

                float randomB =
                    Hash01(
                        context.seedText,
                        301 +
                        shelfIndex * 3);

                float randomC =
                    Hash01(
                        context.seedText,
                        302 +
                        shelfIndex * 3);

                float centreZ =
                    Mathf.Lerp(
                        0.10f,
                        0.90f,
                        randomA);

                float sideSign =
                    shelfIndex % 2 == 0
                        ? -1f
                        : 1f;

                float centreOffset =
                    Mathf.Lerp(
                        0.105f,
                        0.235f,
                        randomB);

                float radiusX =
                    Mathf.Lerp(
                        0.035f,
                        0.075f,
                        randomC);

                float radiusZ =
                    Mathf.Lerp(
                        0.025f,
                        0.060f,
                        randomA);

                FlattenSingleShelf(
                    heights,
                    context,
                    centreZ,
                    sideSign,
                    centreOffset,
                    radiusX,
                    radiusZ);
            }
        }

        private static void FlattenSingleShelf(
            float[,] heights,
            TerrainGenerationContext context,
            float centreZ,
            float sideSign,
            float centreOffset,
            float radiusX,
            float radiusZ)
        {
            float valleyCentre =
                GetValleyCentre(
                    centreZ,
                    context);

            float centreX =
                valleyCentre +
                sideSign *
                centreOffset;

            int centrePixelX =
                Mathf.Clamp(
                    Mathf.RoundToInt(
                        centreX *
                        (HeightmapResolution - 1)),
                    0,
                    HeightmapResolution - 1);

            int centrePixelY =
                Mathf.Clamp(
                    Mathf.RoundToInt(
                        centreZ *
                        (HeightmapResolution - 1)),
                    0,
                    HeightmapResolution - 1);

            float targetHeight =
                heights[
                    centrePixelY,
                    centrePixelX];

            for (int y = 0;
                 y < HeightmapResolution;
                 y++)
            {
                float normalizedY =
                    GetNormalizedCoordinate(y);

                float dz =
                    (normalizedY -
                     centreZ) /
                    radiusZ;

                if (Mathf.Abs(dz) > 1f)
                {
                    continue;
                }

                for (int x = 0;
                     x < HeightmapResolution;
                     x++)
                {
                    float normalizedX =
                        GetNormalizedCoordinate(x);

                    float dx =
                        (normalizedX -
                         centreX) /
                        radiusX;

                    float distanceSquared =
                        dx * dx +
                        dz * dz;

                    if (distanceSquared >= 1f)
                    {
                        continue;
                    }

                    float mask =
                        1f -
                        Mathf.Sqrt(
                            distanceSquared);

                    mask =
                        mask *
                        mask *
                        (3f - 2f * mask);

                    heights[y, x] =
                        Mathf.Lerp(
                            heights[y, x],
                            targetHeight,
                            mask * 0.38f);
                }
            }
        }


        private static void ApplyNaturalErosion(
            float[,] heights,
            TerrainGenerationContext context,
            int passes)
        {
            int resolution =
                HeightmapResolution;

            float[,] working =
                new float[
                    resolution,
                    resolution];

            for (int pass = 0;
                 pass < passes;
                 pass++)
            {
                CopyHeightmap(
                    heights,
                    working);

                float passStrength =
                    Mathf.Lerp(
                        0.22f,
                        0.10f,
                        pass /
                        Mathf.Max(
                            1f,
                            passes - 1f));

                for (int y = 1;
                     y < resolution - 1;
                     y++)
                {
                    float normalizedY =
                        GetNormalizedCoordinate(y);

                    float valleyCentre =
                        GetValleyCentre(
                            normalizedY,
                            context);

                    for (int x = 1;
                         x < resolution - 1;
                         x++)
                    {
                        float normalizedX =
                            GetNormalizedCoordinate(x);

                        float distanceFromValley =
                            Mathf.Abs(
                                normalizedX -
                                valleyCentre);

                        // Keep the immediate river floor and intentional
                        // settlement benches stable.
                        float erosionMask =
                            Mathf.InverseLerp(
                                0.055f,
                                0.34f,
                                distanceFromValley);

                        if (erosionMask <= 0f)
                        {
                            continue;
                        }

                        float current =
                            heights[y, x];

                        int lowestX = x;
                        int lowestY = y;
                        float lowest = current;

                        for (int offsetY = -1;
                             offsetY <= 1;
                             offsetY++)
                        {
                            for (int offsetX = -1;
                                 offsetX <= 1;
                                 offsetX++)
                            {
                                if (offsetX == 0 &&
                                    offsetY == 0)
                                {
                                    continue;
                                }

                                float neighbour =
                                    heights[
                                        y + offsetY,
                                        x + offsetX];

                                if (neighbour < lowest)
                                {
                                    lowest = neighbour;
                                    lowestX = x + offsetX;
                                    lowestY = y + offsetY;
                                }
                            }
                        }

                        float drop =
                            current -
                            lowest;

                        // Only move material on slopes steep enough to erode.
                        float talusThreshold =
                            0.0045f;

                        if (drop <= talusThreshold)
                        {
                            continue;
                        }

                        float drainageNoise =
                            Mathf.PerlinNoise(
                                context.detailOffsetX +
                                2200f +
                                normalizedX * 13f,

                                context.detailOffsetY +
                                2200f +
                                normalizedY * 13f);

                        float movable =
                            (drop -
                             talusThreshold) *
                            passStrength *
                            erosionMask *
                            Mathf.Lerp(
                                0.55f,
                                1f,
                                drainageNoise);

                        movable =
                            Mathf.Min(
                                movable,
                                0.008f);

                        working[y, x] -=
                            movable;

                        // Deposit part of the moved material downslope. The
                        // missing remainder represents material carried away
                        // into the Fraser River.
                        working[
                            lowestY,
                            lowestX] +=
                            movable * 0.62f;
                    }
                }

                CopyHeightmap(
                    working,
                    heights);

                ApplySelectiveTalusSmoothing(
                    heights,
                    context,
                    pass);
            }
        }

        private static void ApplySelectiveTalusSmoothing(
            float[,] heights,
            TerrainGenerationContext context,
            int pass)
        {
            int resolution =
                HeightmapResolution;

            float[,] working =
                new float[
                    resolution,
                    resolution];

            CopyHeightmap(
                heights,
                working);

            for (int y = 1;
                 y < resolution - 1;
                 y++)
            {
                float normalizedY =
                    GetNormalizedCoordinate(y);

                float valleyCentre =
                    GetValleyCentre(
                        normalizedY,
                        context);

                for (int x = 1;
                     x < resolution - 1;
                     x++)
                {
                    float normalizedX =
                        GetNormalizedCoordinate(x);

                    float distanceFromValley =
                        Mathf.Abs(
                            normalizedX -
                            valleyCentre);

                    float wallMask =
                        Mathf.InverseLerp(
                            0.08f,
                            0.34f,
                            distanceFromValley);

                    if (wallMask <= 0f)
                    {
                        continue;
                    }

                    float current =
                        heights[y, x];

                    float average =
                        (
                            heights[y, x - 1] +
                            heights[y, x + 1] +
                            heights[y - 1, x] +
                            heights[y + 1, x]) *
                        0.25f;

                    float localDifference =
                        Mathf.Abs(
                            current -
                            average);

                    // Smooth small artificial steps while preserving large
                    // cliffs and sharp bedrock faces.
                    float stepMask =
                        1f -
                        Mathf.InverseLerp(
                            0.010f,
                            0.035f,
                            localDifference);

                    float noise =
                        Mathf.PerlinNoise(
                            context.ridgeOffsetX +
                            2800f +
                            normalizedX * 8f,

                            context.ridgeOffsetY +
                            2800f +
                            normalizedY * 8f);

                    float amount =
                        wallMask *
                        stepMask *
                        Mathf.Lerp(
                            0.04f,
                            0.11f,
                            noise) *
                        Mathf.Lerp(
                            1f,
                            0.65f,
                            pass / 4f);

                    working[y, x] =
                        Mathf.Lerp(
                            current,
                            average,
                            amount);
                }
            }

            CopyHeightmap(
                working,
                heights);
        }

        private static void GenerateTerrainDetail(
            float[,] heights,
            TerrainGenerationContext context)
        {
            for (int y = 0;
                 y < HeightmapResolution;
                 y++)
            {
                for (int x = 0;
                     x < HeightmapResolution;
                     x++)
                {
                    float normalizedX =
                        GetNormalizedCoordinate(x);

                    float normalizedY =
                        GetNormalizedCoordinate(y);

                    float detailNoise =
                        Mathf.PerlinNoise(
                            context.detailOffsetX +
                            normalizedX * 8f,

                            context.detailOffsetY +
                            normalizedY * 8f);

                    float fineNoise =
                        Mathf.PerlinNoise(
                            context.detailOffsetX +
                            800f +
                            normalizedX * 18f,

                            context.detailOffsetY +
                            800f +
                            normalizedY * 18f);

                    heights[y, x] +=
                        detailNoise * 0.022f;

                    heights[y, x] +=
                        fineNoise * 0.006f;
                }
            }
        }

        private static void GenerateRiverTerraces(
            float[,] heights,
            TerrainGenerationContext context)
        {
            for (int y = 0;
                 y < HeightmapResolution;
                 y++)
            {
                float normalizedY =
                    GetNormalizedCoordinate(y);

                float valleyCentre =
                    GetValleyCentre(
                        normalizedY,
                        context);

                for (int x = 0;
                     x < HeightmapResolution;
                     x++)
                {
                    float normalizedX =
                        GetNormalizedCoordinate(x);

                    float distanceFromValley =
                        Mathf.Abs(
                            normalizedX -
                            valleyCentre);

                    float terraceMask =
                        1f -
                        Mathf.InverseLerp(
                            0.055f,
                            0.16f,
                            distanceFromValley);

                    if (terraceMask <= 0f)
                    {
                        continue;
                    }

                    float currentHeight =
                        heights[y, x];

                    float terraceStep =
                        0.025f;

                    float terracedHeight =
                        Mathf.Round(
                            currentHeight /
                            terraceStep) *
                        terraceStep;

                    heights[y, x] =
                        Mathf.Lerp(
                            currentHeight,
                            terracedHeight,
                            terraceMask * 0.18f);
                }
            }
        }

        private static void ApplyEdgeMountains(
            float[,] heights)
        {
            for (int y = 0;
                 y < HeightmapResolution;
                 y++)
            {
                for (int x = 0;
                     x < HeightmapResolution;
                     x++)
                {
                    float normalizedX =
                        GetNormalizedCoordinate(x);

                    float normalizedY =
                        GetNormalizedCoordinate(y);

                    float edgeX =
                        Mathf.Abs(
                            normalizedX - 0.5f) *
                        2f;

                    float edgeY =
                        Mathf.Abs(
                            normalizedY - 0.5f) *
                        2f;

                    float edgeMask =
                        Mathf.Max(edgeX, edgeY);

                    edgeMask =
                        Mathf.SmoothStep(
                            0f,
                            1f,
                            Mathf.InverseLerp(
                                0.65f,
                                1f,
                                edgeMask));

                    heights[y, x] +=
                        edgeMask * 0.055f;
                }
            }
        }

        private static void SmoothHeightmap(
            float[,] heights,
            int passes)
        {
            int resolution =
                HeightmapResolution;

            float[,] working =
                new float[
                    resolution,
                    resolution];

            for (int pass = 0;
                 pass < passes;
                 pass++)
            {
                for (int y = 0;
                     y < resolution;
                     y++)
                {
                    for (int x = 0;
                         x < resolution;
                         x++)
                    {
                        float total = 0f;
                        int sampleCount = 0;

                        for (int offsetY = -1;
                             offsetY <= 1;
                             offsetY++)
                        {
                            for (int offsetX = -1;
                                 offsetX <= 1;
                                 offsetX++)
                            {
                                int sampleX =
                                    x + offsetX;

                                int sampleY =
                                    y + offsetY;

                                if (sampleX < 0 ||
                                    sampleX >= resolution ||
                                    sampleY < 0 ||
                                    sampleY >= resolution)
                                {
                                    continue;
                                }

                                total +=
                                    heights[
                                        sampleY,
                                        sampleX];

                                sampleCount++;
                            }
                        }

                        working[y, x] =
                            sampleCount > 0
                                ? total / sampleCount
                                : heights[y, x];
                    }
                }

                CopyHeightmap(
                    working,
                    heights);
            }
        }

        private static void NormalizeHeightmap(
            float[,] heights)
        {
            float minimumHeight =
                float.MaxValue;

            float maximumHeight =
                float.MinValue;

            for (int y = 0;
                 y < HeightmapResolution;
                 y++)
            {
                for (int x = 0;
                     x < HeightmapResolution;
                     x++)
                {
                    float height =
                        heights[y, x];

                    minimumHeight =
                        Mathf.Min(
                            minimumHeight,
                            height);

                    maximumHeight =
                        Mathf.Max(
                            maximumHeight,
                            height);
                }
            }

            float heightRange =
                maximumHeight -
                minimumHeight;

            if (heightRange <=
                Mathf.Epsilon)
            {
                return;
            }

            for (int y = 0;
                 y < HeightmapResolution;
                 y++)
            {
                for (int x = 0;
                     x < HeightmapResolution;
                     x++)
                {
                    float normalizedHeight =
                        Mathf.InverseLerp(
                            minimumHeight,
                            maximumHeight,
                            heights[y, x]);

                    // Keep room above the highest generated ridges while
                    // using substantially more of the 520 m terrain range.
                    heights[y, x] =
                        Mathf.Lerp(
                            0.018f,
                            0.72f,
                            Mathf.Pow(
                                normalizedHeight,
                                0.92f));
                }
            }
        }

        private static float GetValleyCentre(
            float normalizedY,
            TerrainGenerationContext context)
        {
            return BoomtownRiverSpine.GetNormalizedCentreX(
                normalizedY,
                context.seedText);
        }

        private static float GetNormalizedCoordinate(
            int coordinate)
        {
            return coordinate /
                   (float)(
                       HeightmapResolution - 1);
        }

        private static void CopyHeightmap(
            float[,] source,
            float[,] destination)
        {
            for (int y = 0;
                 y < HeightmapResolution;
                 y++)
            {
                for (int x = 0;
                     x < HeightmapResolution;
                     x++)
                {
                    destination[y, x] =
                        source[y, x];
                }
            }
        }


        private static float Hash01(
            string seedText,
            int salt)
        {
            int hash =
                StableHash(
                    (seedText ?? string.Empty) +
                    "_" +
                    salt);

            uint unsignedHash =
                unchecked((uint)hash);

            return
                (unsignedHash & 0x00FFFFFF) /
                16777215f;
        }

        private static int StableHash(
            string text)
        {
            unchecked
            {
                int hash = 23;

                foreach (char character
                         in text ??
                         string.Empty)
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

            foreach (
                char invalidCharacter
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
                "Assets/Boomtown/Scripts/WorldGeneration";

            if (!AssetDatabase.IsValidFolder(root))
            {
                if (!AssetDatabase.IsValidFolder(
                        "Assets/Boomtown/Scripts"))
                {
                    AssetDatabase.CreateFolder(
                        "Assets/Boomtown",
                        "Scripts");
                }

                AssetDatabase.CreateFolder(
                    "Assets/Boomtown/Scripts",
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

        private sealed class TerrainGenerationContext
        {
            public string seedText;

            public float offsetX;
            public float offsetY;

            public float ridgeOffsetX;
            public float ridgeOffsetY;

            public float detailOffsetX;
            public float detailOffsetY;
        }
    }
}