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
            SmoothHeightmap(heights, 5);
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

                    // Second, much lower-frequency octave so the foothill
                    // country outside the immediate canyon corridor rolls
                    // instead of reading as a flat plain between the valley
                    // walls and the edge mountains.
                    float rollingHills =
                        Mathf.PerlinNoise(
                            context.offsetX +
                            480f +
                            normalizedX * 0.55f,

                            context.offsetY +
                            480f +
                            normalizedY * 0.55f);

                    rollingHills =
                        Mathf.SmoothStep(
                            0f,
                            1f,
                            rollingHills);

                    heights[y, x] +=
                        broadNoise * 0.09f +
                        rollingHills * 0.11f;
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

                    // A single raw Perlin octave pushed through abs()/pow()
                    // to sharpen it into "ridge noise" is a well-known trap:
                    // Unity's Perlin implementation has an inherent
                    // diagonal grain, and ridge-sharpening makes that grain
                    // read as visible parallel streaks once lit, instead of
                    // looking like irregular fractured rock. Domain-warp the
                    // sample coordinates with an independent, much
                    // lower-frequency noise field first (so the warp itself
                    // doesn't reintroduce a regular pattern), then blend in
                    // a second, finer ridge octave -- both standard fixes
                    // for streaky single-octave ridge noise.
                    float warpNoiseX =
                        Mathf.PerlinNoise(
                            context.ridgeOffsetX +
                            5000f +
                            normalizedX * 1.6f,

                            context.ridgeOffsetY +
                            5000f +
                            normalizedY * 1.6f);

                    float warpNoiseY =
                        Mathf.PerlinNoise(
                            context.ridgeOffsetX +
                            6200f +
                            normalizedX * 1.6f,

                            context.ridgeOffsetY +
                            6200f +
                            normalizedY * 1.6f);

                    float warpedX =
                        normalizedX +
                        (warpNoiseX - 0.5f) * 0.20f;

                    float warpedY =
                        normalizedY +
                        (warpNoiseY - 0.5f) * 0.20f;

                    float ridgeNoise =
                        Mathf.PerlinNoise(
                            context.ridgeOffsetX +
                            warpedX * 4.2f,

                            context.ridgeOffsetY +
                            warpedY * 4.2f);

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

                    float fineRidgeNoise =
                        Mathf.PerlinNoise(
                            context.ridgeOffsetX +
                            900f +
                            warpedX * 9.7f,

                            context.ridgeOffsetY +
                            900f +
                            warpedY * 9.7f);

                    fineRidgeNoise =
                        Mathf.Abs(
                            fineRidgeNoise * 2f -
                            1f);

                    fineRidgeNoise =
                        1f - fineRidgeNoise;

                    fineRidgeNoise =
                        Mathf.Pow(
                            fineRidgeNoise,
                            2.2f);

                    float combinedRidge =
                        ridgeNoise * 0.72f +
                        fineRidgeNoise * 0.28f;

                    heights[y, x] +=
                        combinedRidge *
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

                    // The raw sine grating below reads as a perfectly
                    // regular, mechanical diagonal corduroy pattern once
                    // lit -- real fractured rock doesn't run in dead-straight
                    // parallel lines. Warp the sample coordinates with a
                    // separate low-frequency noise field first so the ridge
                    // lines bend, split, and vary in spacing like an actual
                    // fracture pattern instead of a repeating stripe.
                    float warpNoiseX =
                        Mathf.PerlinNoise(
                            context.ridgeOffsetX +
                            3000f +
                            normalizedX * 3.2f,

                            context.ridgeOffsetY +
                            3000f +
                            normalizedY * 3.2f);

                    float warpNoiseY =
                        Mathf.PerlinNoise(
                            context.ridgeOffsetX +
                            4200f +
                            normalizedX * 3.2f,

                            context.ridgeOffsetY +
                            4200f +
                            normalizedY * 3.2f);

                    float warpedX =
                        normalizedX +
                        (warpNoiseX - 0.5f) * 0.16f;

                    float warpedY =
                        normalizedY +
                        (warpNoiseY - 0.5f) * 0.16f;

                    float ridgeBands =
                        Mathf.Abs(
                            Mathf.Sin(
                                warpedX * 22f +
                                warpedY * 11f +
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

                    // Wider jitter within each slot (was 0.30-0.70, i.e.
                    // only 40% of the slot) so mouths land at genuinely
                    // irregular spacing instead of an almost evenly-combed
                    // row. Combined with the shorter reach below, this stops
                    // neighbouring gullies' influence zones from stacking
                    // into a continuous, mechanically regular set of
                    // parallel diagonal grooves across the whole hillside.
                    float mouthZ =
                        Mathf.Lerp(
                            0.08f,
                            0.92f,
                            (gullyIndex +
                             0.10f +
                             randomA * 0.80f) /
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

                    // Softened further (was 0.035-0.075, up to 39m deep;
                    // then 0.020-0.045) so gullies read as terrain texture
                    // rather than dominating the hillside's silhouette.
                    float depth =
                        Mathf.Lerp(
                            0.012f,
                            0.028f,
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

                // Reach shortened from 0.18 (360m) to 0.10 (200m) -- with
                // 7 gullies per side spaced roughly every 0.143 (286m) of
                // map length, the old 360m reach meant neighbouring
                // gullies' influence zones overlapped almost their entire
                // length, stacking several near-parallel diagonal carves
                // on top of each other into what read as one continuous,
                // mechanically regular corduroy pattern. A reach shorter
                // than the spacing keeps each gully visually distinct.
                const float gullyReach = 0.075f;

                float alongDistance =
                    Mathf.Abs(
                        normalizedY -
                        mouthZ);

                if (alongDistance > gullyReach)
                {
                    continue;
                }

                float alongMask =
                    1f -
                    Mathf.InverseLerp(
                        0.015f,
                        gullyReach,
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
                        gullyReach);

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
            float nominalCentreX =
                GetValleyCentre(
                    centreZ,
                    context) +
                sideSign *
                centreOffset;

            int centrePixelX =
                Mathf.Clamp(
                    Mathf.RoundToInt(
                        nominalCentreX *
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

            // Average a small neighbourhood instead of trusting one sampled
            // pixel, so a single rough/outlier texel can't become the whole
            // shelf's target height.
            float targetHeight =
                SampleNeighbourhoodAverage(
                    heights,
                    centrePixelX,
                    centrePixelY,
                    2);

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

                // Track the wall's own bend at this row instead of reusing
                // one fixed X for the whole shelf -- with a meandering
                // valley, a fixed-position ellipse can drift off the wall
                // and hang partly over the river floor, forcibly flattening
                // a big height mismatch into a crater. Following the wall
                // per row keeps the shelf glued to the slope it was meant
                // to sit on.
                float rowCentreX =
                    GetValleyCentre(
                        normalizedY,
                        context) +
                    sideSign *
                    centreOffset;

                for (int x = 0;
                     x < HeightmapResolution;
                     x++)
                {
                    float normalizedX =
                        GetNormalizedCoordinate(x);

                    float dx =
                        (normalizedX -
                         rowCentreX) /
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

        private static float SampleNeighbourhoodAverage(
            float[,] heights,
            int centrePixelX,
            int centrePixelY,
            int radius)
        {
            float total = 0f;
            int count = 0;

            for (int offsetY = -radius;
                 offsetY <= radius;
                 offsetY++)
            {
                int sampleY =
                    centrePixelY +
                    offsetY;

                if (sampleY < 0 ||
                    sampleY >= HeightmapResolution)
                {
                    continue;
                }

                for (int offsetX = -radius;
                     offsetX <= radius;
                     offsetX++)
                {
                    int sampleX =
                        centrePixelX +
                        offsetX;

                    if (sampleX < 0 ||
                        sampleX >= HeightmapResolution)
                    {
                        continue;
                    }

                    total +=
                        heights[
                            sampleY,
                            sampleX];

                    count++;
                }
            }

            return count > 0
                ? total / count
                : heights[
                    centrePixelY,
                    centrePixelX];
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

            // Reused scratch buffers for the downhill-neighbour weighting
            // below -- declared once and overwritten per pixel instead of
            // allocated fresh each iteration.
            float[] neighbourDrops =
                new float[8];

            int[] neighbourX =
                new int[8];

            int[] neighbourY =
                new int[8];

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

                        // Steepest-of-8 alone was used only to gate/scale
                        // erosion strength (via "drop" below); material was
                        // also dumped entirely into that one neighbour. On a
                        // smooth low-frequency slope, many neighbouring
                        // pixels all pick the same one of the 8 compass
                        // directions as "steepest", and doing that
                        // deterministically for 5 passes builds up visible
                        // coherent diagonal flow-line banding across whole
                        // hillsides. Fix: still gate/scale off the steepest
                        // drop, but *distribute* the moved material across
                        // every downhill neighbour weighted by how much
                        // lower each one is (a simple D-infinity-style
                        // multi-flow scheme), so flow doesn't snap to the
                        // grid's 8 discrete directions.
                        float lowest = current;
                        int downhillCount = 0;
                        float totalDownhillWeight = 0f;

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
                                }

                                float neighbourDrop =
                                    current -
                                    neighbour;

                                if (neighbourDrop > 0f)
                                {
                                    neighbourDrops[downhillCount] =
                                        neighbourDrop;

                                    neighbourX[downhillCount] =
                                        x + offsetX;

                                    neighbourY[downhillCount] =
                                        y + offsetY;

                                    totalDownhillWeight +=
                                        neighbourDrop;

                                    downhillCount++;
                                }
                            }
                        }

                        float drop =
                            current -
                            lowest;

                        // Only move material on slopes steep enough to erode.
                        float talusThreshold =
                            0.0045f;

                        if (drop <= talusThreshold ||
                            downhillCount == 0)
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

                        // Deposit part of the moved material downslope,
                        // spread across all downhill neighbours in
                        // proportion to their share of the total drop. The
                        // missing remainder represents material carried away
                        // into the Fraser River.
                        float depositTotal =
                            movable * 0.62f;

                        for (int n = 0;
                             n < downhillCount;
                             n++)
                        {
                            float share =
                                neighbourDrops[n] /
                                totalDownhillWeight;

                            working[
                                neighbourY[n],
                                neighbourX[n]] +=
                                depositTotal * share;
                        }
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