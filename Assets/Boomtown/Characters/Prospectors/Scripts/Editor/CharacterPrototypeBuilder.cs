using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds a clean, stylized prototype character beneath the selected character's Visual child.
///
/// Usage:
/// 1. Select Bill (or another character root) in the Hierarchy.
/// 2. Choose Boomtown > Characters > Build Character Prototype.
/// 3. The root capsule MeshRenderer is disabled, but colliders/controllers remain untouched.
///
/// This is programmer art by design. The hierarchy uses joint pivots so the visual can
/// later be animated or replaced without changing gameplay scripts on the character root.
/// </summary>
public static class CharacterBuildPrototyper
{
    private const string VisualName = "Visual";
    private const string GeneratedRootName = "Prototype_Character";
    private const string MaterialFolder = "Assets/Boomtown/Characters/Materials/Prototype";

    private static readonly Color SkinColor = new(0.66f, 0.43f, 0.28f);
    private static readonly Color ShirtColor = new(0.20f, 0.30f, 0.34f);
    private static readonly Color PantsColor = new(0.16f, 0.14f, 0.13f);
    private static readonly Color BootColor = new(0.10f, 0.075f, 0.055f);
    private static readonly Color LeatherColor = new(0.25f, 0.12f, 0.055f);
    private static readonly Color HatColor = new(0.18f, 0.105f, 0.045f);
    private static readonly Color BeardColor = new(0.09f, 0.055f, 0.035f);
    private static readonly Color MetalColor = new(0.30f, 0.32f, 0.33f);

