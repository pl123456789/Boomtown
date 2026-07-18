using System;
using UnityEditor;
using UnityEngine;

namespace Boomtown.WorldGeneration.Editor
{
    /// <summary>
    /// Generates a deterministic mixed forest for the Hope, British Columbia region.
    /// Each tree receives species, age, size, quality, and board-foot data.
    /// </summary>
    public static class BoomtownForestGenerator
    {
        private const string GeneratedFolder =
            "Assets/Boomtown/Scripts/WorldGeneration/Generated";

        private const int MaximumTreeCount = 1200;
        private const float PrototypeHeightMetres = 8f;

        private struct SpeciesProfile
        {
            public TreeSpecies species;
            public string shortName;
            public Color32 foliageColour;
            public Color32 trunkColour;
            public float heightMultiplier;
            public float diameterMultiplier;
            public float crownWidthMultiplier;
            public float merchantableMultiplier;
            public float qualityMultiplier;
            public float wetSitePreference;
            public float highSitePreference;
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

            SpeciesProfile[] profiles = BuildSpeciesProfiles();
            GameObject[] prefabs = new GameObject[profiles.Length];

            for (int index = 0; index < profiles.Length; index++)
            {
                prefabs[index] = RebuildSpeciesTree(profiles[index]);
            }

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
            int maximumAttempts = requestedTreeCount * 24;
            float totalBoardFeet = 0f;
            int[] speciesCounts = new int[profiles.Length];

            while (created < requestedTreeCount &&
                   attempts < maximumAttempts)
            {
                attempts++;

                float normalizedX = (float)random.NextDouble();
                float normalizedZ = (float)random.NextDouble();

                if (Mathf.Abs(normalizedX - 0.5f) < 0.065f)
                {
                    continue;
                }

                float slope = terrainData.GetSteepness(
                    normalizedX,
                    normalizedZ);

                if (slope > 43f)
                {
                    continue;
                }

                float patchNoise = Mathf.PerlinNoise(
                    normalizedX * 9f + seed * 0.0001f,
                    normalizedZ * 9f + seed * 0.0001f);

                float fineNoise = Mathf.PerlinNoise(
                    normalizedX * 23f + seed * 0.00037f,
                    normalizedZ * 23f + seed * 0.00029f);

                float forestThreshold = Mathf.Lerp(0.31f, 0.43f, fineNoise);
                if (patchNoise < forestThreshold)
                {
                    continue;
                }

                float worldX =
                    terrainPosition.x + normalizedX * terrainData.size.x;
                float worldZ =
                    terrainPosition.z + normalizedZ * terrainData.size.z;
                float worldY =
                    terrain.SampleHeight(new Vector3(worldX, 0f, worldZ)) +
                    terrainPosition.y;

                float normalizedElevation = Mathf.InverseLerp(
                    terrainPosition.y,
                    terrainPosition.y + terrainData.size.y,
                    worldY);

                float wetness = Mathf.Clamp01(
                    (1f - normalizedElevation) * 0.62f +
                    patchNoise * 0.38f);

                int speciesIndex = SelectSpeciesIndex(
                    profiles,
                    random,
                    wetness,
                    normalizedElevation,
                    slope);

                SpeciesProfile profile = profiles[speciesIndex];
                TreeMeasurements measurements = GenerateTreeMeasurements(
                    random,
                    slope,
                    patchNoise,
                    profile);

                GameObject tree =
                    (GameObject)PrefabUtility.InstantiatePrefab(
                        prefabs[speciesIndex]);

                tree.name =
                    $"Tree_{created:0000}_{profile.shortName}_Age_{measurements.ageYears}";
                tree.transform.SetParent(forestRoot.transform);
                tree.transform.position = new Vector3(worldX, worldY, worldZ);

                float heightScale =
                    measurements.heightMetres / PrototypeHeightMetres;
                float diameterFactor = Mathf.InverseLerp(
                    8f,
                    110f,
                    measurements.diameterCentimetres);
                float crownWidth = Mathf.Lerp(
                    0.68f,
                    1.32f,
                    diameterFactor) * profile.crownWidthMultiplier;

                tree.transform.localScale = new Vector3(
                    heightScale * crownWidth,
                    heightScale,
                    heightScale * crownWidth);

                tree.transform.rotation = Quaternion.Euler(
                    Mathf.Lerp(-1.5f, 1.5f, (float)random.NextDouble()),
                    Mathf.Lerp(0f, 360f, (float)random.NextDouble()),
                    Mathf.Lerp(-1.5f, 1.5f, (float)random.NextDouble()));

                TreeResource resource = tree.AddComponent<TreeResource>();
                resource.species = profile.species;
                resource.ageYears = measurements.ageYears;
                resource.heightMetres = measurements.heightMetres;
                resource.diameterCentimetres = measurements.diameterCentimetres;
                resource.merchantableLengthMetres =
                    measurements.merchantableLengthMetres;
                resource.boardFeet = measurements.boardFeet;
                resource.timberQuality = measurements.timberQuality;

                totalBoardFeet += measurements.boardFeet;
                speciesCounts[speciesIndex]++;
                created++;
            }

