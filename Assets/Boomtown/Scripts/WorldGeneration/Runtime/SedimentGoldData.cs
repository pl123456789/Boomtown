using System;
using UnityEngine;

namespace Boomtown.WorldGeneration
{
    public enum GoldParticleClass
    {
        Coarse,
        Fine,
        Flour
    }

    [Serializable]
    public struct SedimentSample
    {
        public float sand;
        public float gravel;
        public float blackSand;

        public float coarseGoldOunces;
        public float fineGoldOunces;
        public float flourGoldOunces;

        public float TotalGoldOunces =>
            Mathf.Max(0f, coarseGoldOunces) +
            Mathf.Max(0f, fineGoldOunces) +
            Mathf.Max(0f, flourGoldOunces);
    }
}
