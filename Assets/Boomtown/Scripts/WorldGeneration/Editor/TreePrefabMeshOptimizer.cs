using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Boomtown.WorldGeneration.Editor
{
    /// <summary>
    /// Rebuilds every regional tree prefab from procedural low-poly meshes and
    /// immediately applies those meshes to the trees already created by the
    /// world generator. Each tree uses two renderers: trunk and canopy.
    /// </summary>
    public static class TreePrefabMeshOptimizer
    {
        private const string GeneratedFolder =
            "Assets/Boomtown/Scripts/WorldGeneration/Generated";

        private struct TreeShape
        {
            public string name;
            public int canopyTiers;
            public int radialSegments;
            public float trunkHeight;
            public float trunkBaseRadius;
            public float trunkTopRadius;
            public float crownBottom;
            public float crownTop;
            public float lowerRadius;
            public float upperRadius;
            public float tierHeight;
            public float droop;
            public float irregularity;
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
                "instance(s). Each tree now uses one tapered trunk renderer " +
                "and one procedural canopy renderer.");
        }

        private static TreeShape[] BuildShapes()
        {
            return new[]
            {
                new TreeShape { name = "DouglasFir", canopyTiers = 10, radialSegments = 10, trunkHeight = 8.4f, trunkBaseRadius = 0.36f, trunkTopRadius = 0.10f, crownBottom = 2.0f, crownTop = 8.2f, lowerRadius = 2.35f, upperRadius = 0.20f, tierHeight = 0.78f, droop = 0.12f, irregularity = 0.18f },
                new TreeShape { name = "RedCedar", canopyTiers = 11, radialSegments = 10, trunkHeight = 8.0f, trunkBaseRadius = 0.50f, trunkTopRadius = 0.13f, crownBottom = 1.2f, crownTop = 7.9f, lowerRadius = 2.75f, upperRadius = 0.26f, tierHeight = 0.68f, droop = 0.34f, irregularity = 0.24f },
                new TreeShape { name = "Hemlock", canopyTiers = 10, radialSegments = 10, trunkHeight = 8.2f, trunkBaseRadius = 0.32f, trunkTopRadius = 0.09f, crownBottom = 1.8f, crownTop = 8.0f, lowerRadius = 2.05f, upperRadius = 0.18f, tierHeight = 0.72f, droop = 0.30f, irregularity = 0.30f },
                new TreeShape { name = "Lodgepole", canopyTiers = 8, radialSegments = 9, trunkHeight = 8.7f, trunkBaseRadius = 0.25f, trunkTopRadius = 0.07f, crownBottom = 3.1f, crownTop = 8.5f, lowerRadius = 1.48f, upperRadius = 0.16f, tierHeight = 0.68f, droop = 0.05f, irregularity = 0.34f },
                new TreeShape { name = "Spruce", canopyTiers = 11, radialSegments = 10, trunkHeight = 8.3f, trunkBaseRadius = 0.33f, trunkTopRadius = 0.08f, crownBottom = 1.6f, crownTop = 8.15f, lowerRadius = 2.20f, upperRadius = 0.18f, tierHeight = 0.66f, droop = 0.18f, irregularity = 0.14f }
            };
        }

        private static bool RebuildSpeciesPrefab(TreeShape shape)
        {
            string prefabPath = $"{GeneratedFolder}/BT_{shape.name}.prefab";
            string trunkMaterialPath = $"{GeneratedFolder}/BT_{shape.name}_Trunk.mat";
            string foliageMaterialPath = $"{GeneratedFolder}/BT_{shape.name}_Foliage.mat";

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
            TreeResource[] resources =
                Object.FindObjectsByType<TreeResource>(
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

                for (int childIndex = resource.transform.childCount - 1;
                     childIndex >= 0;
                     childIndex--)
                {
                    Object.DestroyImmediate(
                        resource.transform.GetChild(childIndex).gameObject);
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

            MeshFilter filter = child.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;

            MeshRenderer renderer = child.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }

        private static Mesh BuildTaperedTrunkMesh(TreeShape shape)
        {
            const int segments = 10;
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            List<Vector2> uvs = new List<Vector2>();

            for (int ring = 0; ring < 3; ring++)
            {
                float t = ring / 2f;
                float y = shape.trunkHeight * t;
                float radius = Mathf.Lerp(
                    shape.trunkBaseRadius,
                    shape.trunkTopRadius,
                    Mathf.Pow(t, 0.72f));

                for (int i = 0; i < segments; i++)
                {
                    float angle = i / (float)segments * Mathf.PI * 2f;
                    vertices.Add(new Vector3(
                        Mathf.Cos(angle) * radius,
                        y,
                        Mathf.Sin(angle) * radius));
                    uvs.Add(new Vector2(i / (float)segments, t));
                }
            }

            for (int ring = 0; ring < 2; ring++)
            {
                int ringStart = ring * segments;
                int nextRingStart = (ring + 1) * segments;

                for (int i = 0; i < segments; i++)
                {
                    int next = (i + 1) % segments;
                    int a = ringStart + i;
                    int b = ringStart + next;
                    int c = nextRingStart + i;
                    int d = nextRingStart + next;
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

            for (int tier = 0; tier < shape.canopyTiers; tier++)
            {
                float t = tier /
                    (float)Mathf.Max(1, shape.canopyTiers - 1);
                float centreY =
                    Mathf.Lerp(shape.crownBottom, shape.crownTop, t);
                float radius = Mathf.Lerp(
                    shape.lowerRadius,
                    shape.upperRadius,
                    Mathf.Pow(t, 0.88f));

                float offsetX = Mathf.Lerp(
                    -shape.irregularity,
                    shape.irregularity,
                    (float)random.NextDouble());
                float offsetZ = Mathf.Lerp(
                    -shape.irregularity,
                    shape.irregularity,
                    (float)random.NextDouble());
                float ellipse = Mathf.Lerp(
                    0.78f,
                    1.22f,
                    (float)random.NextDouble());
                float rotation =
                    (float)random.NextDouble() * Mathf.PI * 2f;

                AddCanopyTier(
                    vertices,
                    triangles,
                    uvs,
                    shape.radialSegments,
                    new Vector3(offsetX, 0f, offsetZ),
                    centreY - shape.droop,
                    centreY + shape.tierHeight,
                    radius,
                    radius * 0.10f,
                    ellipse,
                    rotation);
            }

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

        private static void AddCanopyTier(
            List<Vector3> vertices,
            List<int> triangles,
            List<Vector2> uvs,
            int segments,
            Vector3 offset,
            float lowerY,
            float upperY,
            float lowerRadius,
            float upperRadius,
            float ellipse,
            float rotation)
        {
            int start = vertices.Count;

            for (int ring = 0; ring < 2; ring++)
            {
                float y = ring == 0 ? lowerY : upperY;
                float radius = ring == 0 ? lowerRadius : upperRadius;

                for (int i = 0; i < segments; i++)
                {
                    float angle =
                        i / (float)segments * Mathf.PI * 2f + rotation;
                    float edgeNoise =
                        1f + Mathf.Sin(i * 2.31f + rotation) * 0.08f;
                    float x =
                        Mathf.Cos(angle) * radius * ellipse * edgeNoise;
                    float z =
                        Mathf.Sin(angle) * radius / ellipse * edgeNoise;
                    vertices.Add(offset + new Vector3(x, y, z));
                    uvs.Add(new Vector2(i / (float)segments, ring));
                }
            }

            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                int a = start + i;
                int b = start + next;
                int c = start + segments + i;
                int d = start + segments + next;
                triangles.Add(a); triangles.Add(c); triangles.Add(b);
                triangles.Add(b); triangles.Add(c); triangles.Add(d);
            }

            int topCentre = vertices.Count;
            vertices.Add(offset + new Vector3(0f, upperY, 0f));
            uvs.Add(new Vector2(0.5f, 1f));

            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                triangles.Add(topCentre);
                triangles.Add(start + segments + i);
                triangles.Add(start + segments + next);
            }
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