            Selection.activeGameObject = forestRoot;
            EditorUtility.SetDirty(terrain.terrainData);
            EditorUtility.SetDirty(forestRoot);
            AssetDatabase.SaveAssets();

            string speciesSummary = string.Empty;
            for (int index = 0; index < profiles.Length; index++)
            {
                if (index > 0)
                {
                    speciesSummary += ", ";
                }

                speciesSummary +=
                    $"{profiles[index].shortName}: {speciesCounts[index]}";
            }

            Debug.Log(
                $"[Boomtown Forest Generator] Created {created} mixed regional " +
                $"trees using seed {mapDefinition.worldSeed}. " +
                $"Estimated standing lumber: {totalBoardFeet:N0} board feet. " +
                speciesSummary + ".");
        }

        private static SpeciesProfile[] BuildSpeciesProfiles()
        {
            return new[]
            {
                new SpeciesProfile
                {
                    species = TreeSpecies.DouglasFir,
                    shortName = "DouglasFir",
                    foliageColour = new Color32(43, 79, 47, 255),
                    trunkColour = new Color32(103, 72, 47, 255),
                    heightMultiplier = 1.12f,
                    diameterMultiplier = 1.12f,
                    crownWidthMultiplier = 0.92f,
                    merchantableMultiplier = 1.08f,
                    qualityMultiplier = 1.04f,
                    wetSitePreference = 0.35f,
                    highSitePreference = 0.48f
                },
                new SpeciesProfile
                {
                    species = TreeSpecies.WesternRedCedar,
                    shortName = "RedCedar",
                    foliageColour = new Color32(39, 76, 58, 255),
                    trunkColour = new Color32(111, 67, 43, 255),
                    heightMultiplier = 1.05f,
                    diameterMultiplier = 1.20f,
                    crownWidthMultiplier = 1.08f,
                    merchantableMultiplier = 1.02f,
                    qualityMultiplier = 1.08f,
                    wetSitePreference = 0.92f,
                    highSitePreference = 0.18f
                },
                new SpeciesProfile
                {
                    species = TreeSpecies.WesternHemlock,
                    shortName = "Hemlock",
                    foliageColour = new Color32(35, 69, 44, 255),
                    trunkColour = new Color32(91, 70, 52, 255),
                    heightMultiplier = 1.02f,
                    diameterMultiplier = 0.92f,
                    crownWidthMultiplier = 0.86f,
                    merchantableMultiplier = 0.98f,
                    qualityMultiplier = 0.94f,
                    wetSitePreference = 0.78f,
                    highSitePreference = 0.34f
                },
                new SpeciesProfile
                {
                    species = TreeSpecies.LodgepolePine,
                    shortName = "Lodgepole",
                    foliageColour = new Color32(61, 89, 55, 255),
                    trunkColour = new Color32(96, 73, 52, 255),
                    heightMultiplier = 0.88f,
                    diameterMultiplier = 0.76f,
                    crownWidthMultiplier = 0.66f,
                    merchantableMultiplier = 1.08f,
                    qualityMultiplier = 0.91f,
                    wetSitePreference = 0.22f,
                    highSitePreference = 0.82f
                },
                new SpeciesProfile
                {
                    species = TreeSpecies.EngelmannSpruce,
                    shortName = "Spruce",
                    foliageColour = new Color32(55, 83, 65, 255),
                    trunkColour = new Color32(93, 76, 58, 255),
                    heightMultiplier = 0.98f,
                    diameterMultiplier = 0.90f,
                    crownWidthMultiplier = 0.82f,
                    merchantableMultiplier = 1.01f,
                    qualityMultiplier = 0.96f,
                    wetSitePreference = 0.56f,
                    highSitePreference = 0.72f
                }
            };
        }