    [MenuItem("Boomtown/Characters/Build Character Prototype", priority = 100)]
    public static void BuildSelectedCharacterPrototype()
    {
        GameObject characterRoot = Selection.activeGameObject;

        if (characterRoot == null)
        {
            EditorUtility.DisplayDialog(
                "Boomtown Character Builder",
                "Select Bill or another character root in the Hierarchy first.",
                "OK");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(characterRoot, "Build Character Prototype");

        DisableGameplayCapsuleRenderer(characterRoot);

        Transform visual = FindOrCreateChild(characterRoot.transform, VisualName);
        ClearGeneratedVisual(visual);

        GameObject generatedRoot = new(GeneratedRootName);
        Undo.RegisterCreatedObjectUndo(generatedRoot, "Create Character Prototype");
        generatedRoot.transform.SetParent(visual, false);
        generatedRoot.transform.localPosition = Vector3.zero;
        generatedRoot.transform.localRotation = Quaternion.identity;
        generatedRoot.transform.localScale = Vector3.one;

        Materials materials = LoadMaterials();

        BuildBody(generatedRoot.transform, materials);
        BuildSimpleGear(generatedRoot.transform, materials);

        Selection.activeGameObject = generatedRoot;
        EditorUtility.SetDirty(characterRoot);

        Debug.Log(
            $"[Boomtown] Built clean character prototype under " +
            $"{characterRoot.name}/{VisualName}/{GeneratedRootName}. " +
            "The root capsule renderer was disabled; gameplay components were preserved.");
    }

    [MenuItem("Boomtown/Characters/Build Character Prototype", true)]
    private static bool ValidateBuildSelectedCharacterPrototype()
    {
        return Selection.activeGameObject != null;
    }

    private static void DisableGameplayCapsuleRenderer(GameObject characterRoot)
    {
        MeshFilter rootMeshFilter = characterRoot.GetComponent<MeshFilter>();
        MeshRenderer rootMeshRenderer = characterRoot.GetComponent<MeshRenderer>();

        if (rootMeshFilter != null &&
            rootMeshFilter.sharedMesh != null &&
            rootMeshFilter.sharedMesh.name.ToLowerInvariant().Contains("capsule") &&
            rootMeshRenderer != null)
        {
            Undo.RecordObject(rootMeshRenderer, "Hide Gameplay Capsule");
            rootMeshRenderer.enabled = false;
            EditorUtility.SetDirty(rootMeshRenderer);
        }
    }

    private static Transform FindOrCreateChild(Transform parent, string childName)
    {
        Transform existing = parent.Find(childName);
        if (existing != null)
        {
            return existing;
        }

        GameObject child = new(childName);
        Undo.RegisterCreatedObjectUndo(child, $"Create {childName}");
        child.transform.SetParent(parent, false);
        return child.transform;
    }

    private static void ClearGeneratedVisual(Transform visual)
    {
        Transform generated = visual.Find(GeneratedRootName);
        if (generated != null)
        {
            Undo.DestroyObjectImmediate(generated.gameObject);
        }

        string[] oldPartNames =
        {
            "Torso_Shirt", "Vest", "Head", "Beard", "Neckerchief",
            "Arm_L", "Arm_R", "Leg_L", "Leg_R", "Boot_L", "Boot_R",
            "Belt", "Belt_Pouch", "Hat_Brim", "Hat_Crown",
            "Backpack", "GoldPan"
        };

        foreach (string oldPartName in oldPartNames)
        {
            Transform oldPart = visual.Find(oldPartName);
            if (oldPart != null)
            {
                Undo.DestroyObjectImmediate(oldPart.gameObject);
            }
        }
    }

    private static void BuildBody(Transform root, Materials m)
    {
        Transform pelvis = CreatePivot(root, "Pelvis", new Vector3(0f, 0.91f, 0f));

        CreatePrimitive(pelvis, "Hips", PrimitiveType.Cube,
            new Vector3(0f, 0.03f, 0f), new Vector3(0.56f, 0.26f, 0.34f), m.Pants);

        CreatePrimitive(pelvis, "Waist", PrimitiveType.Cube,
            new Vector3(0f, 0.28f, 0f), new Vector3(0.52f, 0.34f, 0.33f), m.Shirt);

        CreatePrimitive(pelvis, "Chest", PrimitiveType.Cube,
            new Vector3(0f, 0.58f, 0f), new Vector3(0.68f, 0.42f, 0.38f), m.Shirt);

        CreatePrimitive(pelvis, "Vest_Left", PrimitiveType.Cube,
            new Vector3(-0.18f, 0.56f, -0.205f), new Vector3(0.25f, 0.38f, 0.035f), m.Leather);

        CreatePrimitive(pelvis, "Vest_Right", PrimitiveType.Cube,
            new Vector3(0.18f, 0.56f, -0.205f), new Vector3(0.25f, 0.38f, 0.035f), m.Leather);

        CreatePrimitive(pelvis, "Belt", PrimitiveType.Cube,
            new Vector3(0f, 0.10f, 0f), new Vector3(0.59f, 0.075f, 0.36f), m.Leather);

        CreatePrimitive(pelvis, "Buckle", PrimitiveType.Cube,
            new Vector3(0f, 0.10f, -0.195f), new Vector3(0.10f, 0.085f, 0.035f), m.Metal);

        BuildLeg(pelvis, "LeftLeg", -0.17f, m);
        BuildLeg(pelvis, "RightLeg", 0.17f, m);
        BuildArm(pelvis, "LeftArm", -0.43f, m);
        BuildArm(pelvis, "RightArm", 0.43f, m);
        BuildHead(pelvis, m);
    }

    private static void BuildLeg(Transform pelvis, string name, float x, Materials m)
    {
        Transform upperLegPivot = CreatePivot(pelvis, name + "_UpperPivot", new Vector3(x, -0.13f, 0f));

        CreatePrimitive(upperLegPivot, name + "_Upper", PrimitiveType.Cylinder,
            new Vector3(0f, -0.25f, 0f), new Vector3(0.19f, 0.25f, 0.19f), m.Pants);

        Transform lowerLegPivot = CreatePivot(upperLegPivot, name + "_KneePivot", new Vector3(0f, -0.50f, 0f));

        CreatePrimitive(lowerLegPivot, name + "_Lower", PrimitiveType.Cylinder,
            new Vector3(0f, -0.21f, 0f), new Vector3(0.16f, 0.21f, 0.16f), m.Pants);

        CreatePrimitive(lowerLegPivot, name + "_Boot", PrimitiveType.Cube,
            new Vector3(0f, -0.45f, -0.055f), new Vector3(0.28f, 0.20f, 0.43f), m.Boot);
    }

    private static void BuildArm(Transform pelvis, string name, float x, Materials m)
    {
        Transform shoulderPivot = CreatePivot(pelvis, name + "_ShoulderPivot", new Vector3(x, 0.69f, 0f));
        shoulderPivot.localRotation = Quaternion.Euler(0f, 0f, x < 0f ? -4f : 4f);

        CreatePrimitive(shoulderPivot, name + "_Upper", PrimitiveType.Cylinder,
            new Vector3(0f, -0.22f, 0f), new Vector3(0.145f, 0.22f, 0.145f), m.Shirt);

        Transform elbowPivot = CreatePivot(shoulderPivot, name + "_ElbowPivot", new Vector3(0f, -0.44f, 0f));

        CreatePrimitive(elbowPivot, name + "_Lower", PrimitiveType.Cylinder,
            new Vector3(0f, -0.19f, 0f), new Vector3(0.125f, 0.19f, 0.125f), m.Skin);

        CreatePrimitive(elbowPivot, name + "_Hand", PrimitiveType.Sphere,
            new Vector3(0f, -0.405f, 0f), new Vector3(0.18f, 0.19f, 0.16f), m.Skin);
    }

    private static void BuildHead(Transform pelvis, Materials m)
    {
        Transform neckPivot = CreatePivot(pelvis, "NeckPivot", new Vector3(0f, 0.88f, 0f));

        CreatePrimitive(neckPivot, "Neck", PrimitiveType.Cylinder,
            new Vector3(0f, 0.05f, 0f), new Vector3(0.13f, 0.10f, 0.13f), m.Skin);

        Transform headPivot = CreatePivot(neckPivot, "HeadPivot", new Vector3(0f, 0.25f, 0f));

        CreatePrimitive(headPivot, "Head", PrimitiveType.Sphere,
            Vector3.zero, new Vector3(0.43f, 0.47f, 0.40f), m.Skin);

        CreatePrimitive(headPivot, "Nose", PrimitiveType.Sphere,
            new Vector3(0f, -0.015f, -0.205f), new Vector3(0.10f, 0.11f, 0.13f), m.Skin);

        CreatePrimitive(headPivot, "Beard", PrimitiveType.Sphere,
            new Vector3(0f, -0.16f, -0.15f), new Vector3(0.34f, 0.27f, 0.20f), m.Beard);

        CreatePrimitive(headPivot, "Hat_Brim", PrimitiveType.Cylinder,
            new Vector3(0f, 0.265f, 0f), new Vector3(0.39f, 0.025f, 0.39f), m.Hat);

        CreatePrimitive(headPivot, "Hat_Crown", PrimitiveType.Cylinder,
            new Vector3(0f, 0.39f, 0f), new Vector3(0.255f, 0.12f, 0.255f), m.Hat);

        CreatePrimitive(headPivot, "Hat_Band", PrimitiveType.Cylinder,
            new Vector3(0f, 0.285f, 0f), new Vector3(0.27f, 0.035f, 0.27f), m.Leather);
    }

    private static void BuildSimpleGear(Transform root, Materials m)
    {
        CreatePrimitive(root, "Backpack", PrimitiveType.Cube,
            new Vector3(0f, 1.28f, 0.265f), new Vector3(0.46f, 0.48f, 0.18f), m.Leather);

        CreatePrimitive(root, "Backpack_Roll", PrimitiveType.Cylinder,
            new Vector3(0f, 1.57f, 0.30f), new Vector3(0.18f, 0.27f, 0.18f), m.Shirt,
            new Vector3(0f, 0f, 90f));
    }

    private static Transform CreatePivot(Transform parent, string name, Vector3 localPosition)
    {
        GameObject pivot = new(name);
        pivot.transform.SetParent(parent, false);
        pivot.transform.localPosition = localPosition;
        pivot.transform.localRotation = Quaternion.identity;
        pivot.transform.localScale = Vector3.one;
        return pivot.transform;
    }

    private static GameObject CreatePrimitive(
        Transform parent,
        string name,
        PrimitiveType primitiveType,
        Vector3 localPosition,
        Vector3 localScale,
        Material material,
        Vector3? localEulerAngles = null)
    {
        GameObject part = GameObject.CreatePrimitive(primitiveType);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localRotation = Quaternion.Euler(localEulerAngles ?? Vector3.zero);
        part.transform.localScale = localScale;

        Renderer renderer = part.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
        }

        Collider collider = part.GetComponent<Collider>();
        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }

