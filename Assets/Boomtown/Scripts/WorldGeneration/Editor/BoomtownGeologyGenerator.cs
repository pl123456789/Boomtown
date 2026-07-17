using System;
using UnityEditor;
using UnityEngine;

namespace Boomtown.WorldGeneration.Editor
{
    /// <summary>
    /// Generates one authoritative gold chain:
    ///
    /// mineralized mountain -> quartz vein -> historical weathering ->
    /// river entry -> downstream hydraulic sorting -> finite placer gold.
    ///
    /// The generator never scales placer gold to a fixed district target and
    /// never creates fallback deposits. Every placer ounce is deducted from a
    /// generated quartz vein.
    /// </summary>
    public static class BoomtownGeologyGenerator
    {
        private const string GeneratedFolder =
            "Assets/Boomtown/Scripts/WorldGeneration/Generated";

        private const int GridResolution = 128;

        private const float MinimumCellOunces =
            0.00001f;

        public static BoomtownGeologyData Generate(
            Terrain terrain,
            BoomtownMapDefinition mapDefinition)
        {
            return Generate(
                terrain,
                mapDefinition,
                LoadRiverData(mapDefinition));
        }

        public static BoomtownGeologyData Generate(
            Terrain terrain,
            BoomtownMapDefinition mapDefinition,
            RiverData riverData)
        {
            if (terrain == null ||
                terrain.terrainData == null)
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
                MakeSafeName(
                    mapDefinition.mapName);

            string assetPath =
                $"{GeneratedFolder}/" +
                $"{safeMapName}_{mapDefinition.year}_Geology.asset";

            AssetDatabase.DeleteAsset(
                assetPath);

            BoomtownGeologyData geology =
                ScriptableObject.CreateInstance<
                    BoomtownGeologyData>();

            InitializeGeology(
                terrain,
                mapDefinition,
                geology);

            GenerateBedrockAndQuartzPotential(
                terrain,
                mapDefinition,
                geology);

            BoomtownGoldSourceGenerator.Generate(
                terrain,
                mapDefinition,
                geology);

            if (riverData != null &&
                riverData.samples != null &&
                riverData.samples.Count > 1)
            {
                GenerateVeinDrivenPlacer(
                    riverData,
                    geology);
            }
            else
            {
                Debug.LogWarning(
                    "[Boomtown Geology Generator] RiverData missing. " +
                    "Hard-rock veins were generated, but no placer gold " +
                    "could be transported.");
            }

            Array.Copy(
                geology.coarseGoldInitialOunces,
                geology.coarseGoldRemainingOunces,
                geology.coarseGoldInitialOunces.Length);

            Array.Copy(
                geology.fineGoldInitialOunces,
                geology.fineGoldRemainingOunces,
                geology.fineGoldInitialOunces.Length);

            Array.Copy(
                geology.flourGoldInitialOunces,
                geology.flourGoldRemainingOunces,
                geology.flourGoldInitialOunces.Length);

            geology.RebuildCompatibilityTotals();

            AssetDatabase.CreateAsset(
                geology,
                assetPath);

            EditorUtility.SetDirty(
                geology);

            AssetDatabase.SaveAssets();

            int depositCells =
                CountPositiveCells(
                    geology.placerInitialOunces);

            float released =
                GetHistoricallyReleasedGold(
                    geology);

            Debug.Log(
                $"[Boomtown Geology Generator] Generated vein-driven geology " +
                $"for {mapDefinition.mapName}, {mapDefinition.year}. " +
                $"Mountains: {geology.goldMountains.Count}. " +
                $"Veins: {geology.quartzVeins.Count}. " +
                $"Historically weathered: {released:0.00} oz. " +
                $"Finite placer: {geology.GetTotalInitialOunces():0.00} oz " +
                $"across {depositCells} cells. " +
                $"Hard rock remaining: " +
                $"{geology.GetTotalRemainingHardRockOunces():0.00} oz.");

            return geology;
        }

