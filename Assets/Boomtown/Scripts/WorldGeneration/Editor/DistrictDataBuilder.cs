using UnityEditor;
using UnityEngine;

namespace Boomtown.WorldGeneration.Editor
{
    public static class DistrictDataBuilder
    {
        private const string GeneratedFolder =
            "Assets/Boomtown/Scripts/WorldGeneration/Generated";

        public static DistrictData CreateOrReset(
            BoomtownMapDefinition definition,
            string generationId)
        {
            EnsureGeneratedFolderExists();

            string safeName =
                MakeSafeName(
                    definition.mapName);

            string assetPath =
                $"{GeneratedFolder}/" +
                $"{safeName}_{definition.year}_DistrictData.asset";

            DistrictData data =
                AssetDatabase.LoadAssetAtPath<DistrictData>(
                    assetPath);

            if (data == null)
            {
                data =
                    ScriptableObject.CreateInstance<
                        DistrictData>();

                AssetDatabase.CreateAsset(
                    data,
                    assetPath);
            }

            data.ResetForGeneration(
                definition,
                generationId);

            EditorUtility.SetDirty(
                data);

            AssetDatabase.SaveAssets();

            return data;
        }

        private static string MakeSafeName(
            string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? "District"
                : value.Trim().Replace(" ", string.Empty);
        }

        private static void EnsureGeneratedFolderExists()
        {
            string[] parts =
            {
                "Assets",
                "Boomtown",
                "Scripts",
                "WorldGeneration",
                "Generated"
            };

            string current = parts[0];

            for (int i = 1;
                 i < parts.Length;
                 i++)
            {
                string next =
                    current + "/" + parts[i];

                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(
                        current,
                        parts[i]);
                }

                current = next;
            }
        }
    }
}
