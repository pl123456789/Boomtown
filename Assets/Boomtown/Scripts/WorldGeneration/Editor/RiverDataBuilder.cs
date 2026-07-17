using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Boomtown.WorldGeneration.Editor
{
    /// <summary>
    /// Creates or replaces the generated RiverData asset for a map.
    ///
    /// This class does not decide the river shape. It prepares river-network
    /// metadata and Level 1 hydrology values, then saves the RiverSample list
    /// produced by the river generator.
    /// </summary>
    public static class RiverDataBuilder
    {
        private const string GeneratedFolder =
            "Assets/Boomtown/Scripts/WorldGeneration/Generated";

        public static RiverData Save(
            BoomtownMapDefinition mapDefinition,
            IReadOnlyList<RiverSample> samples)
        {
            if (mapDefinition == null)
            {
                Debug.LogError(
                    "[River Data Builder] No map definition supplied.");

                return null;
            }

            if (samples == null || samples.Count == 0)
            {
                Debug.LogError(
                    "[River Data Builder] No river samples supplied.");

                return null;
            }

            EnsureGeneratedFolderExists();

            string safeMapName =
                MakeSafeName(mapDefinition.mapName);

            string assetPath =
                $"{GeneratedFolder}/" +
                $"{safeMapName}_{mapDefinition.year}_RiverData.asset";

            RiverData oldAsset =
                AssetDatabase.LoadAssetAtPath<RiverData>(
                    assetPath);

            if (oldAsset != null)
            {
                AssetDatabase.DeleteAsset(assetPath);
            }

            List<RiverSample> networkSamples =
                BuildNetworkSamples(samples);

            RiverData riverData =
                ScriptableObject.CreateInstance<RiverData>();

            riverData.samples = networkSamples;

            AssetDatabase.CreateAsset(
                riverData,
                assetPath);

            EditorUtility.SetDirty(riverData);
            AssetDatabase.SaveAssets();

            Debug.Log(
                $"[River Data Builder] Saved {networkSamples.Count} connected " +
                $"river samples for {mapDefinition.mapName}, " +
                $"{mapDefinition.year}. River length: " +
                $"{networkSamples[networkSamples.Count - 1].distanceDownstream:F1} m.");

            return riverData;
        }

        private static List<RiverSample> BuildNetworkSamples(
            IReadOnlyList<RiverSample> sourceSamples)
        {
            List<RiverSample> networkSamples =
                new List<RiverSample>(sourceSamples.Count);

            float distanceDownstream = 0f;

            for (int index = 0;
                 index < sourceSamples.Count;
                 index++)
            {
                RiverSample sample = sourceSamples[index];

                if (index > 0)
                {
                    distanceDownstream +=
                        Vector3.Distance(
                            sourceSamples[index - 1].position,
                            sample.position);
                }

                sample.sampleIndex = index;
                sample.previousSampleIndex =
                    index > 0 ? index - 1 : -1;
                sample.nextSampleIndex =
                    index < sourceSamples.Count - 1 ? index + 1 : -1;
                sample.distanceDownstream = distanceDownstream;
                sample.slope =
                    CalculateDownhillSlope(
                        sourceSamples,
                        index);

                networkSamples.Add(sample);
            }

            return networkSamples;
        }

        private static float CalculateDownhillSlope(
            IReadOnlyList<RiverSample> samples,
            int index)
        {
            if (samples.Count < 2)
            {
                return 0f;
            }

            int upstreamIndex =
                Mathf.Max(0, index - 1);
            int downstreamIndex =
                Mathf.Min(samples.Count - 1, index + 1);

            Vector3 upstream =
                samples[upstreamIndex].position;
            Vector3 downstream =
                samples[downstreamIndex].position;

            Vector2 upstreamHorizontal =
                new Vector2(upstream.x, upstream.z);
            Vector2 downstreamHorizontal =
                new Vector2(downstream.x, downstream.z);

            float horizontalDistance =
                Vector2.Distance(
                    upstreamHorizontal,
                    downstreamHorizontal);

            if (horizontalDistance <= 0.001f)
            {
                return 0f;
            }

            float elevationDrop =
                upstream.y - downstream.y;

            return Mathf.Max(
                0f,
                elevationDrop / horizontalDistance);
        }

        private static string MakeSafeName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "Map";
            }

            return value.Trim().Replace(" ", string.Empty);
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
    }
}