        private static void InitializeGeology(
            Terrain terrain,
            BoomtownMapDefinition definition,
            BoomtownGeologyData geology)
        {
            geology.mapName =
                definition.mapName;

            geology.historicalYear =
                definition.year;

            geology.worldSeed =
                definition.worldSeed;

            geology.resolutionX =
                GridResolution;

            geology.resolutionZ =
                GridResolution;

            geology.worldOrigin =
                terrain.transform.position;

            geology.worldSize =
                terrain.terrainData.size;

            int cellCount =
                GridResolution *
                GridResolution;

            geology.bedrockHardness =
                new float[cellCount];

            geology.quartzPotential =
                new float[cellCount];

            geology.placerConcentration =
                new float[cellCount];

            geology.placerInitialOunces =
                new float[cellCount];

            geology.placerRemainingOunces =
                new float[cellCount];

            geology.placerSourceId =
                new int[cellCount];

            geology.sand =
                new float[cellCount];

            geology.gravel =
                new float[cellCount];

            geology.blackSand =
                new float[cellCount];

            geology.coarseGoldInitialOunces =
                new float[cellCount];

            geology.fineGoldInitialOunces =
                new float[cellCount];

            geology.flourGoldInitialOunces =
                new float[cellCount];

            geology.coarseGoldRemainingOunces =
                new float[cellCount];

            geology.fineGoldRemainingOunces =
                new float[cellCount];

            geology.flourGoldRemainingOunces =
                new float[cellCount];

            Array.Fill(
                geology.placerSourceId,
                -1);
        }

        private static void GenerateBedrockAndQuartzPotential(
            Terrain terrain,
            BoomtownMapDefinition definition,
            BoomtownGeologyData geology)
        {
            int seed =
                StableHash(
                    definition.worldSeed +
                    "_BEDROCK");

            TerrainData terrainData =
                terrain.terrainData;

            for (int z = 0;
                 z < GridResolution;
                 z++)
            {
                for (int x = 0;
                     x < GridResolution;
                     x++)
                {
                    float nx =
                        x /
                        (float)(
                            GridResolution - 1);

                    float nz =
                        z /
                        (float)(
                            GridResolution - 1);

                    int index =
                        z *
                        GridResolution +
                        x;

                    float normalizedHeight =
                        terrainData.GetInterpolatedHeight(
                            nx,
                            nz) /
                        terrainData.size.y;

                    float bedrockNoise =
                        Mathf.PerlinNoise(
                            nx * 4.5f +
                            seed * 0.0001f,
                            nz * 4.5f +
                            seed * 0.0001f);

                    float faultA =
                        1f -
                        Mathf.Abs(
                            Mathf.Sin(
                                nx * 13f +
                                nz * 6f +
                                seed * 0.0002f));

                    float faultB =
                        1f -
                        Mathf.Abs(
                            Mathf.Sin(
                                nx * 6f -
                                nz * 15f +
                                seed * 0.00017f));

                    geology.bedrockHardness[index] =
                        Mathf.Clamp01(
                            bedrockNoise * 0.72f +
                            normalizedHeight * 0.28f);

                    geology.quartzPotential[index] =
                        Mathf.Clamp01(
                            Mathf.Max(
                                faultA,
                                faultB * 0.82f) *
                            Mathf.Lerp(
                                0.55f,
                                1f,
                                normalizedHeight));
                }
            }
        }

        private static void GenerateVeinDrivenPlacer(
            RiverData riverData,
            BoomtownGeologyData geology)
        {
            if (geology.quartzVeins == null)
            {
                return;
            }

            foreach (QuartzVeinData vein
                     in geology.quartzVeins)
            {
                if (vein == null ||
                    vein.initialGoldOunces <= 0f)
                {
                    continue;
                }

                int entryIndex =
                    FindNearestRiverSampleIndex(
                        riverData,
                        Vector3.Lerp(
                            vein.start,
                            vein.end,
                            0.5f));

                RiverSample entrySample =
                    riverData.samples[
                        entryIndex];

                vein.riverEntrySampleIndex =
                    entryIndex;

                vein.riverEntryPosition =
                    entrySample.position;

                float releasedOunces =
                    Mathf.Clamp(
                        vein.initialGoldOunces *
                        vein.historicalWeatheredFraction,
                        0f,
                        vein.remainingGoldOunces);

                vein.historicallyReleasedGoldOunces =
                    releasedOunces;

                vein.remainingGoldOunces -=
                    releasedOunces;

                UpdateMountainWeathering(
                    geology,
                    vein,
                    releasedOunces);

                AssignParticleFractions(
                    vein);

                TransportParticleClass(
                    riverData,
                    geology,
                    vein,
                    entryIndex,
                    releasedOunces *
                    vein.coarseGoldFraction,
                    transportLengthSamples: 18,
                    classTrapBias: 1.25f,
                    bankSpreadMultiplier: 0.72f,
                    particleClass: GoldParticleClass.Coarse);

                TransportParticleClass(
                    riverData,
                    geology,
                    vein,
                    entryIndex,
                    releasedOunces *
                    vein.fineGoldFraction,
                    transportLengthSamples: 54,
                    classTrapBias: 0.92f,
                    bankSpreadMultiplier: 1f,
                    particleClass: GoldParticleClass.Fine);

                TransportParticleClass(
                    riverData,
                    geology,
                    vein,
                    entryIndex,
                    releasedOunces *
                    vein.flourGoldFraction,
                    transportLengthSamples:
                        riverData.samples.Count,
                    classTrapBias: 0.48f,
                    bankSpreadMultiplier: 1.18f,
                    particleClass: GoldParticleClass.Flour);
            }

            NormalizeConcentration(
                geology);
        }

