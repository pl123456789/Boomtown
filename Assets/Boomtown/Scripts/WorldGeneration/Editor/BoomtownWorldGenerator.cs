using System;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using Boomtown.Gameplay.Prospecting;
using Debug = UnityEngine.Debug;

namespace Boomtown.WorldGeneration.Editor
{
    /// <summary>
    /// Boomtown World Generation 2.0 coordinator.
    ///
    /// Existing proven generators remain focused on their current jobs.
    /// This class owns district identity, stage tracking, generated data,
    /// validation, and deterministic replay behaviour.
    /// </summary>
    public static class BoomtownWorldGenerator
    {
        private const string GeneratedFolder =
            "Assets/Boomtown/Scripts/WorldGeneration/Generated";

        public static Terrain GenerateWorld(
            BoomtownMapDefinition mapDefinition)
        {
            if (mapDefinition == null)
            {
                Debug.LogError(
                    "[Boomtown World Generator] No map definition selected.");

                return null;
            }

            string generationId =
                Guid.NewGuid().ToString("N");

            DistrictData districtData =
                DistrictDataBuilder.CreateOrReset(
                    mapDefinition,
                    generationId);

            BoomtownWorldGenerationContext context =
                new BoomtownWorldGenerationContext(
                    mapDefinition,
                    districtData);

            districtData.generationId =
                context.GenerationId;

            Debug.Log(
                $"[Boomtown World Generator 2.0] Beginning district " +
                $"{mapDefinition.mapName}, {mapDefinition.region}, " +
                $"{mapDefinition.year}. Seed: {mapDefinition.worldSeed}.");

            try
            {
                districtData.stage =
                    DistrictGenerationStage.Terrain;

                Terrain terrain =
                    RunModule(
                        context,
                        "Terrain, river, paint, forest and spawn",
                        () =>
                            BoomtownTerrainGenerator.Generate(
                                mapDefinition),
                        result =>
                            result != null,
                        "Generated the playable terrain and currently " +
                        "connected world modules.");

                if (terrain == null)
                {
                    Fail(
                        context,
                        "Terrain generation failed.");

                    return null;
                }

                context.Terrain = terrain;
                districtData.terrainData =
                    terrain.terrainData;

                districtData.stage =
                    DistrictGenerationStage.Hydrology;

                context.RiverData =
                    LoadGeneratedRiverData(
                        mapDefinition);

                districtData.riverData =
                    context.RiverData;

                districtData.riverSampleCount =
                    context.RiverData?.samples?.Count ??
                    0;

                districtData.stage =
                    DistrictGenerationStage.Geology;

                context.GeologyData =
                    RunModule(
                        context,
                        "Geology and finite gold",
                        () =>
                            BoomtownGeologyGenerator.Generate(
                                terrain,
                                mapDefinition,
                                context.RiverData),
                        result =>
                            result != null &&
                            result.IsValid,
                        "Generated source geology and finite exhaustible " +
                        "placer inventory.");

                districtData.geologyData =
                    context.GeologyData;

                RunModule(
                    context,
                    "Scene wiring",
                    () =>
                    {
                        AssignGeneratedRiverDataToProspectingSensors(
                            context.RiverData);

                        BoomtownProspectingComponentRepair
                            .RepairSilentlyAfterGeneration(
                                context.GeologyData);

                        AssignGeneratedGeologyToProspectors(
                            context.GeologyData);

                        RefreshOpenDebugWindows();

                        return true;
                    },
                    result => result,
                    "Automatically connected RiverData, GeologyData, " +
                    "prospecting controllers and editor debug references.");

                PopulateStatistics(
                    context);

                districtData.stage =
                    DistrictGenerationStage.Validation;

                Stopwatch validationTimer =
                    Stopwatch.StartNew();

                bool valid =
                    BoomtownWorldValidator.Validate(
                        context,
                        out string validationSummary);

                validationTimer.Stop();

                districtData.AddModuleReport(
                    "Validation",
                    valid,
                    validationSummary,
                    validationTimer.Elapsed
                        .TotalMilliseconds);

                Selection.activeGameObject =
                    terrain.gameObject;

                EditorUtility.SetDirty(
                    districtData);

                AssetDatabase.SaveAssets();

                Debug.Log(
                    $"[Boomtown World Generator 2.0] " +
                    $"{(valid ? "Finished" : "Finished with errors")} " +
                    $"{mapDefinition.mapName}, {mapDefinition.year}.\n" +
                    validationSummary);

                return terrain;
            }
            catch (Exception exception)
            {
                Fail(
                    context,
                    exception.ToString());

                Debug.LogException(
                    exception);

                return null;
            }
        }

        public static string CreateRandomReplaySeed()
        {
            return Guid.NewGuid()
                .ToString("N")
                .Substring(0, 12)
                .ToUpperInvariant();
        }

        private static T RunModule<T>(
            BoomtownWorldGenerationContext context,
            string moduleName,
            Func<T> operation,
            Func<T, bool> successTest,
            string successMessage)
        {
            Stopwatch timer =
                Stopwatch.StartNew();

            T result;

            try
            {
                result = operation();
            }
            catch (Exception exception)
            {
                timer.Stop();

                context.DistrictData.AddModuleReport(
                    moduleName,
                    false,
                    exception.Message,
                    timer.Elapsed.TotalMilliseconds);

                throw;
            }

            timer.Stop();

            bool succeeded =
                successTest(result);

            context.DistrictData.AddModuleReport(
                moduleName,
                succeeded,
                succeeded
                    ? successMessage
                    : moduleName + " failed.",
                timer.Elapsed.TotalMilliseconds);

            EditorUtility.SetDirty(
                context.DistrictData);

            return result;
        }

