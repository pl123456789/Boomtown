using System;
using System.Collections.Generic;
using UnityEngine;

namespace Boomtown.WorldGeneration
{
    public enum DistrictGenerationStage
    {
        NotStarted,
        Preparing,
        Terrain,
        Hydrology,
        TerrainPainting,
        Ecology,
        Population,
        Geology,
        Validation,
        Complete,
        Failed
    }

    [Serializable]
    public sealed class DistrictModuleReport
    {
        public string moduleName;
        public bool completed;
        public string message;
        public double durationMilliseconds;
    }

    /// <summary>
    /// Persistent generated-world record. This is the shared identity and
    /// report for one procedural district. Runtime gameplay can query it
    /// without knowing how editor generation was performed.
    /// </summary>
    [CreateAssetMenu(
        fileName = "New District Data",
        menuName = "Boomtown/World Generation/District Data")]
    public sealed class DistrictData : ScriptableObject
    {
        [Header("Identity")]
        public string districtName;
        public string region;
        public int historicalYear;
        public string worldSeed;
        public string generationId;
        public string generatedUtc;

        [Header("State")]
        public DistrictGenerationStage stage;
        public bool generationSucceeded;
        [TextArea(2, 5)]
        public string validationSummary;

        [Header("Generated Assets")]
        public TerrainData terrainData;
        public RiverData riverData;
        public BoomtownGeologyData geologyData;

        [Header("District Statistics")]
        public int riverSampleCount;
        public int placerDepositCellCount;
        public float initialPlacerGoldOunces;
        public float remainingPlacerGoldOunces;
        public int generatedTreeCount;

        [Header("Future District Systems")]
        [Tooltip("Reserved for the future watershed module.")]
        public int watershedCount;

        [Tooltip("Reserved for future random mineralized mountain systems.")]
        public int mineralizedMountainCount;

        [Tooltip("Reserved for future generated quartz veins.")]
        public int quartzVeinCount;
        public float initialHardRockGoldOunces;
        public float remainingHardRockGoldOunces;

        [Header("Module Report")]
        public List<DistrictModuleReport> modules =
            new List<DistrictModuleReport>();

        public void ResetForGeneration(
            BoomtownMapDefinition definition,
            string newGenerationId)
        {
            districtName =
                definition != null
                    ? definition.mapName
                    : "Unknown District";

            region =
                definition != null
                    ? definition.region
                    : string.Empty;

            historicalYear =
                definition != null
                    ? definition.year
                    : 0;

            worldSeed =
                definition != null
                    ? definition.worldSeed
                    : string.Empty;

            generationId =
                newGenerationId;

            generatedUtc =
                DateTime.UtcNow.ToString("O");

            stage =
                DistrictGenerationStage.Preparing;

            generationSucceeded = false;
            validationSummary = string.Empty;

            terrainData = null;
            riverData = null;
            geologyData = null;

            riverSampleCount = 0;
            placerDepositCellCount = 0;
            initialPlacerGoldOunces = 0f;
            remainingPlacerGoldOunces = 0f;
            generatedTreeCount = 0;

            watershedCount = 0;
            mineralizedMountainCount = 0;
            quartzVeinCount = 0;
            initialHardRockGoldOunces = 0f;
            remainingHardRockGoldOunces = 0f;

            modules.Clear();
        }

        public void AddModuleReport(
            string moduleName,
            bool completed,
            string message,
            double durationMilliseconds)
        {
            modules.Add(
                new DistrictModuleReport
                {
                    moduleName = moduleName,
                    completed = completed,
                    message = message,
                    durationMilliseconds =
                        durationMilliseconds
                });
        }
    }
}
