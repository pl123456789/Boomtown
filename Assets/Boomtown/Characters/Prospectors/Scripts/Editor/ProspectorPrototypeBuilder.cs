using System.IO;
using UnityEditor;
using UnityEngine;

namespace Boomtown.Characters.Prospectors.Editor
{
    /// <summary>
    /// Builds a lightweight Bill Parsons visual prototype beneath the existing Bill gameplay root.
    /// The gameplay object and its components are never replaced.
    /// </summary>
    public static class ProspectorPrototypeBuilder
    {
        private const string CharacterRoot = "Assets/Boomtown/Characters";
        private const string MaterialFolder = CharacterRoot + "/Materials";
        private const string PrefabFolder = CharacterRoot + "/Prospectors/Prefabs";
        private const string PrefabPath = PrefabFolder + "/Bill_Prototype.prefab";

        [MenuItem("Boomtown/Characters/Build Bill Prototype")]
        public static void BuildBillPrototype()
        {
            EnsureFolder(CharacterRoot, "Materials");
            EnsureFolder(CharacterRoot + "/Prospectors", "Prefabs");

            GameObject billRoot = GameObject.Find("Bill");
            if (billRoot == null)
            {
                EditorUtility.DisplayDialog(
                    "Bill not found",
                    "Open the Boomtown gameplay scene and make sure the existing root object is named 'Bill'.",
                    "OK");
                return;
            }

            Transform oldVisual = billRoot.transform.Find("Visual");
            if (oldVisual != null)
            {
                Undo.DestroyObjectImmediate(oldVisual.gameObject);
            }

            GameObject visual = new GameObject("Visual");
            Undo.RegisterCreatedObjectUndo(visual, "Build Bill Prototype");
            visual.transform.SetParent(billRoot.transform, false);

            Material skin = GetOrCreateMaterial("BT_Prospector_Skin", new Color(0.47f, 0.28f, 0.18f));
            Material shirt = GetOrCreateMaterial("BT_Prospector_Shirt_Cream", new Color(0.72f, 0.67f, 0.53f));
            Material vest = GetOrCreateMaterial("BT_Prospector_Vest_Brown", new Color(0.22f, 0.12f, 0.065f));
            Material trousers = GetOrCreateMaterial("BT_Prospector_Trousers_Charcoal", new Color(0.12f, 0.105f, 0.09f));
            Material leather = GetOrCreateMaterial("BT_Prospector_Leather", new Color(0.15f, 0.075f, 0.035f));
            Material scarf = GetOrCreateMaterial("BT_Prospector_Scarf_Red", new Color(0.36f, 0.055f, 0.035f));
            Material beard = GetOrCreateMaterial("BT_Prospector_Hair_Dark", new Color(0.075f, 0.045f, 0.025f));

            // Approximate human scale: 1.78 metres tall, grounded at local Y = 0.
            CreatePart("Torso_Shirt", PrimitiveType.Capsule, visual.transform,
                new Vector3(0f, 1.18f, 0f), new Vector3(0.42f, 0.42f, 0.28f), shirt);
            CreatePart("Vest", PrimitiveType.Cube, visual.transform,
                new Vector3(0f, 1.22f, -0.015f), new Vector3(0.47f, 0.58f, 0.26f), vest);
            CreatePart("Head", PrimitiveType.Sphere, visual.transform,
                new Vector3(0f, 1.68f, 0f), new Vector3(0.28f, 0.32f, 0.28f), skin);
            CreatePart("Beard", PrimitiveType.Sphere, visual.transform,
                new Vector3(0f, 1.56f, -0.075f), new Vector3(0.29f, 0.22f, 0.22f), beard);
            CreatePart("Neckerchief", PrimitiveType.Cube, visual.transform,
                new Vector3(0f, 1.44f, -0.17f), new Vector3(0.30f, 0.12f, 0.06f), scarf);

            CreateLimb("Arm_L", visual.transform, new Vector3(-0.34f, 1.20f, 0f), new Vector3(0.14f, 0.58f, 0.14f), shirt);
            CreateLimb("Arm_R", visual.transform, new Vector3(0.34f, 1.20f, 0f), new Vector3(0.14f, 0.58f, 0.14f), shirt);
            CreateLimb("Leg_L", visual.transform, new Vector3(-0.14f, 0.57f, 0f), new Vector3(0.18f, 0.78f, 0.20f), trousers);
            CreateLimb("Leg_R", visual.transform, new Vector3(0.14f, 0.57f, 0f), new Vector3(0.18f, 0.78f, 0.20f), trousers);
            CreatePart("Boot_L", PrimitiveType.Cube, visual.transform,
                new Vector3(-0.14f, 0.13f, -0.055f), new Vector3(0.22f, 0.24f, 0.38f), leather);
            CreatePart("Boot_R", PrimitiveType.Cube, visual.transform,
                new Vector3(0.14f, 0.13f, -0.055f), new Vector3(0.22f, 0.24f, 0.38f), leather);

            CreatePart("Belt", PrimitiveType.Cube, visual.transform,
                new Vector3(0f, 0.93f, -0.02f), new Vector3(0.52f, 0.09f, 0.31f), leather);
            CreatePart("Belt_Pouch", PrimitiveType.Cube, visual.transform,
                new Vector3(0.30f, 0.88f, -0.03f), new Vector3(0.16f, 0.22f, 0.13f), leather);

            CreateHat(visual.transform, leather);
            CreateBackpack(visual.transform, leather);
            CreateGoldPan(visual.transform);

            DisableAllColliders(visual);

            PrefabUtility.SaveAsPrefabAssetAndConnect(visual, PrefabPath, InteractionMode.UserAction);
            Selection.activeGameObject = visual;
            EditorGUIUtility.PingObject(visual);
            EditorUtility.SetDirty(billRoot);

            Debug.Log("[Boomtown] Bill Parsons prototype created under Bill/Visual and saved as " + PrefabPath);
        }