        private static int SelectSpeciesIndex(
            SpeciesProfile[] profiles,
            System.Random random,
            float wetness,
            float elevation,
            float slope)
        {
            float slopeFactor = Mathf.InverseLerp(0f, 43f, slope);
            float totalWeight = 0f;
            float[] weights = new float[profiles.Length];

            for (int index = 0; index < profiles.Length; index++)
            {
                SpeciesProfile profile = profiles[index];
                float wetMatch = 1f - Mathf.Abs(
                    wetness - profile.wetSitePreference);
                float elevationMatch = 1f - Mathf.Abs(
                    elevation - profile.highSitePreference);

                float weight = Mathf.Max(
                    0.04f,
                    wetMatch * 0.56f +
                    elevationMatch * 0.34f +
                    (1f - slopeFactor) * 0.10f);

                weights[index] = weight;
                totalWeight += weight;
            }

            float roll = (float)random.NextDouble() * totalWeight;
            float cumulative = 0f;

            for (int index = 0; index < weights.Length; index++)
            {
                cumulative += weights[index];
                if (roll <= cumulative)
                {
                    return index;
                }
            }

            return 0;
        }

        private static TreeMeasurements GenerateTreeMeasurements(
            System.Random random,
            float slopeDegrees,
            float patchNoise,
            SpeciesProfile profile)
        {
            float ageRoll = (float)random.NextDouble();
            float weightedAge = Mathf.Pow(ageRoll, 1.55f);
            int ageYears = Mathf.RoundToInt(
                Mathf.Lerp(10f, 210f, weightedAge));

            float ageFactor = Mathf.InverseLerp(10f, 210f, ageYears);
            float siteQuality = Mathf.Clamp01(
                patchNoise * 0.68f +
                (1f - Mathf.InverseLerp(0f, 43f, slopeDegrees)) * 0.32f);

            float growthVariation = Mathf.Lerp(
                0.86f,
                1.15f,
                (float)random.NextDouble());

            float heightMetres = Mathf.Lerp(
                2.8f,
                34f,
                Mathf.Pow(ageFactor, 0.70f));
            heightMetres *= Mathf.Lerp(0.82f, 1.10f, siteQuality);
            heightMetres *= growthVariation * profile.heightMultiplier;

            float diameterCentimetres = Mathf.Lerp(
                6f,
                105f,
                Mathf.Pow(ageFactor, 0.84f));
            diameterCentimetres *= Mathf.Lerp(0.80f, 1.14f, siteQuality);
            diameterCentimetres *= Mathf.Lerp(
                0.88f,
                1.12f,
                (float)random.NextDouble());
            diameterCentimetres *= profile.diameterMultiplier;

            float timberQuality = Mathf.Lerp(
                0.60f,
                0.98f,
                (float)random.NextDouble());
            timberQuality *= Mathf.Lerp(
                0.86f,
                1f,
                1f - Mathf.InverseLerp(18f, 43f, slopeDegrees));
            timberQuality *= profile.qualityMultiplier;
            timberQuality = Mathf.Clamp01(timberQuality);

            float merchantableLengthMetres =
                heightMetres *
                Mathf.Lerp(0.54f, 0.74f, timberQuality) *
                profile.merchantableMultiplier;

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

            float diameterInches = diameterCentimetres * centimetresToInches;
            float merchantableFeet = merchantableLengthMetres * metresToFeet;
            int fullLogs = Mathf.Max(
                0,
                Mathf.FloorToInt(merchantableFeet / logLengthFeet));
            float boardFeet = 0f;

            for (int logIndex = 0; logIndex < fullLogs; logIndex++)
            {
                float taperMultiplier = Mathf.Max(
                    0.52f,
                    1f - logIndex * 0.082f);
                float logDiameter = diameterInches * taperMultiplier;
                float usableDiameter = Mathf.Max(0f, logDiameter - 4f);

                boardFeet +=
                    usableDiameter * usableDiameter *
                    logLengthFeet / 16f;
            }

            return Mathf.Max(0f, boardFeet * timberQuality);
        }