        private static void AssignParticleFractions(
            QuartzVeinData vein)
        {
            float exposure =
                Mathf.Clamp01(
                    vein.exposedFraction);

            float widthFactor =
                Mathf.InverseLerp(
                    0.35f,
                    3.8f,
                    vein.widthMetres);

            vein.coarseGoldFraction =
                Mathf.Lerp(
                    0.08f,
                    0.31f,
                    exposure *
                    widthFactor);

            vein.flourGoldFraction =
                Mathf.Lerp(
                    0.26f,
                    0.52f,
                    1f - exposure);

            vein.fineGoldFraction =
                Mathf.Max(
                    0f,
                    1f -
                    vein.coarseGoldFraction -
                    vein.flourGoldFraction);

            float total =
                vein.coarseGoldFraction +
                vein.fineGoldFraction +
                vein.flourGoldFraction;

            if (total > 0f)
            {
                vein.coarseGoldFraction /=
                    total;

                vein.fineGoldFraction /=
                    total;

                vein.flourGoldFraction /=
                    total;
            }
        }

        private static void TransportParticleClass(
            RiverData riverData,
            BoomtownGeologyData geology,
            QuartzVeinData vein,
            int entryIndex,
            float sourceOunces,
            int transportLengthSamples,
            float classTrapBias,
            float bankSpreadMultiplier,
            GoldParticleClass particleClass)
        {
            if (sourceOunces <= 0f)
            {
                return;
            }

            float carried =
                sourceOunces;

            int finalIndex =
                Mathf.Min(
                    riverData.samples.Count - 1,
                    entryIndex +
                    transportLengthSamples);

            int travelledSamples = 0;

            for (int sampleIndex = entryIndex;
                 sampleIndex <= finalIndex &&
                 carried > MinimumCellOunces;
                 sampleIndex++)
            {
                RiverSample sample =
                    riverData.samples[
                        sampleIndex];

                float t =
                    transportLengthSamples <= 0
                        ? 1f
                        : travelledSamples /
                          (float)transportLengthSamples;

                float suitability =
                    CalculateHydraulicTrap(
                        riverData,
                        sampleIndex,
                        classTrapBias);

                // A small finite trace settles through almost every downstream
                // reach. Strong traps receive the bulk of the mass.
                float traceFraction =
                    Mathf.Lerp(
                        0.0012f,
                        0.00025f,
                        t);

                float trapFraction =
                    Mathf.Lerp(
                        0.004f,
                        0.19f,
                        suitability);

                float depositFraction =
                    Mathf.Clamp01(
                        traceFraction +
                        trapFraction);

                float deposited =
                    Mathf.Min(
                        carried,
                        carried *
                        depositFraction);

                DepositAroundRiverSample(
                    geology,
                    sample,
                    deposited,
                    suitability,
                    vein.id,
                    bankSpreadMultiplier,
                    particleClass);

                carried -=
                    deposited;

                travelledSamples++;
            }

            // Fine material that reaches the district boundary is recorded as
            // transported out of this playable district, not destroyed.
        }