        return part;
    }

    private static Materials LoadMaterials()
    {
        EnsureFolderExists(MaterialFolder);

        return new Materials
        {
            Skin = LoadOrCreateMaterial("Proto_Skin", SkinColor),
            Shirt = LoadOrCreateMaterial("Proto_Shirt", ShirtColor),
            Pants = LoadOrCreateMaterial("Proto_Pants", PantsColor),
            Boot = LoadOrCreateMaterial("Proto_Boots", BootColor),
            Leather = LoadOrCreateMaterial("Proto_Leather", LeatherColor),
            Hat = LoadOrCreateMaterial("Proto_Hat", HatColor),
            Beard = LoadOrCreateMaterial("Proto_Beard", BeardColor),
            Metal = LoadOrCreateMaterial("Proto_Metal", MetalColor)
        };
    }

    private static Material LoadOrCreateMaterial(string materialName, Color color)
    {
        string path = $"{MaterialFolder}/{materialName}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (material != null)
        {
            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        material = new Material(shader)
        {
            name = materialName,
            color = color
        };

        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void EnsureFolderExists(string fullFolderPath)
    {
        string[] parts = fullFolderPath.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }

        AssetDatabase.SaveAssets();
    }

    private sealed class Materials
    {
        public Material Skin;
        public Material Shirt;
        public Material Pants;
        public Material Boot;
        public Material Leather;
        public Material Hat;
        public Material Beard;
        public Material Metal;
    }
}
