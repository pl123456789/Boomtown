using System;
using System.Collections.Generic;
using UnityEngine;

namespace Boomtown.WorldGeneration
{
    [CreateAssetMenu(
        fileName = "BoomtownGeologyData",
        menuName = "Boomtown/World Generation/Geology Data")]
    public sealed class BoomtownGeologyData : ScriptableObject
    {
        public string mapName;
        public int historicalYear;
        public string worldSeed;

        public int resolutionX;
        public int resolutionZ;

        public Vector3 worldOrigin;
        public Vector3 worldSize;

        public float[] bedrockHardness;
        public float[] quartzPotential;

        [Header("Compatibility Placer Field")]
        [Tooltip("Normalized total placer grade from all particle classes.")]
        public float[] placerConcentration;

        [Tooltip("Original finite total gold inventory in troy ounces per cell.")]
        public float[] placerInitialOunces;

        [Tooltip("Current finite total gold inventory in troy ounces per cell.")]
        public float[] placerRemainingOunces;

        [Tooltip("Dominant source quartz-vein ID per cell.")]
        public int[] placerSourceId;

        [Header("Sediment")]
        public float[] sand;
        public float[] gravel;
        public float[] blackSand;

        [Header("Original Gold by Particle Class")]
        public float[] coarseGoldInitialOunces;
        public float[] fineGoldInitialOunces;
        public float[] flourGoldInitialOunces;

        [Header("Remaining Gold by Particle Class")]
        public float[] coarseGoldRemainingOunces;
        public float[] fineGoldRemainingOunces;
        public float[] flourGoldRemainingOunces;

        [Header("Hard-Rock Gold Sources")]
        public List<GoldMountainData> goldMountains =
            new List<GoldMountainData>();

        public List<QuartzVeinData> quartzVeins =
            new List<QuartzVeinData>();

        public int CellCount =>
            Mathf.Max(0, resolutionX) *
            Mathf.Max(0, resolutionZ);

        public bool IsValid =>
            resolutionX > 1 &&
            resolutionZ > 1 &&
            placerRemainingOunces != null &&
            placerRemainingOunces.Length == CellCount;

        public bool HasSedimentLayers =>
            sand != null &&
            gravel != null &&
            blackSand != null &&
            sand.Length == CellCount &&
            gravel.Length == CellCount &&
            blackSand.Length == CellCount;

        public bool TryWorldToCell(
            Vector3 worldPosition,
            out int x,
            out int z,
            out int index)
        {
            x = 0;
            z = 0;
            index = -1;

            if (!IsValid ||
                worldSize.x <= 0f ||
                worldSize.z <= 0f)
            {
                return false;
            }

            float nx =
                (worldPosition.x - worldOrigin.x) /
                worldSize.x;

            float nz =
                (worldPosition.z - worldOrigin.z) /
                worldSize.z;

            if (nx < 0f || nx > 1f ||
                nz < 0f || nz > 1f)
            {
                return false;
            }

            x = Mathf.Clamp(
                Mathf.RoundToInt(
                    nx * (resolutionX - 1)),
                0,
                resolutionX - 1);

            z = Mathf.Clamp(
                Mathf.RoundToInt(
                    nz * (resolutionZ - 1)),
                0,
                resolutionZ - 1);

            index = z * resolutionX + x;
            return true;
        }

        public SedimentSample GetSedimentSample(
            Vector3 worldPosition)
        {
            if (!TryWorldToCell(
                    worldPosition,
                    out _,
                    out _,
                    out int index))
            {
                return default;
            }

            return new SedimentSample
            {
                sand = SafeValue(sand, index),
                gravel = SafeValue(gravel, index),
                blackSand = SafeValue(blackSand, index),
                coarseGoldOunces =
                    SafeValue(coarseGoldRemainingOunces, index),
                fineGoldOunces =
                    SafeValue(fineGoldRemainingOunces, index),
                flourGoldOunces =
                    SafeValue(flourGoldRemainingOunces, index)
            };
        }

        public float GetRemainingOunces(
            Vector3 worldPosition)
        {
            return TryWorldToCell(
                    worldPosition,
                    out _,
                    out _,
                    out int index)
                ? Mathf.Max(
                    0f,
                    placerRemainingOunces[index])
                : 0f;
        }

        public float GetGrade(
            Vector3 worldPosition)
        {
            if (!TryWorldToCell(
                    worldPosition,
                    out _,
                    out _,
                    out int index) ||
                placerConcentration == null ||
                index >= placerConcentration.Length)
            {
                return 0f;
            }

            return Mathf.Clamp01(
                placerConcentration[index]);
        }