        private static GameObject RebuildSpeciesTree(
            SpeciesProfile profile)
        {
            string prefabPath =
                $"{GeneratedFolder}/BT_{profile.shortName}.prefab";
            string foliagePath =
                $"{GeneratedFolder}/BT_{profile.shortName}_Foliage.mat";
            string trunkPath =
                $"{GeneratedFolder}/BT_{profile.shortName}_Trunk.mat";

            AssetDatabase.DeleteAsset(prefabPath);

            Material foliageMaterial = GetOrCreateMaterial(
                foliagePath,
                profile.foliageColour);
            Material trunkMaterial = GetOrCreateMaterial(
                trunkPath,
                profile.trunkColour);

            GameObject tree = new GameObject($"BT_{profile.shortName}");

            float trunkRadius = profile.species == TreeSpecies.WesternRedCedar
                ? 0.42f
                : 0.32f;

            GameObject trunk =
                GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.SetParent(tree.transform, false);
            trunk.transform.localPosition = new Vector3(0f, 2.75f, 0f);
            trunk.transform.localScale = new Vector3(
                trunkRadius,
                2.75f,
                trunkRadius);
            trunk.GetComponent<MeshRenderer>().sharedMaterial = trunkMaterial;
            UnityEngine.Object.DestroyImmediate(trunk.GetComponent<Collider>());

            int layerCount = profile.species == TreeSpecies.WesternRedCedar
                ? 6
                : 5;

            for (int layer = 0; layer < layerCount; layer++)
            {
                float t = layer / (float)Mathf.Max(1, layerCount - 1);
                float y = Mathf.Lerp(2.8f, 7.15f, t);
                float radius = Mathf.Lerp(2.35f, 0.48f, t);
                float vertical = Mathf.Lerp(0.78f, 0.46f, t);

                if (profile.species == TreeSpecies.LodgepolePine)
                {
                    radius *= 0.72f;
                    y += 0.20f * layer;
                }
                else if (profile.species == TreeSpecies.WesternRedCedar)
                {
                    radius *= 1.10f;
                    vertical *= 0.82f;
                }
                else if (profile.species == TreeSpecies.WesternHemlock)
                {
                    radius *= 0.88f;
                }

                CreateCanopyLayer(
                    tree.transform,
                    $"Canopy_{layer}",
                    new Vector3(0f, y, 0f),
                    new Vector3(radius, vertical, radius),
                    foliageMaterial,
                    layer * 17f);
            }

            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(
                tree,
                prefabPath);
            UnityEngine.Object.DestroyImmediate(tree);
            return savedPrefab;
        }

        private static void CreateCanopyLayer(
            Transform parent,
            string objectName,
            Vector3 localPosition,
            Vector3 localScale,
            Material foliageMaterial,
            float rotation)
        {
            GameObject layer =
                GameObject.CreatePrimitive(PrimitiveType.Sphere);
            layer.name = objectName;
            layer.transform.SetParent(parent, false);
            layer.transform.localPosition = localPosition;
            layer.transform.localScale = localScale;
            layer.transform.localRotation = Quaternion.Euler(0f, rotation, 0f);
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
            material.SetFloat("_Smoothness", 0.08f);
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
    }
}
