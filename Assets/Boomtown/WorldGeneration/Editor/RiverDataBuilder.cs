using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Boomtown.WorldGeneration.Editor
{
    /// <summary>
    /// Creates or replaces the generated RiverData asset for a map.
    ///
    /// This class does not decide the river shape. It only saves the physical
    /// RiverSample list produced by the river generator.
    /// </summary>
    public static class RiverDataBuilder
    {
        private const string GeneratedFolder =
            "Assets/Boomtown/WorldGeneration/Generated";

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

            RiverData riverData =
                ScriptableObject.CreateInstance<RiverData>();

            riverData.samples =
                new List<RiverSample>(samples);

            AssetDatabase.CreateAsset(
                riverData,
                assetPath);

            EditorUtility.SetDirty(riverData);
            AssetDatabase.SaveAssets();

            Debug.Log(
                $"[River Data Builder] Saved {samples.Count} river samples " +
                $"for {mapDefinition.mapName}, {mapDefinition.year}.");

            return riverData;
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
                "Assets/Boomtown/WorldGeneration";

            if (!AssetDatabase.IsValidFolder(root))
            {
                AssetDatabase.CreateFolder(
                    "Assets/Boomtown",
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