        public float Extract(
            Vector3 worldPosition,
            float requestedOunces)
        {
            if (requestedOunces <= 0f ||
                !TryWorldToCell(
                    worldPosition,
                    out _,
                    out _,
                    out int index))
            {
                return 0f;
            }

            float available =
                Mathf.Max(
                    0f,
                    placerRemainingOunces[index]);

            float extracted =
                Mathf.Min(
                    available,
                    requestedOunces);

            if (extracted <= 0f)
            {
                return 0f;
            }

            float coarse =
                SafeValue(
                    coarseGoldRemainingOunces,
                    index);

            float fine =
                SafeValue(
                    fineGoldRemainingOunces,
                    index);

            float flour =
                SafeValue(
                    flourGoldRemainingOunces,
                    index);

            float classTotal =
                coarse + fine + flour;

            if (classTotal > 0f)
            {
                float factor =
                    extracted / classTotal;

                coarseGoldRemainingOunces[index] =
                    Mathf.Max(
                        0f,
                        coarse -
                        coarse * factor);

                fineGoldRemainingOunces[index] =
                    Mathf.Max(
                        0f,
                        fine -
                        fine * factor);

                flourGoldRemainingOunces[index] =
                    Mathf.Max(
                        0f,
                        flour -
                        flour * factor);
            }

            placerRemainingOunces[index] =
                available - extracted;

            return extracted;
        }

        public float GetTotalInitialOunces() =>
            SumPositive(placerInitialOunces);

        public float GetTotalRemainingOunces() =>
            SumPositive(placerRemainingOunces);

        public float GetTotalInitialHardRockOunces()
        {
            float total = 0f;

            if (quartzVeins == null)
            {
                return total;
            }

            foreach (QuartzVeinData vein in quartzVeins)
            {
                if (vein != null)
                {
                    total +=
                        Mathf.Max(
                            0f,
                            vein.initialGoldOunces);
                }
            }

            return total;
        }

        public float GetTotalRemainingHardRockOunces()
        {
            float total = 0f;

            if (quartzVeins == null)
            {
                return total;
            }

            foreach (QuartzVeinData vein in quartzVeins)
            {
                if (vein != null)
                {
                    total +=
                        Mathf.Max(
                            0f,
                            vein.remainingGoldOunces);
                }
            }

            return total;
        }

        public void ResetDepletion()
        {
            CopyArray(
                placerInitialOunces,
                ref placerRemainingOunces);

            CopyArray(
                coarseGoldInitialOunces,
                ref coarseGoldRemainingOunces);

            CopyArray(
                fineGoldInitialOunces,
                ref fineGoldRemainingOunces);

            CopyArray(
                flourGoldInitialOunces,
                ref flourGoldRemainingOunces);
        }

        public void RebuildCompatibilityTotals()
        {
            int count = CellCount;

            if (count <= 0)
            {
                return;
            }

            EnsureArray(
                ref placerInitialOunces,
                count);

            EnsureArray(
                ref placerRemainingOunces,
                count);

            for (int index = 0;
                 index < count;
                 index++)
            {
                placerInitialOunces[index] =
                    SafeValue(
                        coarseGoldInitialOunces,
                        index) +
                    SafeValue(
                        fineGoldInitialOunces,
                        index) +
                    SafeValue(
                        flourGoldInitialOunces,
                        index);

                placerRemainingOunces[index] =
                    SafeValue(
                        coarseGoldRemainingOunces,
                        index) +
                    SafeValue(
                        fineGoldRemainingOunces,
                        index) +
                    SafeValue(
                        flourGoldRemainingOunces,
                        index);
            }
        }

        private static float SafeValue(
            float[] values,
            int index)
        {
            return values != null &&
                   index >= 0 &&
                   index < values.Length
                ? Mathf.Max(0f, values[index])
                : 0f;
        }

        private static void CopyArray(
            float[] source,
            ref float[] destination)
        {
            if (source == null)
            {
                destination = null;
                return;
            }

            destination =
                new float[source.Length];

            Array.Copy(
                source,
                destination,
                source.Length);
        }

        private static void EnsureArray(
            ref float[] values,
            int count)
        {
            if (values == null ||
                values.Length != count)
            {
                values =
                    new float[count];
            }
        }

        private static float SumPositive(
            float[] values)
        {
            if (values == null)
            {
                return 0f;
            }

            float total = 0f;

            foreach (float value in values)
            {
                total += Mathf.Max(0f, value);
            }

            return total;
        }
    }
}
