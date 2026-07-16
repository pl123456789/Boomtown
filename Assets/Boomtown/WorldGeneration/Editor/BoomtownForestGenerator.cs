using System;
using UnityEditor;
using UnityEngine;

namespace Boomtown.WorldGeneration.Editor
{
    /// <summary>
    /// Generates a deterministic placeholder forest as ordinary prefab instances.
    ///
    /// We are intentionally NOT using Unity Terrain tree instances in this
    /// prototype version because Unity 6 requires special tree shaders for
    /// terrain billboarding. Normal prefab instances are easier to verify and
    /// can later be replaced by a production vegetation system.
    /// </summary>
    public static class BoomtownForestGenerator
    {
        private const string GeneratedFolder =
            "Assets/Boomtown/WorldGeneration/Generated";

        private const string TreePrefabPath =
            GeneratedFolder + "/BT_PrototypeConifer.prefab";

        private const string FoliageMaterialPath =
            GeneratedFolder + "/BT_TreeFoliage.mat";

        // Keep the first verified test light.
        private const int MaximumTreeCount = 600;

        public static void Generate(
            Terrain terrain,
            BoomtownMapDefinition mapDefinition)
        {
            if (terrain == null || terrain.terrainData == null)
            {
                Debug.LogError(
                    "[Boomtown Forest Generator] No valid Terrain supplied.");
                return;
            }

            if (mapDefinition == null)
            {
                Debug.LogError(
                    "[Boomtown Forest Generator] No map definition supplied.");
                return;
            }

            EnsureGeneratedFolderExists();

            // Remove Terrain-tree data left over from the earlier attempt.
            terrain.terrainData.treeInstances = Array.Empty<TreeInstance>();
            terrain.terrainData.treePrototypes = Array.Empty<TreePrototype>();

            GameObject treePrefab = RebuildPrototypeTree();

            string forestName =
                $"GeneratedForest_{MakeSafeName(mapDefinition.mapName)}";

            GameObject oldForest = GameObject.Find(forestName);

            if (oldForest != null)
            {
                UnityEngine.Object.DestroyImmediate(oldForest);
            }

            GameObject forestRoot = new GameObject(forestName);

            int seed = StableHash(mapDefinition.worldSeed + "_FOREST");
            System.Random random = new System.Random(seed);

            int requestedTreeCount = Mathf.RoundToInt(
                MaximumTreeCount *
                Mathf.Clamp01(mapDefinition.forestDensity));

            TerrainData terrainData = terrain.terrainData;
            Vector3 terrainPosition = terrain.transform.position;

            int created = 0;
            int attempts = 0;
            int maximumAttempts = requestedTreeCount * 20;

            while (created < requestedTreeCount &&
                   attempts < maximumAttempts)
            {
                attempts++;

                float normalizedX = (float)random.NextDouble();
                float normalizedZ = (float)random.NextDouble();

                // Leave the centre corridor open for the future Fraser River,
                // wagon road, gravel bars, and Hope settlement.
                if (Mathf.Abs(normalizedX - 0.5f) < 0.08f)
                {
                    continue;
                }

                float slope =
                    terrainData.GetSteepness(
                        normalizedX,
                        normalizedZ);

                if (slope > 38f)
                {
                    continue;
                }

                float patchNoise = Mathf.PerlinNoise(
                    normalizedX * 8f + seed * 0.0001f,
                    normalizedZ * 8f + seed * 0.0001f);

                if (patchNoise < 0.38f)
                {
                    continue;
                }

                float worldX =
                    terrainPosition.x +
                    normalizedX * terrainData.size.x;

                float worldZ =
                    terrainPosition.z +
                    normalizedZ * terrainData.size.z;

                float worldY =
                    terrain.SampleHeight(
                        new Vector3(worldX, 0f, worldZ)) +
                    terrainPosition.y;

                GameObject tree =
                    (GameObject)PrefabUtility.InstantiatePrefab(
                        treePrefab);

                tree.transform.SetParent(forestRoot.transform);
                tree.transform.position =
                    new Vector3(worldX, worldY, worldZ);

                float scale = Mathf.Lerp(
                    0.8f,
                    1.35f,
                    (float)random.NextDouble());

                tree.transform.localScale =
                    new Vector3(scale, scale, scale);

                tree.transform.rotation =
                    Quaternion.Euler(
                        0f,
                        Mathf.Lerp(
                            0f,
                            360f,
                            (float)random.NextDouble()),
                        0f);

                created++;
            }

            Selection.activeGameObject = forestRoot;

            EditorUtility.SetDirty(terrain.terrainData);
            EditorUtility.SetDirty(forestRoot);

            AssetDatabase.SaveAssets();

            Debug.Log(
                $"[Boomtown Forest Generator] Created {created} visible " +
                $"placeholder tree GameObjects using seed " +
                $"{mapDefinition.worldSeed}.");
        }

        private static GameObject RebuildPrototypeTree()
        {
            AssetDatabase.DeleteAsset(TreePrefabPath);

            Material foliageMaterial = GetOrCreateMaterial(
                FoliageMaterialPath,
                new Color32(62, 92, 55, 255)); // #3E5C37

            // One capsule is enough for a visible placeholder tree.
            // Its renderer is standard URP and does not require a tree shader.
            GameObject tree =
                GameObject.CreatePrimitive(PrimitiveType.Capsule);

            tree.name = "BT_PrototypeConifer";

            // Approximate placeholder dimensions:
            // width 2.4 m, height 7 m.
            tree.transform.localScale =
                new Vector3(1.2f, 3.5f, 1.2f);

            UnityEngine.Object.DestroyImmediate(
                tree.GetComponent<Collider>());

            tree.GetComponent<MeshRenderer>().sharedMaterial =
                foliageMaterial;

            GameObject savedPrefab =
                PrefabUtility.SaveAsPrefabAsset(
                    tree,
                    TreePrefabPath);

            UnityEngine.Object.DestroyImmediate(tree);

            return savedPrefab;
        }

        private static Material GetOrCreateMaterial(
            string path,
            Color32 colour)
        {
            Material material =
                AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material == null)
            {
                Shader shader =
                    Shader.Find("Universal Render Pipeline/Lit");

                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }

                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = colour;
            EditorUtility.SetDirty(material);

            return material;
        }

        private static int StableHash(string text)
        {
            unchecked
            {
                int hash = 23;

                foreach (char character in text ?? string.Empty)
                {
                    hash = hash * 31 + character;
                }

                return hash;
            }
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

            if (!AssetDatabase.IsValidFolder(GeneratedFolder))
            {
                AssetDatabase.CreateFolder(root, "Generated");
            }
        }
    }
}