using UnityEngine;

namespace Boomtown.WorldGeneration
{
    /// <summary>
    /// Defines one Boomtown map.
    ///
    /// This contains information about a historical location.
    /// The World Generator reads one of these and builds the world.
    /// </summary>
    [CreateAssetMenu(
        fileName = "New Map Definition",
        menuName = "Boomtown/World/Map Definition")]
    public class BoomtownMapDefinition : ScriptableObject
    {
        [Header("General")]

        [Tooltip("Display name shown in menus.")]
        public string mapName = "Hope";

        [Tooltip("Province or State.")]
        public string region = "British Columbia";

        [Tooltip("Historical year.")]
        public int year = 1858;

        [Tooltip("Random generation seed.")]
        public string worldSeed = "HOPE1858";

        [Header("Terrain")]

        [Tooltip("Raw DEM filename.")]
        public string demFile = "";

        [Tooltip("Generated Unity heightmap filename.")]
        public string heightmapFile = "";

        [Header("Nature")]

        [Range(0f,1f)]
        public float forestDensity = 0.85f;

        [Range(0f,1f)]
        public float rockDensity = 0.35f;

        [Range(0f,1f)]
        public float bushDensity = 0.60f;

        [Range(0f,1f)]
        public float grassDensity = 0.95f;

        [Header("Gameplay")]

        public bool generateRoads = true;
        public bool generateRivers = true;
        public bool generateForests = true;
        public bool generateSettlements = true;
    }
}