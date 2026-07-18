using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Boomtown.WorldGeneration.Editor
{
    /// <summary>
    /// Combines the procedural canopy layers in each generated species prefab
    /// into one shared canopy mesh. Each tree prefab then uses only two renderers:
    /// one trunk and one canopy.
    /// </summary>
    [InitializeOnLoad]
    public static class TreePrefabMeshOptimizer
    {
        private const string GeneratedFolder =
            "Assets/Boomtown/Scripts/WorldGeneration/Generated";

        private static readonly string[] SpeciesNames =
        {
            "DouglasFir",
            "RedCedar",
            "Hemlock",
            "Lodgepole",
            "Spruce"
        };

        private static bool optimizationQueued;
        private static bool optimizing;

        static TreePrefabMeshOptimizer()
        {
            EditorApplication.hierarchyChanged += QueueOptimization;
            EditorApplication.delayCall += OptimizeGeneratedTreePrefabs;
        }

        [MenuItem("Boomtown/World Generation/Optimize Generated Tree Prefabs")]
        public static void OptimizeGeneratedTreePrefabs()
        {
            if (optimizing)
            {
                return;
            }

            optimizing = true;
            optimizationQueued = false;

            try
            {
                int optimizedCount = 0;

                foreach (string speciesName in SpeciesNames)
                {
                    if (OptimizeSpeciesPrefab(speciesName))
                    {
                        optimizedCount++;
                    }
                }

                if (optimizedCount > 0)
                {
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();

                    Debug.Log(
                        $"[Tree Prefab Optimizer] Combined canopy layers for " +
                        $"{optimizedCount} species prefab(s). Each tree now uses " +
                        "one trunk renderer and one canopy renderer.");
                }
            }
            finally
            {
                optimizing = false;
            }
        }

        private static void QueueOptimization()
        {
            if (optimizing || optimizationQueued)
            {
                return;
            }

            optimizationQueued = true;
            EditorApplication.delayCall += OptimizeGeneratedTreePrefabs;
        }

        private static bool OptimizeSpeciesPrefab(string speciesName)
        {
            string prefabPath =
                $"{GeneratedFolder}/BT_{speciesName}.prefab";

            GameObject prefabAsset =
                AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (prefabAsset == null)
            {
                return false;
            }

            Transform existingCombinedCanopy =
                prefabAsset.transform.Find("Canopy");

            if (existingCombinedCanopy != null &&
                prefabAsset.transform.childCount <= 2)
            {
                return false;
            }

            GameObject prefabRoot =
                PrefabUtility.LoadPrefabContents(prefabPath);

            try
            {
                List<MeshFilter> canopyFilters = new List<MeshFilter>();
                Material canopyMaterial = null;

                for (int childIndex = 0;
                     childIndex < prefabRoot.transform.childCount;
                     childIndex++)
                {
                    Transform child = prefabRoot.transform.GetChild(childIndex);

                    if (!child.name.StartsWith("Canopy_"))
                    {
                        continue;
                    }

                    MeshFilter filter = child.GetComponent<MeshFilter>();
                    MeshRenderer renderer = child.GetComponent<MeshRenderer>();

                    if (filter == null || filter.sharedMesh == null)
                    {
                        continue;
                    }

                    canopyFilters.Add(filter);

                    if (canopyMaterial == null && renderer != null)
                    {
                        canopyMaterial = renderer.sharedMaterial;
                    }
                }

                if (canopyFilters.Count <= 1)
                {
                    return false;
                }

                CombineInstance[] combineInstances =
                    new CombineInstance[canopyFilters.Count];

                Matrix4x4 rootToLocal =
                    prefabRoot.transform.worldToLocalMatrix;

                for (int index = 0; index < canopyFilters.Count; index++)
                {
                    MeshFilter filter = canopyFilters[index];
                    combineInstances[index] = new CombineInstance
                    {
                        mesh = filter.sharedMesh,
                        transform =
                            rootToLocal * filter.transform.localToWorldMatrix
                    };
                }

                Mesh combinedMesh = new Mesh
                {
                    name = $"BT_{speciesName}_CanopyMesh",
                    indexFormat = IndexFormat.UInt32
                };

                combinedMesh.CombineMeshes(
                    combineInstances,
                    true,
                    true,
                    false);
                combinedMesh.RecalculateBounds();
                combinedMesh.RecalculateNormals();
                combinedMesh.Optimize();

                string meshPath =
                    $"{GeneratedFolder}/BT_{speciesName}_CanopyMesh.asset";

                AssetDatabase.DeleteAsset(meshPath);
                AssetDatabase.CreateAsset(combinedMesh, meshPath);

                for (int index = canopyFilters.Count - 1;
                     index >= 0;
                     index--)
                {
                    Object.DestroyImmediate(
                        canopyFilters[index].gameObject);
                }

                Transform oldCombinedCanopy =
                    prefabRoot.transform.Find("Canopy");

                if (oldCombinedCanopy != null)
                {
                    Object.DestroyImmediate(oldCombinedCanopy.gameObject);
                }

                GameObject canopy = new GameObject("Canopy");
                canopy.transform.SetParent(prefabRoot.transform, false);

                MeshFilter combinedFilter =
                    canopy.AddComponent<MeshFilter>();
                MeshRenderer combinedRenderer =
                    canopy.AddComponent<MeshRenderer>();

                combinedFilter.sharedMesh = combinedMesh;
                combinedRenderer.sharedMaterial = canopyMaterial;

                PrefabUtility.SaveAsPrefabAsset(
                    prefabRoot,
                    prefabPath);

                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }
    }
}