        private static void CreateHat(Transform parent, Material material)
        {
            CreatePart("Hat_Brim", PrimitiveType.Cylinder, parent,
                new Vector3(0f, 1.91f, 0f), new Vector3(0.43f, 0.025f, 0.43f), material);
            CreatePart("Hat_Crown", PrimitiveType.Cylinder, parent,
                new Vector3(0f, 2.00f, 0f), new Vector3(0.27f, 0.16f, 0.27f), material);
        }

        private static void CreateBackpack(Transform parent, Material material)
        {
            CreatePart("Backpack", PrimitiveType.Cube, parent,
                new Vector3(0f, 1.21f, 0.23f), new Vector3(0.42f, 0.52f, 0.18f), material);
        }

        private static void CreateGoldPan(Transform parent)
        {
            Material metal = GetOrCreateMaterial("BT_Prospector_Metal", new Color(0.18f, 0.19f, 0.19f));
            GameObject pan = CreatePart("GoldPan", PrimitiveType.Cylinder, parent,
                new Vector3(-0.35f, 0.78f, 0.08f), new Vector3(0.28f, 0.035f, 0.28f), metal);
            pan.transform.localRotation = Quaternion.Euler(90f, 0f, 12f);
        }

        private static void CreateLimb(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            CreatePart(name, PrimitiveType.Capsule, parent, position, scale, material);
        }

        private static GameObject CreatePart(
            string name,
            PrimitiveType primitive,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Material material)
        {
            GameObject part = GameObject.CreatePrimitive(primitive);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;

            Renderer renderer = part.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            return part;
        }

        private static void DisableAllColliders(GameObject root)
        {
            foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
            {
                Object.DestroyImmediate(collider);
            }
        }

        private static Material GetOrCreateMaterial(string name, Color colour)
        {
            string path = MaterialFolder + "/" + name + ".mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                return existing;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            Material material = new Material(shader)
            {
                name = name,
                color = colour
            };

            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }
    }
}