        private static float CalculateHydraulicTrap(
            RiverData riverData,
            int sampleIndex,
            float classTrapBias)
        {
            RiverSample sample =
                riverData.samples[
                    sampleIndex];

            float slowWater =
                1f -
                Mathf.InverseLerp(
                    0.75f,
                    3.1f,
                    sample.velocity);

            float shallowWater =
                1f -
                Mathf.InverseLerp(
                    1.2f,
                    8f,
                    sample.depth);

            float gravel =
                Mathf.Clamp01(
                    sample.gravelProbability);

            float bankTrap =
                Mathf.Max(
                    LandscapeTrap(
                        sample.leftLandscape),
                    LandscapeTrap(
                        sample.rightLandscape));

            float bendTrap =
                CalculateBendTrap(
                    riverData,
                    sampleIndex);

            return Mathf.Clamp01(
                (
                    gravel * 0.31f +
                    slowWater * 0.24f +
                    shallowWater * 0.12f +
                    bankTrap * 0.19f +
                    bendTrap * 0.14f
                ) *
                classTrapBias);
        }

        private static float CalculateBendTrap(
            RiverData riverData,
            int index)
        {
            if (index <= 0 ||
                index >=
                riverData.samples.Count - 1)
            {
                return 0f;
            }

            Vector3 previous =
                riverData.samples[index - 1]
                    .tangent.normalized;

            Vector3 next =
                riverData.samples[index + 1]
                    .tangent.normalized;

            float turn =
                Vector3.Angle(
                    previous,
                    next);

            return Mathf.InverseLerp(
                1f,
                18f,
                turn);
        }

        private static float LandscapeTrap(
            RiverLandscapeType landscape)
        {
            switch (landscape)
            {
                case RiverLandscapeType.GravelBar:
                    return 1f;

                case RiverLandscapeType.BedrockMargin:
                    return 0.86f;

                case RiverLandscapeType.Floodplain:
                    return 0.54f;

                case RiverLandscapeType.Terrace:
                    return 0.38f;

                case RiverLandscapeType.CutBank:
                    return 0.12f;

                case RiverLandscapeType.ActiveChannel:
                default:
                    return 0.28f;
            }
        }

        private static void DepositAroundRiverSample(
            BoomtownGeologyData geology,
            RiverSample sample,
            float totalOunces,
            float suitability,
            int veinId,
            float spreadMultiplier,
            GoldParticleClass particleClass)
        {
            if (totalOunces <= 0f)
            {
                return;
            }

            bool preferLeft =
                LandscapeTrap(
                    sample.leftLandscape) >=
                LandscapeTrap(
                    sample.rightLandscape);

            float sideSign =
                preferLeft
                    ? -1f
                    : 1f;

            float preferredWidth =
                preferLeft
                    ? sample.leftWidth
                    : sample.rightWidth;

            Vector3 centre =
                sample.position +
                sample.RightDirection *
                preferredWidth *
                0.68f *
                sideSign;

            float radius =
                Mathf.Lerp(
                    16f,
                    58f,
                    suitability) *
                spreadMultiplier;

            DistributeCircularDeposit(
                geology,
                centre,
                radius,
                totalOunces,
                suitability,
                veinId,
                particleClass);
        }

        private static void DistributeCircularDeposit(
            BoomtownGeologyData geology,
            Vector3 centre,
            float radius,
            float totalOunces,
            float suitability,
            int veinId,
            GoldParticleClass particleClass)
        {
            int minX;
            int maxX;
            int minZ;
            int maxZ;

            GetCellBounds(
                geology,
                centre,
                radius,
                out minX,
                out maxX,
                out minZ,
                out maxZ);

            float totalWeight = 0f;

            for (int z = minZ;
                 z <= maxZ;
                 z++)
            {
                for (int x = minX;
                     x <= maxX;
                     x++)
                {
                    float distance =
                        CellDistance(
                            geology,
                            x,
                            z,
                            centre);

                    if (distance > radius)
                    {
                        continue;
                    }

                    totalWeight +=
                        Mathf.Pow(
                            1f -
                            distance /
                            radius,
                            1.7f);
                }
            }

            if (totalWeight <=
                Mathf.Epsilon)
            {
                return;
            }

            for (int z = minZ;
                 z <= maxZ;
                 z++)
            {
                for (int x = minX;
                     x <= maxX;
                     x++)
                {
                    float distance =
                        CellDistance(
                            geology,
                            x,
                            z,
                            centre);

                    if (distance > radius)
                    {
                        continue;
                    }

                    float weight =
                        Mathf.Pow(
                            1f -
                            distance /
                            radius,
                            1.7f);

                    int index =
                        z *
                        geology.resolutionX +
                        x;

                    float ounces =
                        totalOunces *
                        weight /
                        totalWeight;

                    AddGoldByParticleClass(
                        geology,
                        index,
                        ounces,
                        particleClass);

                    AddSediment(
                        geology,
                        index,
                        suitability,
                        particleClass,
                        ounces);

                    geology.placerConcentration[index] +=
                        ounces *
                        Mathf.Lerp(
                            0.65f,
                            1.35f,
                            suitability);

                    float currentTotal =
                        geology.coarseGoldInitialOunces[index] +
                        geology.fineGoldInitialOunces[index] +
                        geology.flourGoldInitialOunces[index];

                    if (geology.placerSourceId[index] < 0 ||
                        ounces >
                        currentTotal * 0.5f)
                    {
                        geology.placerSourceId[index] =
                            veinId;
                    }
                }
            }
        }

