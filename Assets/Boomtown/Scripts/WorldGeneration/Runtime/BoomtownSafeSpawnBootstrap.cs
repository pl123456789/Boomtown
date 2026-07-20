using System.Collections.Generic;
using UnityEngine;

namespace Boomtown.WorldGeneration
{
    internal static class BoomtownSafeSpawnBootstrap
    {
        private const float CharacterHeightOffset = 1.1f;
        private const float MaximumSlope = 14f;
        private const float RiverClearance = 24f;
        private const float MinimumHeightGain = 2.5f;
        private const float CompanionSpacing = 2.2f;

        // A blind procedural search (TryFindSafePoint) has never had eyes
        // on the result, so it earns a generous 24m margin. A position the
        // scene already placed a character at -- like Hope's hand-authored
        // town square -- has already been screenshot-verified as dry, flat
        // ground, so it only needs to clear a smaller sanity-check margin,
        // not be re-litigated against the same conservative bar.
        private const float ExistingPlacementRiverClearance = 12f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void PlaceStartingCharactersOnSafeGround()
        {
            GameObject bill = GameObject.Find("Bill");
            GameObject ted = GameObject.Find("Ted");
            Terrain terrain = Terrain.activeTerrain ?? Object.FindFirstObjectByType<Terrain>();

            if (bill == null || terrain == null)
            {
                return;
            }

            List<Vector3> riverPoints = CollectRiverMeshPoints();

            // Hand-authored maps (Hope, and later Yale) already place Bill
            // and Ted at a deliberate spawn point -- the town square, not
            // wherever a generic heuristic thinks is "safest." Only search
            // for an alternate spot if the scene's own placement actually
            // fails the same safety bar a procedurally generated map would
            // need to pass. This keeps the bootstrap a safety net for
            // future procedural maps instead of an override that fights
            // curated ones.
            if (IsPositionSafe(terrain, bill.transform.position, riverPoints))
            {
                return;
            }

            Vector3 originalCentre = ted == null
                ? bill.transform.position
                : Vector3.Lerp(bill.transform.position, ted.transform.position, 0.5f);

            Vector3 safePoint;

            if (!TryFindSafePoint(terrain, originalCentre, riverPoints, out safePoint))
            {
                Debug.LogWarning(
                    "[World Generation] Could not find a safer starting location. " +
                    "Bill and Ted were left at their scene positions.");
                return;
            }

            Vector3 facing = FindDirectionAwayFromRiver(safePoint, riverPoints);
            Vector3 right = Vector3.Cross(Vector3.up, facing).normalized;

            PlaceCharacter(bill.transform, terrain, safePoint - right * CompanionSpacing * 0.5f, facing);

            if (ted != null)
            {
                PlaceCharacter(ted.transform, terrain, safePoint + right * CompanionSpacing * 0.5f, facing);
            }

            Debug.Log(
                $"[World Generation] Starting characters moved to safe ground at {safePoint}.");
        }

        /// <summary>
        /// Same bar a candidate point has to clear in TryFindSafePoint
        /// (in bounds, gentle enough slope, clear of the river), checked
        /// against wherever a character already is instead of searching
        /// outward from it.
        /// </summary>
        private static bool IsPositionSafe(
            Terrain terrain,
            Vector3 position,
            IReadOnlyList<Vector3> riverPoints)
        {
            if (!IsInsideTerrain(terrain, position))
            {
                return false;
            }

            float slope = SampleSlope(terrain, position);
            float riverDistance = DistanceToRiver(position, riverPoints);

            return slope <= MaximumSlope &&
                   riverDistance >= ExistingPlacementRiverClearance;
        }

