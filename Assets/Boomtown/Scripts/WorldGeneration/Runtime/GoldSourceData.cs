using System;
using System.Collections.Generic;
using UnityEngine;

namespace Boomtown.WorldGeneration
{
    [Serializable]
    public sealed class GoldMountainData
    {
        public int id;
        public string displayName;

        public Vector3 centre;
        public float radius;
        public float elevation;

        [Range(0f, 1f)]
        public float mineralization;

        public float totalHardRockGoldOunces;
        public float remainingHardRockGoldOunces;
        public float historicallyWeatheredGoldOunces;

        public List<int> quartzVeinIds =
            new List<int>();
    }

    [Serializable]
    public sealed class QuartzVeinData
    {
        public int id;
        public int mountainId;

        public Vector3 start;
        public Vector3 end;

        public float widthMetres;
        public float depthMetres;

        [Tooltip("Gold grade in troy ounces per short ton of ore.")]
        public float gradeOuncesPerTon;

        public float initialGoldOunces;
        public float remainingGoldOunces;

        [Range(0f, 1f)]
        public float exposedFraction;

        [Range(0f, 1f)]
        public float historicalWeatheredFraction;

        [Header("Placer Transport")]
        public float historicallyReleasedGoldOunces;
        public int riverEntrySampleIndex;
        public Vector3 riverEntryPosition;

        [Range(0f, 1f)]
        public float coarseGoldFraction;

        [Range(0f, 1f)]
        public float fineGoldFraction;

        [Range(0f, 1f)]
        public float flourGoldFraction;
    }
}