        private static void AddGoldByParticleClass(
            BoomtownGeologyData geology,
            int index,
            float ounces,
            GoldParticleClass particleClass)
        {
            switch (particleClass)
            {
                case GoldParticleClass.Coarse:
                    geology.coarseGoldInitialOunces[index] +=
                        ounces;
                    break;

                case GoldParticleClass.Fine:
                    geology.fineGoldInitialOunces[index] +=
                        ounces;
                    break;

                case GoldParticleClass.Flour:
                    geology.flourGoldInitialOunces[index] +=
                        ounces;
                    break;
            }
        }

        private static void AddSediment(
            BoomtownGeologyData geology,
            int index,
            float suitability,
            GoldParticleClass particleClass,
            float goldOunces)
        {
            float massScale =
                Mathf.Log10(
                    1f +
                    goldOunces *
                    1000f);

            geology.sand[index] +=
                massScale *
                Mathf.Lerp(
                    0.35f,
                    1.1f,
                    1f - suitability);

            geology.gravel[index] +=
                massScale *
                Mathf.Lerp(
                    0.25f,
                    1.25f,
                    suitability);

            float heavyMineralBias =
                particleClass ==
                GoldParticleClass.Coarse
                    ? 1.25f
                    : particleClass ==
                      GoldParticleClass.Fine
                        ? 0.95f
                        : 0.62f;

            geology.blackSand[index] +=
                massScale *
                suitability *
                heavyMineralBias;
        }

        private static void NormalizeConcentration(
            BoomtownGeologyData geology)
        {
            float maximum = 0f;

            for (int index = 0;
                 index <
                 geology.placerConcentration.Length;
                 index++)
            {
                maximum =
                    Mathf.Max(
                        maximum,
                        geology.placerConcentration[index]);
            }

            if (maximum <= 0f)
            {
                return;
            }

            float logarithmicMaximum =
                Mathf.Log10(
                    1f + maximum);

            for (int index = 0;
                 index <
                 geology.placerConcentration.Length;
                 index++)
            {
                float value =
                    geology.placerConcentration[index];

                geology.placerConcentration[index] =
                    value <= 0f
                        ? 0f
                        : Mathf.Clamp01(
                            Mathf.Log10(
                                1f + value) /
                            logarithmicMaximum);
            }
        }

        private static void UpdateMountainWeathering(
            BoomtownGeologyData geology,
            QuartzVeinData vein,
            float releasedOunces)
        {
            if (geology.goldMountains == null)
            {
                return;
            }

            foreach (GoldMountainData mountain
                     in geology.goldMountains)
            {
                if (mountain == null ||
                    mountain.id !=
                    vein.mountainId)
                {
                    continue;
                }

                mountain.historicallyWeatheredGoldOunces +=
                    releasedOunces;

                mountain.remainingHardRockGoldOunces =
                    Mathf.Max(
                        0f,
                        mountain.remainingHardRockGoldOunces -
                        releasedOunces);

                return;
            }
        }

        private static int FindNearestRiverSampleIndex(
            RiverData riverData,
            Vector3 position)
        {
            int nearestIndex = 0;
            float bestDistance =
                float.MaxValue;

            for (int index = 0;
                 index <
                 riverData.samples.Count;
                 index++)
            {
                Vector3 delta =
                    riverData.samples[index].position -
                    position;

                delta.y = 0f;

                float distance =
                    delta.sqrMagnitude;

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    nearestIndex = index;
                }
            }

