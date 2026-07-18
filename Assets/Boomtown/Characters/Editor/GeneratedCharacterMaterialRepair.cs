using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Boomtown.Characters.Editor
{
    /// <summary>
    /// Converts the character generator's temporary materials into persistent URP
    /// material assets. This prevents generated characters from becoming magenta
    /// after a script reload, scene save, or Unity restart.
    /// </summary>
    [InitializeOnLoad]
    internal static class GeneratedCharacterMaterialRepair
    {
        private const string MaterialRoot = "Assets/Boomtown/Characters/Materials";
        private const string GeneratedFolder = MaterialRoot + "/Generated";
        private static bool repairQueued;

        static GeneratedCharacterMaterialRepair()
        {
            EditorApplication.delayCall += QueueRepair;
            EditorApplication.hierarchyChanged += QueueRepair;
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

            Shader shader = FindSupportedShader();
            if (shader == null)
            {
                Debug.LogError("[Boomtown] Could not find URP Lit or Standard shader for generated character materials.");
                return;
            }

            bool changed = false;
            Dictionary<string, Material> cache = new Dictionary<string, Material>();

            for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
            {
                Scene scene = SceneManager.GetSceneAt(sceneIndex);
                if (!scene.isLoaded)
                    continue;

                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
                    foreach (Renderer renderer in renderers)
                    {
                        if (!IsGeneratedCharacterRenderer(renderer))
                            continue;

                        Material source = renderer.sharedMaterial;
                        if (source == null)
                            continue;

                        Color color = ReadColor(source);
                        string role = CleanRoleName(source.name);
                        string key = role + "_" + ColorUtility.ToHtmlStringRGBA(color);

                        if (!cache.TryGetValue(key, out Material persistent))
                        {
                            persistent = LoadOrCreateMaterial(key, role, color, shader);
                            cache.Add(key, persistent);
                        }

                        if (renderer.sharedMaterial != persistent)
                        {
                            Undo.RecordObject(renderer, "Repair Generated Character Material");
                            renderer.sharedMaterial = persistent;
                            EditorUtility.SetDirty(renderer);
                            changed = true;
                        }
                    }
                }
            }

            if (changed)
            {
                AssetDatabase.SaveAssets();
                UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
                Debug.Log("[Boomtown] Repaired generated character materials. Magenta materials should now be gone.");
            }
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

        private static Material LoadOrCreateMaterial(string key, string role, Color color, Shader shader)
        {
            string path = GeneratedFolder + "/" + key + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material == null)
            {
                material = new Material(shader)
                {
                    name = "Boomtown Generated " + role
                };
                SetColor(material, color);
                SetSurfaceValues(material);
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                if (material.shader == null || material.shader.name == "Hidden/InternalErrorShader")
                    material.shader = shader;

                SetColor(material, color);
                SetSurfaceValues(material);
                EditorUtility.SetDirty(material);
            }

            return material;
        }

        private static Shader FindSupportedShader()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");
            return shader;
        }

        private static Color ReadColor(Material material)
        {
            if (material.HasProperty("_BaseColor"))
                return material.GetColor("_BaseColor");
            if (material.HasProperty("_Color"))
                return material.GetColor("_Color");
            return Color.gray;
        }

        private static void SetColor(Material material, Color color)
        {
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
            material.color = color;
        }

        private static void SetSurfaceValues(Material material)
        {
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", 0.06f);
            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", 0f);
        }

        private static string CleanRoleName(string materialName)
        {
            string role = materialName
                .Replace("Boomtown Generated ", string.Empty)
                .Replace("Generated ", string.Empty)
                .Trim();

            if (string.IsNullOrWhiteSpace(role))
                role = "Material";

            foreach (char invalid in System.IO.Path.GetInvalidFileNameChars())
                role = role.Replace(invalid, '_');

            return role.Replace(' ', '_');
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets", "Boomtown");
            EnsureFolder("Assets/Boomtown", "Characters");
            EnsureFolder("Assets/Boomtown/Characters", "Materials");
            EnsureFolder(MaterialRoot, "Generated");
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, child);
        }
    }
}
