using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Boomtown.Characters.Editor
{
    /// <summary>
    /// Assigns a small, fixed set of persistent materials to generated characters.
    /// Material names never include colours or generated hashes, preventing path spam
    /// and Windows filename-length failures.
    /// </summary>
    [InitializeOnLoad]
    internal static class GeneratedCharacterMaterialRepair
    {
        private const string GeneratedFolder = "Assets/Boomtown/Characters/Materials/Generated";
        private static bool repairQueued;
        private static bool cleanedThisSession;

        private static readonly Dictionary<string, Color> RoleColors = new Dictionary<string, Color>
        {
            { "Skin", new Color(0.72f, 0.50f, 0.34f) },
            { "Shirt", new Color(0.72f, 0.69f, 0.59f) },
            { "Trousers", new Color(0.24f, 0.27f, 0.28f) },
            { "Vest", new Color(0.24f, 0.14f, 0.07f) },
            { "Leather", new Color(0.16f, 0.075f, 0.035f) },
            { "Boots", new Color(0.075f, 0.055f, 0.035f) },
            { "Hat", new Color(0.30f, 0.20f, 0.11f) },
            { "Hair", new Color(0.16f, 0.09f, 0.045f) },
            { "Canvas", new Color(0.29f, 0.27f, 0.19f) },
            { "Accent", new Color(0.42f, 0.12f, 0.08f) },
            { "Metal", new Color(0.55f, 0.44f, 0.22f) },
            { "Dark", new Color(0.02f, 0.018f, 0.015f) }
        };

        static GeneratedCharacterMaterialRepair()
        {
            EditorApplication.delayCall += QueueRepair;
            EditorApplication.hierarchyChanged += QueueRepair;
        }

        [MenuItem("Boomtown/Characters/Repair Generated Character Materials", priority = 20)]
        private static void RepairFromMenu()
        {
            cleanedThisSession = false;
            RepairGeneratedCharacters();
        }

        private static void QueueRepair()
        {
            if (repairQueued || EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            repairQueued = true;
            EditorApplication.delayCall += RepairGeneratedCharacters;
        }

        private static void RepairGeneratedCharacters()
        {
            repairQueued = false;
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            EnsureFolders();

            if (!cleanedThisSession)
            {
                DeleteBrokenGeneratedMaterials();
                EnsureFolders();
                cleanedThisSession = true;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            if (shader == null)
            {
                Debug.LogError("[Boomtown] No supported character shader was found.");
                return;
            }

            Dictionary<string, Material> materials = BuildMaterialLibrary(shader);
            bool changed = false;

            for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
            {
                Scene scene = SceneManager.GetSceneAt(sceneIndex);
                if (!scene.isLoaded)
                    continue;

                foreach (GameObject sceneRoot in scene.GetRootGameObjects())
                {
                    foreach (Renderer renderer in sceneRoot.GetComponentsInChildren<Renderer>(true))
                    {
                        if (!IsGeneratedCharacterRenderer(renderer))
                            continue;

                        string role = GetRole(renderer.gameObject.name);
                        Material target = materials[role];
                        if (renderer.sharedMaterial == target)
                            continue;

                        Undo.RecordObject(renderer, "Repair Generated Character Material");
                        renderer.sharedMaterial = target;
                        EditorUtility.SetDirty(renderer);
                        changed = true;
                    }
                }
            }

            if (changed)
            {
                AssetDatabase.SaveAssets();
                UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
                Debug.Log("[Boomtown] Character materials repaired with stable URP assets.");
            }
        }

        private static Dictionary<string, Material> BuildMaterialLibrary(Shader shader)
        {
            Dictionary<string, Material> result = new Dictionary<string, Material>();
            foreach (KeyValuePair<string, Color> entry in RoleColors)
                result.Add(entry.Key, LoadOrCreate(entry.Key, entry.Value, shader));
            return result;
        }

        private static Material LoadOrCreate(string role, Color color, Shader shader)
        {
            string path = GeneratedFolder + "/" + role + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material == null)
            {
                material = new Material(shader) { name = "Boomtown " + role };
                SetMaterialValues(material, color);
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = shader;
                SetMaterialValues(material, color);
                EditorUtility.SetDirty(material);
            }

            return material;
        }

        private static void SetMaterialValues(Material material, Color color)
        {
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", 0.06f);
            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", 0f);
            material.color = color;
        }

        private static string GetRole(string partName)
        {
            string name = partName.ToLowerInvariant();

            if (name.Contains("eye")) return "Dark";
            if (name.Contains("buckle")) return "Metal";
            if (name.Contains("beard") || name.Contains("moustache")) return "Hair";
            if (name.Contains("hatband") || name.Contains("belt") || name.Contains("pouch") || name.Contains("holster")) return "Leather";
            if (name.Contains("hat")) return "Hat";
            if (name.Contains("boot")) return "Boots";
            if (name.Contains("vest")) return "Vest";
            if (name.Contains("trouser") || name.Contains("pelvis") || name.Contains("thigh") || name.Contains("shin")) return "Trousers";
            if (name.Contains("neckerc") || name.Contains("bedroll")) return "Accent";
            if (name.Contains("backpack")) return "Canvas";
            if (name.Contains("head") || name.Contains("neck") || name.Contains("nose") || name.Contains("forearm") || name.Contains("hand")) return "Skin";
            return "Shirt";
        }

        private static bool IsGeneratedCharacterRenderer(Renderer renderer)
        {
            Transform current = renderer.transform;
            while (current != null)
            {
                if (current.name == "Visual")
                    return true;
                current = current.parent;
            }
            return false;
        }

        private static void DeleteBrokenGeneratedMaterials()
        {
            if (AssetDatabase.IsValidFolder(GeneratedFolder))
                AssetDatabase.DeleteAsset(GeneratedFolder);
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets", "Boomtown");
            EnsureFolder("Assets/Boomtown", "Characters");
            EnsureFolder("Assets/Boomtown/Characters", "Materials");
            EnsureFolder("Assets/Boomtown/Characters/Materials", "Generated");
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, child);
        }
    }
}