            return nearestIndex;
        }

        private static void GetCellBounds(
            BoomtownGeologyData geology,
            Vector3 centre,
            float radius,
            out int minX,
            out int maxX,
            out int minZ,
            out int maxZ)
        {
            float normalizedMinX =
                (centre.x -
                 radius -
                 geology.worldOrigin.x) /
                geology.worldSize.x;

            float normalizedMaxX =
                (centre.x +
                 radius -
                 geology.worldOrigin.x) /
                geology.worldSize.x;

            float normalizedMinZ =
                (centre.z -
                 radius -
                 geology.worldOrigin.z) /
                geology.worldSize.z;

            float normalizedMaxZ =
                (centre.z +
                 radius -
                 geology.worldOrigin.z) /
                geology.worldSize.z;

            minX =
                Mathf.Clamp(
                    Mathf.FloorToInt(
                        normalizedMinX *
                        (geology.resolutionX - 1)),
                    0,
                    geology.resolutionX - 1);

            maxX =
                Mathf.Clamp(
                    Mathf.CeilToInt(
                        normalizedMaxX *
                        (geology.resolutionX - 1)),
                    0,
                    geology.resolutionX - 1);

            minZ =
                Mathf.Clamp(
                    Mathf.FloorToInt(
                        normalizedMinZ *
                        (geology.resolutionZ - 1)),
                    0,
                    geology.resolutionZ - 1);

            maxZ =
                Mathf.Clamp(
                    Mathf.CeilToInt(
                        normalizedMaxZ *
                        (geology.resolutionZ - 1)),
                    0,
                    geology.resolutionZ - 1);
        }

        private static float CellDistance(
            BoomtownGeologyData geology,
            int x,
            int z,
            Vector3 centre)
        {
            float nx =
                x /
                (float)(
                    geology.resolutionX - 1);

            float nz =
                z /
                (float)(
                    geology.resolutionZ - 1);

            float worldX =
                geology.worldOrigin.x +
                nx * geology.worldSize.x;

            float worldZ =
                geology.worldOrigin.z +
                nz * geology.worldSize.z;

            return Vector2.Distance(
                new Vector2(
                    worldX,
                    worldZ),
                new Vector2(
                    centre.x,
                    centre.z));
        }

        private static float GetHistoricallyReleasedGold(
            BoomtownGeologyData geology)
        {
            float total = 0f;

            if (geology.quartzVeins == null)
            {
                return total;
            }

            foreach (QuartzVeinData vein
                     in geology.quartzVeins)
            {
                if (vein != null)
                {
                    total +=
                        Mathf.Max(
                            0f,
                            vein.historicallyReleasedGoldOunces);
                }
            }

            return total;
        }

        private static int CountPositiveCells(
            float[] values)
        {
            if (values == null)
            {
                return 0;
            }

            int count = 0;

            foreach (float value in values)
            {
                if (value >
                    MinimumCellOunces)
                {
                    count++;
                }
            }

            return count;
        }

        private static RiverData LoadRiverData(
            BoomtownMapDefinition mapDefinition)
        {
            if (mapDefinition == null)
            {
                return null;
            }

            string safeMapName =
                MakeSafeName(
                    mapDefinition.mapName);

            string path =
                $"{GeneratedFolder}/" +
                $"{safeMapName}_{mapDefinition.year}_RiverData.asset";

            return AssetDatabase.LoadAssetAtPath<
                RiverData>(path);
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

        private static string MakeSafeName(
            string value)
        {
            return string.IsNullOrWhiteSpace(
                    value)
                ? "Map"
                : value
                    .Trim()
                    .Replace(
                        " ",
                        string.Empty);
        }

        private static void EnsureGeneratedFolderExists()
        {
            string[] parts =
            {
                "Assets",
                "Boomtown",
                "Scripts",
                "WorldGeneration",
                "Generated"
            };

            string current =
                parts[0];

            for (int index = 1;
                 index < parts.Length;
                 index++)
            {
                string next =
                    current +
                    "/" +
                    parts[index];

                if (!AssetDatabase.IsValidFolder(
                        next))
                {
                    AssetDatabase.CreateFolder(
                        current,
                        parts[index]);
                }

                current = next;
            }
        }
    }
}
