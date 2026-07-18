using System;
using UnityEditor;
using UnityEngine;

namespace Boomtown.WorldGeneration.Editor
{
    /// <summary>
    /// Generates a deterministic lightweight forest as ordinary prefab instances.
    /// Each generated tree receives age, size, timber quality, and board-foot data.
    /// </summary>
    public static class BoomtownForestGenerator
    {
        private const string GeneratedFolder =
            "Assets/Boomtown/Scripts/WorldGeneration/Generated";

        private const string TreePrefabPath =
            GeneratedFolder + "/BT_PrototypeConifer.prefab";

        private const string FoliageMaterialPath =
            GeneratedFolder + "/BT_TreeFoliage.mat";

        private const string TrunkMaterialPath =
            GeneratedFolder + "/BT_TreeTrunk.mat";

        private const int MaximumTreeCount = 600;
        private const float PrototypeHeightMetres = 8f;

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
            forestRoot.transform.SetParent(
                BoomtownWorldHierarchy.GetForestContainer(),
                false);

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
            float totalBoardFeet = 0f;

            while (created < requestedTreeCount &&
                   attempts < maximumAttempts)
            {
                attempts++;

                float normalizedX = (float)random.NextDouble();
                float normalizedZ = (float)random.NextDouble();

                if (Mathf.Abs(normalizedX - 0.5f) < 0.08f)
                {
                    continue;
                }

                float slope = terrainData.GetSteepness(
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

                TreeMeasurements measurements =
                    GenerateTreeMeasurements(random, slope, patchNoise);

                GameObject tree =
                    (GameObject)PrefabUtility.InstantiatePrefab(treePrefab);

                tree.name = $"Tree_{created:000}_Age_{measurements.ageYears}";
                tree.transform.SetParent(forestRoot.transform);
                tree.transform.position =
                    new Vector3(worldX, worldY, worldZ);

                float heightScale =
                    measurements.heightMetres / PrototypeHeightMetres;

                float diameterFactor = Mathf.InverseLerp(
                    8f,
                    85f,
                    measurements.diameterCentimetres);

                float crownWidthMultiplier = Mathf.Lerp(
                    0.72f,
                    1.28f,
                    diameterFactor);

                tree.transform.localScale =
                    new Vector3(
                        heightScale * crownWidthMultiplier,
                        heightScale,
                        heightScale * crownWidthMultiplier);

                tree.transform.rotation = Quaternion.Euler(
                    0f,
                    Mathf.Lerp(
                        0f,
                        360f,
                        (float)random.NextDouble()),
                    0f);

                TreeResource resource =
                    tree.AddComponent<TreeResource>();

                resource.ageYears = measurements.ageYears;
                resource.heightMetres = measurements.heightMetres;
                resource.diameterCentimetres =
                    measurements.diameterCentimetres;
                resource.merchantableLengthMetres =
                    measurements.merchantableLengthMetres;
                resource.boardFeet = measurements.boardFeet;
                resource.timberQuality = measurements.timberQuality;

                totalBoardFeet += measurements.boardFeet;
                created++;
            }

            Selection.activeGameObject = forestRoot;
            EditorUtility.SetDirty(terrain.terrainData);
            EditorUtility.SetDirty(forestRoot);
            AssetDatabase.SaveAssets();

            Debug.Log(
                $"[Boomtown Forest Generator] Created {created} age-varied " +
                $"conifers using seed {mapDefinition.worldSeed}. " +
                $"Estimated standing lumber: {totalBoardFeet:N0} board feet.");
        }

        private static TreeMeasurements GenerateTreeMeasurements(
            System.Random random,
            float slopeDegrees,
            float patchNoise)
        {
            float ageRoll = (float)random.NextDouble();
            float weightedAge = Mathf.Pow(ageRoll, 1.65f);
            int ageYears = Mathf.RoundToInt(
                Mathf.Lerp(12f, 165f, weightedAge));

            float ageFactor = Mathf.InverseLerp(
                12f,
                165f,
                ageYears);

            float siteQuality = Mathf.Clamp01(
                patchNoise * 0.72f +
                (1f - Mathf.InverseLerp(0f, 38f, slopeDegrees)) * 0.28f);

            float growthVariation = Mathf.Lerp(
                0.88f,
                1.12f,
                (float)random.NextDouble());

            float heightMetres = Mathf.Lerp(
                3.5f,
                28f,
                Mathf.Pow(ageFactor, 0.72f));

            heightMetres *= Mathf.Lerp(0.84f, 1.08f, siteQuality);
            heightMetres *= growthVariation;

            float diameterCentimetres = Mathf.Lerp(
                8f,
                85f,
                Mathf.Pow(ageFactor, 0.86f));

            diameterCentimetres *= Mathf.Lerp(0.82f, 1.12f, siteQuality);
            diameterCentimetres *= Mathf.Lerp(
                0.9f,
                1.1f,
                (float)random.NextDouble());

            float timberQuality = Mathf.Lerp(
                0.62f,
                0.98f,
                (float)random.NextDouble());

            timberQuality *= Mathf.Lerp(
                0.88f,
                1f,
                1f - Mathf.InverseLerp(20f, 38f, slopeDegrees));
            timberQuality = Mathf.Clamp01(timberQuality);

            float merchantableLengthMetres =
                heightMetres * Mathf.Lerp(0.55f, 0.72f, timberQuality);

            float boardFeet = EstimateBoardFeet(
                diameterCentimetres,
                merchantableLengthMetres,
                timberQuality);

            return new TreeMeasurements
            {
                ageYears = ageYears,
                heightMetres = heightMetres,
                diameterCentimetres = diameterCentimetres,
                merchantableLengthMetres = merchantableLengthMetres,
                timberQuality = timberQuality,
                boardFeet = boardFeet
            };
        }

