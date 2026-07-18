using UnityEngine;

namespace Boomtown.WorldGeneration
{
    public enum TreeSpecies
    {
        DouglasFir = 0,
        WesternRedCedar = 1,
        WesternHemlock = 2,
        LodgepolePine = 3,
        EngelmannSpruce = 4
    }

    /// <summary>
    /// Stores inspectable forestry values for one generated tree.
    /// Board feet are a gameplay estimate, not a professional timber cruise.
    /// </summary>
    public class TreeResource : MonoBehaviour
    {
        [Header("Tree Identity")]

        [Tooltip("Regional tree species used for appearance and timber values.")]
        public TreeSpecies species;

        [Tooltip("Approximate tree age in years.")]
        [Min(1)]
        public int ageYears;

        [Tooltip("Generated standing height in metres.")]
        [Min(0f)]
        public float heightMetres;

        [Tooltip("Approximate diameter at breast height in centimetres.")]
        [Min(0f)]
        public float diameterCentimetres;

        [Header("Harvest Estimate")]

        [Tooltip("Usable straight-stem length in metres.")]
        [Min(0f)]
        public float merchantableLengthMetres;

        [Tooltip("Estimated sawn lumber yield in board feet.")]
        [Min(0f)]
        public float boardFeet;

        [Tooltip("Normalized tree quality. Lower quality reduces usable lumber.")]
        [Range(0f, 1f)]
        public float timberQuality = 1f;
    }
}