        private static bool TryFindSafePoint(
            Terrain terrain,
            Vector3 origin,
            IReadOnlyList<Vector3> riverPoints,
            out Vector3 bestPoint)
        {
            bestPoint = default;
            float originalGround = SampleGroundHeight(terrain, origin);
            float bestScore = float.MinValue;
            bool found = false;

            for (float radius = 12f; radius <= 220f; radius += 8f)
            {
                int sampleCount = Mathf.Max(16, Mathf.CeilToInt(radius * 0.35f));

                for (int index = 0; index < sampleCount; index++)
                {
                    float angle = index / (float)sampleCount * Mathf.PI * 2f;
                    Vector3 candidate = origin + new Vector3(
                        Mathf.Cos(angle) * radius,
                        0f,
                        Mathf.Sin(angle) * radius);

                    if (!IsInsideTerrain(terrain, candidate))
                    {
                        continue;
                    }

                    float groundHeight = SampleGroundHeight(terrain, candidate);
                    float slope = SampleSlope(terrain, candidate);
                    float riverDistance = DistanceToRiver(candidate, riverPoints);

                    if (slope > MaximumSlope || riverDistance < RiverClearance)
                    {
                        continue;
                    }

                    float heightGain = groundHeight - originalGround;
                    if (riverPoints.Count > 0 && heightGain < MinimumHeightGain)
                    {
                        continue;
                    }

                    float preferredRiverDistance = riverPoints.Count == 0
                        ? 0f
                        : Mathf.Abs(riverDistance - 38f);

                    float score =
                        -radius * 0.45f -
                        slope * 2.5f -
                        preferredRiverDistance * 0.35f +
                        Mathf.Clamp(heightGain, 0f, 20f) * 4f;

                    if (score <= bestScore)
                    {
                        continue;
                    }

                    bestScore = score;
                    bestPoint = new Vector3(candidate.x, groundHeight, candidate.z);
                    found = true;
                }

                if (found && radius >= 40f)
                {
                    break;
                }
            }

            return found;
        }

        private static void PlaceCharacter(
            Transform character,
            Terrain terrain,
            Vector3 position,
            Vector3 facing)
        {
            position.y = SampleGroundHeight(terrain, position) + CharacterHeightOffset;
            character.SetPositionAndRotation(
                position,
                Quaternion.LookRotation(facing, Vector3.up));
        }

        private static List<Vector3> CollectRiverMeshPoints()
        {
            List<Vector3> points = new List<Vector3>();
            GameObject rivers = GameObject.Find("Rivers");

            if (rivers == null)
            {
                return points;
            }

            MeshFilter[] meshFilters = rivers.GetComponentsInChildren<MeshFilter>(true);

            foreach (MeshFilter meshFilter in meshFilters)
            {
                Mesh mesh = meshFilter.sharedMesh;
                if (mesh == null)
                {
                    continue;
                }

                Vector3[] vertices = mesh.vertices;
                int stride = Mathf.Max(1, vertices.Length / 600);

                for (int index = 0; index < vertices.Length; index += stride)
                {
                    points.Add(meshFilter.transform.TransformPoint(vertices[index]));
                }
            }

            return points;
        }

        private static Vector3 FindDirectionAwayFromRiver(
            Vector3 position,
            IReadOnlyList<Vector3> riverPoints)
        {
            if (riverPoints.Count == 0)
            {
                return Vector3.forward;
            }

            Vector3 nearest = riverPoints[0];
            float nearestDistance = float.MaxValue;

            for (int index = 0; index < riverPoints.Count; index++)
            {
                Vector3 delta = riverPoints[index] - position;
                delta.y = 0f;
                float distance = delta.sqrMagnitude;

                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = riverPoints[index];
                }
            }

            Vector3 direction = position - nearest;
            direction.y = 0f;
            return direction.sqrMagnitude > 0.01f
                ? direction.normalized
                : Vector3.forward;
        }

        private static float DistanceToRiver(
            Vector3 position,
            IReadOnlyList<Vector3> riverPoints)
        {
            if (riverPoints.Count == 0)
            {
                return float.MaxValue;
            }

            float closestSquared = float.MaxValue;

            for (int index = 0; index < riverPoints.Count; index++)
            {
                Vector3 delta = riverPoints[index] - position;
                delta.y = 0f;
                closestSquared = Mathf.Min(closestSquared, delta.sqrMagnitude);
            }

            return Mathf.Sqrt(closestSquared);
        }

        private static float SampleGroundHeight(Terrain terrain, Vector3 worldPosition)
        {
            return terrain.SampleHeight(worldPosition) + terrain.transform.position.y;
        }

        private static float SampleSlope(Terrain terrain, Vector3 worldPosition)
        {
            TerrainData data = terrain.terrainData;
            Vector3 local = worldPosition - terrain.transform.position;
            float normalizedX = Mathf.Clamp01(local.x / data.size.x);
            float normalizedZ = Mathf.Clamp01(local.z / data.size.z);
            return data.GetSteepness(normalizedX, normalizedZ);
        }

        private static bool IsInsideTerrain(Terrain terrain, Vector3 worldPosition)
        {
            Vector3 minimum = terrain.transform.position;
            Vector3 maximum = minimum + terrain.terrainData.size;

            return worldPosition.x >= minimum.x &&
                   worldPosition.x <= maximum.x &&
                   worldPosition.z >= minimum.z &&
                   worldPosition.z <= maximum.z;
        }
    }
}
