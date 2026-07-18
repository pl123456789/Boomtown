using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Boomtown.WorldGeneration.Editor
{
    /// <summary>
    /// Builds lightweight regional conifer prefabs from custom procedural meshes.
    /// Each tree uses one trunk renderer and one combined canopy renderer.
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

            int refreshedInstances = RefreshGeneratedForestInstances();

            Debug.Log(
                $"[Boomtown Tree Builder] Rebuilt {rebuilt} regional tree " +
                $"prefab(s) and refreshed {refreshedInstances} generated tree " +
                "instance(s) with irregular branch-fan canopies.");
        }

        private static TreeShape[] BuildShapes()
        {
            return new[]
            {
                new TreeShape
                {
                    name = "DouglasFir", tierCount = 13, branchesPerTier = 7,
                    trunkHeight = 9.2f, trunkBaseRadius = 0.34f,
                    trunkTopRadius = 0.075f, crownBottom = 2.7f,
                    crownTop = 9.0f, lowerBranchLength = 2.25f,
                    upperBranchLength = 0.28f, branchWidth = 0.52f,
                    branchThickness = 0.16f, droop = 0.20f,
                    irregularity = 0.22f, tierSkipChance = 0.08f,
                    tipLean = 0.05f
                },
                new TreeShape
                {
                    name = "RedCedar", tierCount = 14, branchesPerTier = 8,
                    trunkHeight = 8.5f, trunkBaseRadius = 0.48f,
                    trunkTopRadius = 0.11f, crownBottom = 1.1f,
                    crownTop = 8.2f, lowerBranchLength = 2.75f,
                    upperBranchLength = 0.38f, branchWidth = 0.72f,
                    branchThickness = 0.20f, droop = 0.52f,
                    irregularity = 0.30f, tierSkipChance = 0.04f,
                    tipLean = 0.10f
                },
                new TreeShape
                {
                    name = "Hemlock", tierCount = 12, branchesPerTier = 7,
                    trunkHeight = 8.9f, trunkBaseRadius = 0.30f,
                    trunkTopRadius = 0.065f, crownBottom = 2.0f,
                    crownTop = 8.65f, lowerBranchLength = 2.05f,
                    upperBranchLength = 0.25f, branchWidth = 0.48f,
                    branchThickness = 0.14f, droop = 0.48f,
                    irregularity = 0.38f, tierSkipChance = 0.14f,
                    tipLean = 0.28f
                },
                new TreeShape
                {
                    name = "Lodgepole", tierCount = 10, branchesPerTier = 5,
                    trunkHeight = 9.4f, trunkBaseRadius = 0.23f,
                    trunkTopRadius = 0.055f, crownBottom = 4.0f,
                    crownTop = 9.15f, lowerBranchLength = 1.38f,
                    upperBranchLength = 0.20f, branchWidth = 0.34f,
                    branchThickness = 0.10f, droop = 0.10f,
                    irregularity = 0.46f, tierSkipChance = 0.24f,
                    tipLean = 0.08f
                },
                new TreeShape
                {
                    name = "Spruce", tierCount = 15, branchesPerTier = 8,
                    trunkHeight = 8.8f, trunkBaseRadius = 0.32f,
                    trunkTopRadius = 0.06f, crownBottom = 1.5f,
                    crownTop = 8.6f, lowerBranchLength = 2.30f,
                    upperBranchLength = 0.24f, branchWidth = 0.56f,
                    branchThickness = 0.17f, droop = 0.34f,
                    irregularity = 0.18f, tierSkipChance = 0.03f,
                    tipLean = 0.04f
                }
            };
        }

        private static bool RebuildSpeciesPrefab(TreeShape shape)
        {
            string prefabPath = $"{GeneratedFolder}/BT_{shape.name}.prefab";
            string trunkMaterialPath =
                $"{GeneratedFolder}/BT_{shape.name}_Trunk.mat";
            string foliageMaterialPath =
                $"{GeneratedFolder}/BT_{shape.name}_Foliage.mat";

            Material trunkMaterial =
                AssetDatabase.LoadAssetAtPath<Material>(trunkMaterialPath);
            Material foliageMaterial =
                AssetDatabase.LoadAssetAtPath<Material>(foliageMaterialPath);

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
                string prefabPath =
                    $"{GeneratedFolder}/BT_{speciesName}.prefab";
                GameObject prefab =
                    AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

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
                MeshRenderer trunkRenderer =
                    prefabTrunk.GetComponent<MeshRenderer>();
                MeshFilter canopyFilter = prefabCanopy.GetComponent<MeshFilter>();
                MeshRenderer canopyRenderer =
                    prefabCanopy.GetComponent<MeshRenderer>();

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

                CreateMeshChild(
                    resource.transform,
                    "Trunk",
                    trunkFilter.sharedMesh,
                    trunkRenderer.sharedMaterial);
                CreateMeshChild(
                    resource.transform,
                    "Canopy",
                    canopyFilter.sharedMesh,
                    canopyRenderer.sharedMaterial);

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
            const int rings = 4;
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
                    Mathf.Pow(t, 0.72f));

                for (int segment = 0; segment < segments; segment++)
                {
                    float angle =
                        segment / (float)segments * Mathf.PI * 2f;
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
                    int a = start + segment;
                    int b = start + next;
                    int c = nextStart + segment;
                    int d = nextStart + next;
                    triangles.Add(a); triangles.Add(c); triangles.Add(b);
                    triangles.Add(b); triangles.Add(c); triangles.Add(d);
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
            System.Random random =
                new System.Random(StableHash(shape.name));

            for (int tier = 0; tier < shape.tierCount; tier++)
            {
                if (tier > 1 && tier < shape.tierCount - 2 &&
                    random.NextDouble() < shape.tierSkipChance)
                {
                    continue;
                }

                float t = tier / (float)Mathf.Max(1, shape.tierCount - 1);
                float y = Mathf.Lerp(shape.crownBottom, shape.crownTop, t);
                float branchLength = Mathf.Lerp(
                    shape.lowerBranchLength,
                    shape.upperBranchLength,
                    Mathf.Pow(t, 0.82f));

                int branchCount = Mathf.Max(
                    3,
                    shape.branchesPerTier + random.Next(-1, 2));
                float tierRotation =
                    (float)random.NextDouble() * Mathf.PI * 2f;

                Vector3 centreOffset = new Vector3(
                    Mathf.Lerp(-shape.irregularity, shape.irregularity,
                        (float)random.NextDouble()),
                    0f,
                    Mathf.Lerp(-shape.irregularity, shape.irregularity,
                        (float)random.NextDouble()));

                centreOffset += new Vector3(
                    shape.tipLean * t * t,
                    0f,
                    shape.tipLean * 0.35f * t * t);

                for (int branch = 0; branch < branchCount; branch++)
                {
                    float angle = tierRotation +
                        branch / (float)branchCount * Mathf.PI * 2f;
                    float lengthVariation = Mathf.Lerp(
                        0.74f,
                        1.18f,
                        (float)random.NextDouble());
                    float widthVariation = Mathf.Lerp(
                        0.78f,
                        1.18f,
                        (float)random.NextDouble());
                    float verticalVariation = Mathf.Lerp(
                        -0.10f,
                        0.12f,
                        (float)random.NextDouble());

                    AddBranchFan(
                        vertices,
                        triangles,
                        uvs,
                        centreOffset + new Vector3(0f, y + verticalVariation, 0f),
                        angle,
                        branchLength * lengthVariation,
                        shape.branchWidth * widthVariation *
                            Mathf.Lerp(1f, 0.48f, t),
                        shape.branchThickness * Mathf.Lerp(1f, 0.55f, t),
                        shape.droop * Mathf.Lerp(1f, 0.42f, t));
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
                Mathf.Cos(angle),
                0f,
                Mathf.Sin(angle));
            Vector3 right = new Vector3(-forward.z, 0f, forward.x);

            Vector3 baseCentre = origin + forward * 0.12f;
            Vector3 middle = origin + forward * (length * 0.52f) -
                Vector3.up * (droop * 0.35f);
            Vector3 tip = origin + forward * length - Vector3.up * droop;

            int start = vertices.Count;
            vertices.Add(baseCentre - right * halfWidth * 0.35f +
                         Vector3.up * halfThickness);
            vertices.Add(baseCentre + right * halfWidth * 0.35f +
                         Vector3.up * halfThickness);
            vertices.Add(middle + right * halfWidth +
                         Vector3.up * halfThickness * 0.35f);
            vertices.Add(tip + Vector3.up * halfThickness * 0.10f);
            vertices.Add(middle - right * halfWidth +
                         Vector3.up * halfThickness * 0.35f);

            vertices.Add(baseCentre - right * halfWidth * 0.35f -
                         Vector3.up * halfThickness);
            vertices.Add(baseCentre + right * halfWidth * 0.35f -
                         Vector3.up * halfThickness);
            vertices.Add(middle + right * halfWidth -
                         Vector3.up * halfThickness * 0.35f);
            vertices.Add(tip - Vector3.up * halfThickness * 0.10f);
            vertices.Add(middle - right * halfWidth -
                         Vector3.up * halfThickness * 0.35f);

            for (int index = 0; index < 10; index++)
            {
                uvs.Add(new Vector2((index % 5) / 4f, index < 5 ? 1f : 0f));
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
                int topA = start + edge;
                int topB = start + next;
                int bottomA = start + 5 + edge;
                int bottomB = start + 5 + next;
                AddTriangle(triangles, topA, bottomA, topB);
                AddTriangle(triangles, topB, bottomA, bottomB);
            }
        }

        private static void AddCrownTip(
            List<Vector3> vertices,
            List<int> triangles,
            List<Vector2> uvs,
            TreeShape shape)
        {
            const int segments = 7;
            float baseY = shape.crownTop - 0.55f;
            float tipY = shape.trunkHeight + 0.10f;
            float radius = Mathf.Max(0.12f, shape.upperBranchLength * 0.85f);
            Vector3 lean = new Vector3(shape.tipLean, 0f, shape.tipLean * 0.35f);
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
                AddTriangle(triangles, start + index, tipIndex, start + next);
            }
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
