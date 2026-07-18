using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Boomtown.WorldGeneration.Editor
{
    /// <summary>
    /// Rebuilds every regional tree prefab from procedural low-poly meshes.
    /// Called automatically by BoomtownWorldGenerator after forest generation.
    /// Each species prefab uses two renderers: one trunk and one canopy.
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

            Debug.Log(
                $"[Boomtown Tree Builder] Rebuilt {rebuilt} regional tree " +
                "prefab(s) from procedural low-poly meshes. Each tree uses " +
                "one tapered trunk renderer and one combined canopy renderer.");
        }

        private static TreeShape[] BuildShapes()
        {
            return new[]
            {
                new TreeShape { name = "DouglasFir", canopyTiers = 8, radialSegments = 9, trunkHeight = 8.2f, trunkBaseRadius = 0.34f, trunkTopRadius = 0.12f, crownBottom = 2.2f, crownTop = 8.0f, lowerRadius = 2.25f, upperRadius = 0.28f, tierHeight = 0.95f, droop = 0.12f, irregularity = 0.16f },
                new TreeShape { name = "RedCedar", canopyTiers = 9, radialSegments = 9, trunkHeight = 7.8f, trunkBaseRadius = 0.46f, trunkTopRadius = 0.14f, crownBottom = 1.5f, crownTop = 7.7f, lowerRadius = 2.55f, upperRadius = 0.34f, tierHeight = 0.82f, droop = 0.28f, irregularity = 0.22f },
                new TreeShape { name = "Hemlock", canopyTiers = 8, radialSegments = 9, trunkHeight = 8.0f, trunkBaseRadius = 0.31f, trunkTopRadius = 0.10f, crownBottom = 2.0f, crownTop = 7.9f, lowerRadius = 1.95f, upperRadius = 0.25f, tierHeight = 0.86f, droop = 0.24f, irregularity = 0.25f },
                new TreeShape { name = "Lodgepole", canopyTiers = 7, radialSegments = 8, trunkHeight = 8.5f, trunkBaseRadius = 0.25f, trunkTopRadius = 0.09f, crownBottom = 3.0f, crownTop = 8.3f, lowerRadius = 1.45f, upperRadius = 0.22f, tierHeight = 0.82f, droop = 0.06f, irregularity = 0.30f },
                new TreeShape { name = "Spruce", canopyTiers = 9, radialSegments = 9, trunkHeight = 8.1f, trunkBaseRadius = 0.32f, trunkTopRadius = 0.10f, crownBottom = 1.9f, crownTop = 8.0f, lowerRadius = 2.05f, upperRadius = 0.24f, tierHeight = 0.78f, droop = 0.16f, irregularity = 0.12f }
            };
        }

        private static bool RebuildSpeciesPrefab(TreeShape shape)
        {
            string prefabPath = $"{GeneratedFolder}/BT_{shape.name}.prefab";
            string trunkMaterialPath = $"{GeneratedFolder}/BT_{shape.name}_Trunk.mat";
            string foliageMaterialPath = $"{GeneratedFolder}/BT_{shape.name}_Foliage.mat";

            Material trunkMaterial = AssetDatabase.LoadAssetAtPath<Material>(trunkMaterialPath);
            Material foliageMaterial = AssetDatabase.LoadAssetAtPath<Material>(foliageMaterialPath);

            if (trunkMaterial == null || foliageMaterial == null)
            {
                return false;
            }

            Mesh trunkMesh = BuildTaperedTrunkMesh(shape);
            Mesh canopyMesh = BuildCanopyMesh(shape);

            string trunkMeshPath = $"{GeneratedFolder}/BT_{shape.name}_TrunkMesh.asset";
            string canopyMeshPath = $"{GeneratedFolder}/BT_{shape.name}_CanopyMesh.asset";

            AssetDatabase.DeleteAsset(trunkMeshPath);
            AssetDatabase.DeleteAsset(canopyMeshPath);
            AssetDatabase.CreateAsset(trunkMesh, trunkMeshPath);
            AssetDatabase.CreateAsset(canopyMesh, canopyMeshPath);

            GameObject root = new GameObject($"BT_{shape.name}");

            GameObject trunk = new GameObject("Trunk");
            trunk.transform.SetParent(root.transform, false);
            trunk.AddComponent<MeshFilter>().sharedMesh = trunkMesh;
            MeshRenderer trunkRenderer = trunk.AddComponent<MeshRenderer>();
            trunkRenderer.sharedMaterial = trunkMaterial;
            trunkRenderer.shadowCastingMode = ShadowCastingMode.On;
            trunkRenderer.receiveShadows = true;

            GameObject canopy = new GameObject("Canopy");
            canopy.transform.SetParent(root.transform, false);
            canopy.AddComponent<MeshFilter>().sharedMesh = canopyMesh;
            MeshRenderer canopyRenderer = canopy.AddComponent<MeshRenderer>();
            canopyRenderer.sharedMaterial = foliageMaterial;
            canopyRenderer.shadowCastingMode = ShadowCastingMode.On;
            canopyRenderer.receiveShadows = true;

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return true;
        }

        private static Mesh BuildTaperedTrunkMesh(TreeShape shape)
        {
            const int segments = 10;
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            List<Vector2> uvs = new List<Vector2>();

            for (int ring = 0; ring < 2; ring++)
            {
                float y = ring == 0 ? 0f : shape.trunkHeight;
                float radius = ring == 0 ? shape.trunkBaseRadius : shape.trunkTopRadius;

                for (int i = 0; i < segments; i++)
                {
                    float angle = i / (float)segments * Mathf.PI * 2f;
                    vertices.Add(new Vector3(Mathf.Cos(angle) * radius, y, Mathf.Sin(angle) * radius));
                    uvs.Add(new Vector2(i / (float)segments, ring));
                }
            }

            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                int a = i;
                int b = next;
                int c = segments + i;
                int d = segments + next;
                triangles.Add(a); triangles.Add(c); triangles.Add(b);
                triangles.Add(b); triangles.Add(c); triangles.Add(d);
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

            for (int tier = 0; tier < shape.canopyTiers; tier++)
            {
                float t = tier / (float)Mathf.Max(1, shape.canopyTiers - 1);
                float centreY = Mathf.Lerp(shape.crownBottom, shape.crownTop, t);
                float radius = Mathf.Lerp(shape.lowerRadius, shape.upperRadius, t);
                float offsetX = Mathf.Lerp(-shape.irregularity, shape.irregularity, (float)random.NextDouble());
                float offsetZ = Mathf.Lerp(-shape.irregularity, shape.irregularity, (float)random.NextDouble());
                float ellipse = Mathf.Lerp(0.82f, 1.16f, (float)random.NextDouble());
                float rotation = (float)random.NextDouble() * Mathf.PI * 2f;

                AddCanopyTier(vertices, triangles, uvs, shape.radialSegments,
                    new Vector3(offsetX, 0f, offsetZ),
                    centreY - shape.droop,
                    centreY + shape.tierHeight,
                    radius,
                    radius * 0.18f,
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
                    float angle = i / (float)segments * Mathf.PI * 2f + rotation;
                    float x = Mathf.Cos(angle) * radius * ellipse;
                    float z = Mathf.Sin(angle) * radius / ellipse;
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