        private static void AssignGeneratedRiverDataToProspectingSensors(
            RiverData riverData)
        {
            if (riverData == null)
            {
                return;
            }

            MonoBehaviour[] behaviours =
                UnityEngine.Object.FindObjectsByType<
                    MonoBehaviour>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            int assignedCount = 0;

            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour == null ||
                    behaviour.GetType().Name !=
                    "ProspectingLocationSensor")
                {
                    continue;
                }

                SerializedObject serializedObject =
                    new SerializedObject(
                        behaviour);

                SerializedProperty riverProperty =
                    serializedObject.FindProperty(
                        "riverData");

                if (riverProperty == null)
                {
                    riverProperty =
                        serializedObject.FindProperty(
                            "generatedRiverData");
                }

                if (riverProperty == null)
                {
                    Debug.LogWarning(
                        "[Boomtown World Generator 2.0] Found a " +
                        "ProspectingLocationSensor but could not find its " +
                        "serialized RiverData field.",
                        behaviour);

                    continue;
                }

                riverProperty.objectReferenceValue =
                    riverData;

                serializedObject
                    .ApplyModifiedPropertiesWithoutUndo();

                EditorUtility.SetDirty(
                    behaviour);

                assignedCount++;
            }

            Debug.Log(
                $"[Boomtown World Generator 2.0] Assigned generated RiverData " +
                $"to {assignedCount} ProspectingLocationSensor instance(s).");
        }

        private static void AssignGeneratedGeologyToProspectors(
            BoomtownGeologyData geologyData)
        {
            if (geologyData == null) return;

            GoldPanningController[] controllers =
                UnityEngine.Object.FindObjectsByType<GoldPanningController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            foreach (GoldPanningController controller in controllers)
            {
                if (controller == null) continue;
                controller.AssignGeologyData(geologyData);
                EditorUtility.SetDirty(controller);
            }

            Debug.Log(
                $"[Boomtown World Generator 2.0] Assigned generated geology " +
                $"to {controllers.Length} GoldPanningController instance(s).");
        }

        private static void RefreshOpenDebugWindows()
        {
            UnityEditorInternal.InternalEditorUtility
                .RepaintAllViews();
        }

        private static void PopulateStatistics(
            BoomtownWorldGenerationContext context)
        {
            BoomtownGeologyData geology =
                context.GeologyData;

            if (geology != null)
            {
                context.DistrictData
                    .initialPlacerGoldOunces =
                    geology.GetTotalInitialOunces();

                context.DistrictData
                    .remainingPlacerGoldOunces =
                    geology.GetTotalRemainingOunces();

                int depositCells = 0;

                if (geology.placerInitialOunces != null)
                {
                    foreach (float ounces in
                             geology.placerInitialOunces)
                    {
                        if (ounces > 0f)
                        {
                            depositCells++;
                        }
                    }
                }

                context.DistrictData
                    .placerDepositCellCount =
                    depositCells;

                context.DistrictData.mineralizedMountainCount =
                    geology.goldMountains?.Count ?? 0;

                context.DistrictData.quartzVeinCount =
                    geology.quartzVeins?.Count ?? 0;

                context.DistrictData.initialHardRockGoldOunces =
                    geology.GetTotalInitialHardRockOunces();

                context.DistrictData.remainingHardRockGoldOunces =
                    geology.GetTotalRemainingHardRockOunces();
            }

            Transform forestContainer =
                BoomtownWorldHierarchy
                    .GetForestContainer();

            int treeCount = 0;

            if (forestContainer != null)
            {
                for (int i = 0;
                     i < forestContainer.childCount;
                     i++)
                {
                    treeCount +=
                        forestContainer
                            .GetChild(i)
                            .childCount;
                }
            }

            context.DistrictData.generatedTreeCount =
                treeCount;
        }

        private static RiverData LoadGeneratedRiverData(
            BoomtownMapDefinition mapDefinition)
        {
            string safeMapName =
                string.IsNullOrWhiteSpace(
                    mapDefinition.mapName)
                    ? "Map"
                    : mapDefinition.mapName
                        .Trim()
                        .Replace(" ", string.Empty);

            string path =
                $"{GeneratedFolder}/" +
                $"{safeMapName}_{mapDefinition.year}_RiverData.asset";

            RiverData riverData =
                AssetDatabase.LoadAssetAtPath<
                    RiverData>(path);

            if (riverData == null)
            {
                Debug.LogWarning(
                    "[Boomtown World Generator 2.0] RiverData was not found.");
            }

            return riverData;
        }

        private static void Fail(
            BoomtownWorldGenerationContext context,
            string message)
        {
            context.DistrictData.stage =
                DistrictGenerationStage.Failed;

            context.DistrictData
                .generationSucceeded =
                false;

            context.DistrictData
                .validationSummary =
                message;

            EditorUtility.SetDirty(
                context.DistrictData);

            AssetDatabase.SaveAssets();

            Debug.LogError(
                "[Boomtown World Generator 2.0] " +
                message);
        }
    }
}
