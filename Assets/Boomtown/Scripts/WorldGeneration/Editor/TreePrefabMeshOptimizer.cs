using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Boomtown.WorldGeneration.Editor
{
    /// <summary>
    /// Builds lightweight regional conifers from clustered procedural branch fans.
    /// Every tree remains two renderers: one trunk and one combined canopy.
    /// </summary>
    public static class TreePrefabMeshOptimizer
    {
        private const string GeneratedFolder =
            "Assets/Boomtown/Scripts/WorldGeneration/Generated";

        private struct TreeShape
        {
            public string name;
            public int tierCount;
            public int branchesPerTier;
            public int spraysPerBranch;
            public float trunkHeight;
            public float trunkBaseRadius;
            public float trunkTopRadius;
            public float crownBottom;
            public float crownTop;
            public float lowerBranchLength;
            public float upperBranchLength;
            public float branchWidth;
            public float branchThickness;
            public float droop;
            public float irregularity;
            public float tierSkipChance;
            public float tipLean;
            public float crownDensity;
        }

        public static void OptimizeGeneratedTreePrefabs()
        {
            TreeShape[] shapes = BuildShapes();
            int rebuilt = 0;

            foreach (TreeShape shape in shapes)
            {
                if (RebuildSpeciesPrefab(shape))
                {
                    rebuilt++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            int refreshed = RefreshGeneratedForestInstances();
            Debug.Log(
                $"[Boomtown Tree Builder] Rebuilt {rebuilt} regional tree " +
                $"prefab(s) and refreshed {refreshed} generated tree instance(s) " +
                "with clustered branch architecture.");
        }

        private static TreeShape[] BuildShapes()
        {
            return new[]
            {
                new TreeShape
                {
                    name = "DouglasFir", tierCount = 15, branchesPerTier = 7,
                    spraysPerBranch = 3, trunkHeight = 9.6f,
                    trunkBaseRadius = 0.34f, trunkTopRadius = 0.065f,
                    crownBottom = 2.8f, crownTop = 9.35f,
                    lowerBranchLength = 2.30f, upperBranchLength = 0.22f,
                    branchWidth = 0.36f, branchThickness = 0.11f,
                    droop = 0.36f, irregularity = 0.24f,
                    tierSkipChance = 0.10f, tipLean = 0.05f,
                    crownDensity = 0.92f
                },
                new TreeShape
                {
                    name = "RedCedar", tierCount = 17, branchesPerTier = 8,
                    spraysPerBranch = 4, trunkHeight = 8.8f,
                    trunkBaseRadius = 0.50f, trunkTopRadius = 0.10f,
                    crownBottom = 0.9f, crownTop = 8.55f,
                    lowerBranchLength = 2.85f, upperBranchLength = 0.34f,
                    branchWidth = 0.50f, branchThickness = 0.13f,
                    droop = 0.72f, irregularity = 0.34f,
                    tierSkipChance = 0.02f, tipLean = 0.14f,
                    crownDensity = 1.15f
                },
                new TreeShape
                {
                    name = "Hemlock", tierCount = 14, branchesPerTier = 7,
                    spraysPerBranch = 3, trunkHeight = 9.2f,
                    trunkBaseRadius = 0.29f, trunkTopRadius = 0.055f,
                    crownBottom = 2.2f, crownTop = 8.95f,
                    lowerBranchLength = 1.95f, upperBranchLength = 0.18f,
                    branchWidth = 0.34f, branchThickness = 0.09f,
                    droop = 0.66f, irregularity = 0.44f,
                    tierSkipChance = 0.16f, tipLean = 0.42f,
                    crownDensity = 0.84f
                },
                new TreeShape
                {
                    name = "Lodgepole", tierCount = 8, branchesPerTier = 5,
                    spraysPerBranch = 2, trunkHeight = 9.8f,
                    trunkBaseRadius = 0.22f, trunkTopRadius = 0.045f,
                    crownBottom = 5.2f, crownTop = 9.45f,
                    lowerBranchLength = 1.15f, upperBranchLength = 0.16f,
                    branchWidth = 0.25f, branchThickness = 0.075f,
                    droop = 0.16f, irregularity = 0.52f,
                    tierSkipChance = 0.30f, tipLean = 0.10f,
                    crownDensity = 0.62f
                },
                new TreeShape
                {
                    name = "Spruce", tierCount = 18, branchesPerTier = 8,
                    spraysPerBranch = 3, trunkHeight = 9.0f,
                    trunkBaseRadius = 0.32f, trunkTopRadius = 0.052f,
                    crownBottom = 1.15f, crownTop = 8.75f,
                    lowerBranchLength = 2.35f, upperBranchLength = 0.18f,
                    branchWidth = 0.40f, branchThickness = 0.11f,
                    droop = 0.46f, irregularity = 0.20f,
                    tierSkipChance = 0.02f, tipLean = 0.04f,
                    crownDensity = 1.08f
                }
            };
        }

        private static bool RebuildSpeciesPrefab(TreeShape shape)
        {
            string prefabPath = $"{GeneratedFolder}/BT_{shape.name}.prefab";
            Material trunkMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                $"{GeneratedFolder}/BT_{shape.name}_Trunk.mat");
            Material foliageMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                $"{GeneratedFolder}/BT_{shape.name}_Foliage.mat");

            if (trunkMaterial == null || foliageMaterial == null)
            {
                return false;
            }

            Mesh trunkMesh = BuildTaperedTrunkMesh(shape);
            Mesh canopyMesh = BuildCanopyMesh(shape);
            string trunkMeshPath =
                $"{GeneratedFolder}/BT_{shape.name}_TrunkMesh.asset";
            string canopyMeshPath =
                $"{GeneratedFolder}/BT_{shape.name}_CanopyMesh.asset";

            AssetDatabase.DeleteAsset(trunkMeshPath);
            AssetDatabase.DeleteAsset(canopyMeshPath);
            AssetDatabase.CreateAsset(trunkMesh, trunkMeshPath);
            AssetDatabase.CreateAsset(canopyMesh, canopyMeshPath);

            GameObject root = new GameObject($"BT_{shape.name}");
            CreateMeshChild(root.transform, "Trunk", trunkMesh, trunkMaterial);
            CreateMeshChild(root.transform, "Canopy", canopyMesh, foliageMaterial);
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return true;
        }

        private static int RefreshGeneratedForestInstances()
        {
            TreeResource[] resources = Object.FindObjectsByType<TreeResource>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            int refreshed = 0;

            foreach (TreeResource resource in resources)
            {
                if (resource == null)
                {
                    continue;
                }

                string speciesName = GetSpeciesAssetName(resource.species);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    $"{GeneratedFolder}/BT_{speciesName}.prefab");
                if (prefab == null)
                {
                    continue;
                }

                Transform prefabTrunk = prefab.transform.Find("Trunk");
                Transform prefabCanopy = prefab.transform.Find("Canopy");
                if (prefabTrunk == null || prefabCanopy == null)
                {
                    continue;
                }

                MeshFilter trunkFilter = prefabTrunk.GetComponent<MeshFilter>();
                MeshRenderer trunkRenderer = prefabTrunk.GetComponent<MeshRenderer>();
                MeshFilter canopyFilter = prefabCanopy.GetComponent<MeshFilter>();
                MeshRenderer canopyRenderer = prefabCanopy.GetComponent<MeshRenderer>();
                if (trunkFilter == null || trunkRenderer == null ||
                    canopyFilter == null || canopyRenderer == null)
                {
                    continue;
                }

                for (int index = resource.transform.childCount - 1;
                     index >= 0;
                     index--)
                {
                    Object.DestroyImmediate(
                        resource.transform.GetChild(index).gameObject);
                }

                CreateMeshChild(resource.transform, "Trunk",
                    trunkFilter.sharedMesh, trunkRenderer.sharedMaterial);
                CreateMeshChild(resource.transform, "Canopy",
                    canopyFilter.sharedMesh, canopyRenderer.sharedMaterial);
                EditorUtility.SetDirty(resource.gameObject);
                refreshed++;
            }

            return refreshed;
        }

        private static string GetSpeciesAssetName(TreeSpecies species)
        {
            switch (species)
            {
                case TreeSpecies.WesternRedCedar:
                    return "RedCedar";
                case TreeSpecies.WesternHemlock:
                    return "Hemlock";
                case TreeSpecies.LodgepolePine:
                    return "Lodgepole";
                case TreeSpecies.EngelmannSpruce:
                    return "Spruce";
                default:
                    return "DouglasFir";
            }
        }

        private static void CreateMeshChild(
            Transform parent,
            string objectName,
            Mesh mesh,
            Material material)
        {
            GameObject child = new GameObject(objectName);
            child.transform.SetParent(parent, false);
            child.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = child.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }

        private static Mesh BuildTaperedTrunkMesh(TreeShape shape)
        {
            const int segments = 9;
            const int rings = 5;
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            List<Vector2> uvs = new List<Vector2>();

            for (int ring = 0; ring < rings; ring++)
            {
                float t = ring / (float)(rings - 1);
                float y = shape.trunkHeight * t;
                float radius = Mathf.Lerp(
                    shape.trunkBaseRadius,
                    shape.trunkTopRadius,
                    Mathf.Pow(t, 0.70f));

                for (int segment = 0; segment < segments; segment++)
                {
                    float angle = segment / (float)segments * Mathf.PI * 2f;
                    vertices.Add(new Vector3(
                        Mathf.Cos(angle) * radius,
                        y,
                        Mathf.Sin(angle) * radius));
                    uvs.Add(new Vector2(segment / (float)segments, t));
                }
            }

            for (int ring = 0; ring < rings - 1; ring++)
            {
                int start = ring * segments;
                int nextStart = (ring + 1) * segments;
                for (int segment = 0; segment < segments; segment++)
                {
                    int next = (segment + 1) % segments;
                    AddQuad(triangles,
                        start + segment,
                        nextStart + segment,
                        start + next,
                        nextStart + next);
                }
            }

            Mesh mesh = new Mesh { name = $"BT_{shape.name}_TrunkMesh" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetUVs(0, uvs);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh BuildCanopyMesh(TreeShape shape)
        {
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            List<Vector2> uvs = new List<Vector2>();
            System.Random random = new System.Random(StableHash(shape.name));

            for (int tier = 0; tier < shape.tierCount; tier++)
            {
                float t = tier / (float)Mathf.Max(1, shape.tierCount - 1);
                if (tier > 1 && tier < shape.tierCount - 2 &&
                    random.NextDouble() < shape.tierSkipChance)
                {
                    continue;
                }

                float y = Mathf.Lerp(shape.crownBottom, shape.crownTop, t);
                float branchLength = Mathf.Lerp(
                    shape.lowerBranchLength,
                    shape.upperBranchLength,
                    Mathf.Pow(t, 0.82f));
                int branchCount = Mathf.Max(3,
                    Mathf.RoundToInt(shape.branchesPerTier * shape.crownDensity) +
                    random.Next(-1, 2));
                float tierRotation =
                    (float)random.NextDouble() * Mathf.PI * 2f;
                Vector3 centreOffset = new Vector3(
                    RandomRange(random, -shape.irregularity, shape.irregularity),
                    0f,
                    RandomRange(random, -shape.irregularity, shape.irregularity));
                centreOffset += new Vector3(
                    shape.tipLean * t * t,
                    0f,
                    shape.tipLean * 0.35f * t * t);

                for (int branch = 0; branch < branchCount; branch++)
                {
                    float angle = tierRotation +
                        branch / (float)branchCount * Mathf.PI * 2f +
                        RandomRange(random, -0.12f, 0.12f);
                    float length = branchLength *
                        RandomRange(random, 0.72f, 1.18f);
                    float width = shape.branchWidth *
                        Mathf.Lerp(1f, 0.42f, t) *
                        RandomRange(random, 0.78f, 1.18f);
                    float thickness = shape.branchThickness *
                        Mathf.Lerp(1f, 0.50f, t);
                    float droop = shape.droop *
                        Mathf.Lerp(1f, 0.34f, t) *
                        RandomRange(random, 0.78f, 1.22f);
                    Vector3 origin = centreOffset + new Vector3(
                        0f,
                        y + RandomRange(random, -0.10f, 0.12f),
                        0f);

                    AddBranchCluster(
                        vertices,
                        triangles,
                        uvs,
                        origin,
                        angle,
                        length,
                        width,
                        thickness,
                        droop,
                        shape.spraysPerBranch,
                        random);
                }
            }

            AddCrownTip(vertices, triangles, uvs, shape);

            Mesh mesh = new Mesh
            {
                name = $"BT_{shape.name}_CanopyMesh",
                indexFormat = IndexFormat.UInt32
            };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetUVs(0, uvs);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.Optimize();
            return mesh;
        }

        private static void AddBranchCluster(
            List<Vector3> vertices,
            List<int> triangles,
            List<Vector2> uvs,
            Vector3 origin,
            float angle,
            float length,
            float halfWidth,
            float halfThickness,
            float droop,
            int sprayCount,
            System.Random random)
        {
            AddBranchFan(vertices, triangles, uvs,
                origin, angle, length, halfWidth, halfThickness, droop);

            for (int spray = 1; spray < sprayCount; spray++)
            {
                float side = spray % 2 == 0 ? 1f : -1f;
                float spread = Mathf.Lerp(0.18f, 0.42f,
                    spray / (float)Mathf.Max(1, sprayCount - 1));
                float sprayAngle = angle + side * spread +
                    RandomRange(random, -0.06f, 0.06f);
                float sprayLength = length *
                    RandomRange(random, 0.58f, 0.82f);
                Vector3 sprayOrigin = origin + new Vector3(
                    Mathf.Cos(angle),
                    0f,
                    Mathf.Sin(angle)) * length * RandomRange(random, 0.18f, 0.42f);

                AddBranchFan(vertices, triangles, uvs,
                    sprayOrigin,
                    sprayAngle,
                    sprayLength,
                    halfWidth * 0.72f,
                    halfThickness * 0.78f,
                    droop * RandomRange(random, 0.70f, 1.08f));
            }
        }

        private static void AddBranchFan(
            List<Vector3> vertices,
            List<int> triangles,
            List<Vector2> uvs,
            Vector3 origin,
            float angle,
            float length,
            float halfWidth,
            float halfThickness,
            float droop)
        {
            Vector3 forward = new Vector3(
                Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            Vector3 right = new Vector3(-forward.z, 0f, forward.x);
            Vector3 baseCentre = origin + forward * 0.10f;
            Vector3 middle = origin + forward * (length * 0.52f) -
                Vector3.up * (droop * 0.32f);
            Vector3 tip = origin + forward * length - Vector3.up * droop;
            int start = vertices.Count;

            vertices.Add(baseCentre - right * halfWidth * 0.28f +
                         Vector3.up * halfThickness);
            vertices.Add(baseCentre + right * halfWidth * 0.28f +
                         Vector3.up * halfThickness);
            vertices.Add(middle + right * halfWidth +
                         Vector3.up * halfThickness * 0.25f);
            vertices.Add(tip);
            vertices.Add(middle - right * halfWidth +
                         Vector3.up * halfThickness * 0.25f);

            vertices.Add(baseCentre - right * halfWidth * 0.28f -
                         Vector3.up * halfThickness);
            vertices.Add(baseCentre + right * halfWidth * 0.28f -
                         Vector3.up * halfThickness);
            vertices.Add(middle + right * halfWidth -
                         Vector3.up * halfThickness * 0.25f);
            vertices.Add(tip - Vector3.up * halfThickness * 0.10f);
            vertices.Add(middle - right * halfWidth -
                         Vector3.up * halfThickness * 0.25f);

            for (int index = 0; index < 10; index++)
            {
                uvs.Add(new Vector2((index % 5) / 4f,
                    index < 5 ? 1f : 0f));
            }

            AddTriangle(triangles, start + 0, start + 1, start + 2);
            AddTriangle(triangles, start + 0, start + 2, start + 4);
            AddTriangle(triangles, start + 4, start + 2, start + 3);
            AddTriangle(triangles, start + 5, start + 7, start + 6);
            AddTriangle(triangles, start + 5, start + 9, start + 7);
            AddTriangle(triangles, start + 9, start + 8, start + 7);

            for (int edge = 0; edge < 5; edge++)
            {
                int next = (edge + 1) % 5;
                AddQuad(triangles,
                    start + edge,
                    start + 5 + edge,
                    start + next,
                    start + 5 + next);
            }
        }

        private static void AddCrownTip(
            List<Vector3> vertices,
            List<int> triangles,
            List<Vector2> uvs,
            TreeShape shape)
        {
            const int segments = 7;
            float baseY = shape.crownTop - 0.45f;
            float tipY = shape.trunkHeight + 0.10f;
            float radius = Mathf.Max(0.10f, shape.upperBranchLength * 0.72f);
            Vector3 lean = new Vector3(
                shape.tipLean,
                0f,
                shape.tipLean * 0.35f);
            int start = vertices.Count;

            for (int index = 0; index < segments; index++)
            {
                float angle = index / (float)segments * Mathf.PI * 2f;
                vertices.Add(new Vector3(
                    Mathf.Cos(angle) * radius,
                    baseY,
                    Mathf.Sin(angle) * radius));
                uvs.Add(new Vector2(index / (float)segments, 0f));
            }

            int tipIndex = vertices.Count;
            vertices.Add(lean + new Vector3(0f, tipY, 0f));
            uvs.Add(new Vector2(0.5f, 1f));

            for (int index = 0; index < segments; index++)
            {
                int next = (index + 1) % segments;
                AddTriangle(triangles,
                    start + index,
                    tipIndex,
                    start + next);
            }
        }

        private static float RandomRange(
            System.Random random,
            float minimum,
            float maximum)
        {
            return Mathf.Lerp(minimum, maximum, (float)random.NextDouble());
        }

        private static void AddQuad(
            List<int> triangles,
            int a,
            int b,
            int c,
            int d)
        {
            AddTriangle(triangles, a, b, c);
            AddTriangle(triangles, c, b, d);
        }

        private static void AddTriangle(
            List<int> triangles,
            int a,
            int b,
            int c)
        {
            triangles.Add(a);
            triangles.Add(b);
            triangles.Add(c);
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
    }
}