        private static float EstimateBoardFeet(
            float diameterCentimetres,
            float merchantableLengthMetres,
            float timberQuality)
        {
            const float centimetresToInches = 0.393701f;
            const float metresToFeet = 3.28084f;
            const float logLengthFeet = 16f;

            float diameterInches =
                diameterCentimetres * centimetresToInches;
            float merchantableFeet =
                merchantableLengthMetres * metresToFeet;

            int fullLogs = Mathf.Max(
                0,
                Mathf.FloorToInt(merchantableFeet / logLengthFeet));

            float boardFeet = 0f;

            for (int logIndex = 0;
                 logIndex < fullLogs;
                 logIndex++)
            {
                float taperMultiplier = Mathf.Max(
                    0.55f,
                    1f - logIndex * 0.085f);

                float logDiameter =
                    diameterInches * taperMultiplier;

                float usableDiameter = Mathf.Max(
                    0f,
                    logDiameter - 4f);

                boardFeet +=
                    usableDiameter * usableDiameter *
                    logLengthFeet / 16f;
            }

            return Mathf.Max(
                0f,
                boardFeet * timberQuality);
        }

        private static GameObject RebuildPrototypeTree()
        {
            AssetDatabase.DeleteAsset(TreePrefabPath);

            Material foliageMaterial = GetOrCreateMaterial(
                FoliageMaterialPath,
                new Color32(48, 82, 45, 255));
            Material trunkMaterial = GetOrCreateMaterial(
                TrunkMaterialPath,
                new Color32(92, 63, 40, 255));

            GameObject tree = new GameObject("BT_PrototypeConifer");

            GameObject trunk =
                GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.SetParent(tree.transform, false);
            trunk.transform.localPosition = new Vector3(0f, 2.4f, 0f);
            trunk.transform.localScale = new Vector3(0.34f, 2.4f, 0.34f);
            trunk.GetComponent<MeshRenderer>().sharedMaterial = trunkMaterial;
            UnityEngine.Object.DestroyImmediate(trunk.GetComponent<Collider>());

            CreateCanopyLayer(
                tree.transform,
                "LowerCanopy",
                new Vector3(0f, 3.2f, 0f),
                new Vector3(2.4f, 1.35f, 2.4f),
                foliageMaterial);

            CreateCanopyLayer(
                tree.transform,
                "MiddleCanopy",
                new Vector3(0f, 4.8f, 0f),
                new Vector3(1.85f, 1.28f, 1.85f),
                foliageMaterial);

            CreateCanopyLayer(
                tree.transform,
                "UpperCanopy",
                new Vector3(0f, 6.15f, 0f),
                new Vector3(1.25f, 1.08f, 1.25f),
                foliageMaterial);

            CreateCanopyLayer(
                tree.transform,
                "Crown",
                new Vector3(0f, 7.05f, 0f),
                new Vector3(0.62f, 0.82f, 0.62f),
                foliageMaterial);

            GameObject savedPrefab =
                PrefabUtility.SaveAsPrefabAsset(
                    tree,
                    TreePrefabPath);

            UnityEngine.Object.DestroyImmediate(tree);
            return savedPrefab;
        }

        private static void CreateCanopyLayer(
            Transform parent,
            string objectName,
            Vector3 localPosition,
            Vector3 localScale,
            Material foliageMaterial)
        {
            GameObject layer =
                GameObject.CreatePrimitive(PrimitiveType.Sphere);

            layer.name = objectName;
            layer.transform.SetParent(parent, false);
            layer.transform.localPosition = localPosition;
            layer.transform.localScale = localScale;
            layer.GetComponent<MeshRenderer>().sharedMaterial = foliageMaterial;
            UnityEngine.Object.DestroyImmediate(layer.GetComponent<Collider>());
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

            if (!AssetDatabase.IsValidFolder(GeneratedFolder))
            {
                AssetDatabase.CreateFolder(root, "Generated");
            }
        }

        private struct TreeMeasurements
        {
            public int ageYears;
            public float heightMetres;
            public float diameterCentimetres;
            public float merchantableLengthMetres;
            public float timberQuality;
            public float boardFeet;
        }
    }
}
