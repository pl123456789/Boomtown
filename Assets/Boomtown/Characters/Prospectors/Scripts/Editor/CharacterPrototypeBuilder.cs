using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public sealed class CharacterPrototypeBuilderWindow : EditorWindow
{
    private GameObject characterRoot;
    private int appearanceSeed = 1;

    [MenuItem("Boomtown/Characters/Build Character Prototype", priority = 100)]
    private static void OpenWindow()
    {
        CharacterPrototypeBuilderWindow window = GetWindow<CharacterPrototypeBuilderWindow>();
        window.titleContent = new GUIContent("Character Builder");
        window.minSize = new Vector2(340f, 220f);
        window.characterRoot = CharacterBuildPrototyper.ResolveCharacterRoot(Selection.activeGameObject);
        window.Show();
    }

    private void OnSelectionChange()
    {
        GameObject resolved = CharacterBuildPrototyper.ResolveCharacterRoot(Selection.activeGameObject);
        if (resolved != null)
        {
            characterRoot = resolved;
            Repaint();
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Boomtown Character Prototype", EditorStyles.boldLabel);
        EditorGUILayout.Space(4f);

        characterRoot = (GameObject)EditorGUILayout.ObjectField(
            "Character Root", characterRoot, typeof(GameObject), true);

        appearanceSeed = EditorGUILayout.IntField("Appearance Seed", appearanceSeed);

        EditorGUILayout.HelpBox(
            "Select Bill, Ted, or one of their Visual/Prototype children. " +
            "The builder preserves gameplay components, hides the root capsule, " +
            "faces the model correctly, and places its boots on the character root.",
            MessageType.Info);

        using (new EditorGUI.DisabledScope(characterRoot == null))
        {
            if (GUILayout.Button("Build / Rebuild Character", GUILayout.Height(32f)))
            {
                CharacterBuildPrototyper.BuildCharacter(characterRoot, appearanceSeed);
            }

            if (GUILayout.Button("Randomize Appearance", GUILayout.Height(28f)))
            {
                appearanceSeed = UnityEngine.Random.Range(1, int.MaxValue);
                CharacterBuildPrototyper.BuildCharacter(characterRoot, appearanceSeed);
            }
        }

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField(
            "Tip: use a fixed seed for a character so their appearance stays consistent.",
            EditorStyles.wordWrappedMiniLabel);
    }
}

public static class CharacterBuildPrototyper
{
    private const string VisualName = "Visual";
    private const string GeneratedRootName = "Prototype_Character";
    private const string MaterialFolder = "Assets/Boomtown/Characters/Materials/Prototype";

    private static readonly Color[] SkinColors =
    {
        new(0.78f, 0.58f, 0.42f),
        new(0.66f, 0.43f, 0.28f),
        new(0.52f, 0.32f, 0.20f),
        new(0.39f, 0.23f, 0.15f)
    };

    private static readonly Color[] ShirtColors =
    {
        new(0.73f, 0.68f, 0.53f),
        new(0.20f, 0.30f, 0.34f),
        new(0.37f, 0.18f, 0.14f),
        new(0.22f, 0.34f, 0.24f),
        new(0.35f, 0.35f, 0.34f)
    };

    private static readonly Color[] PantsColors =
    {
        new(0.16f, 0.14f, 0.13f),
        new(0.20f, 0.22f, 0.23f),
        new(0.16f, 0.20f, 0.27f),
        new(0.25f, 0.19f, 0.14f)
    };

    private static readonly Color[] LeatherColors =
    {
        new(0.25f, 0.12f, 0.055f),
        new(0.16f, 0.09f, 0.045f),
        new(0.31f, 0.19f, 0.09f),
        new(0.12f, 0.12f, 0.10f)
    };

    private static readonly Color[] HatColors =
    {
        new(0.18f, 0.105f, 0.045f),
        new(0.10f, 0.09f, 0.075f),
        new(0.28f, 0.27f, 0.24f),
        new(0.23f, 0.16f, 0.09f)
    };

    private static readonly Color[] BeardColors =
    {
        new(0.09f, 0.055f, 0.035f),
        new(0.20f, 0.11f, 0.055f),
        new(0.31f, 0.24f, 0.17f),
        new(0.18f, 0.18f, 0.17f)
    };

    private static readonly Color BootColor = new(0.10f, 0.075f, 0.055f);
    private static readonly Color MetalColor = new(0.30f, 0.32f, 0.33f);

    public static GameObject ResolveCharacterRoot(GameObject selected)
    {
        if (selected == null)
        {
            return null;
        }

        Transform current = selected.transform;
        while (current != null)
        {
            if (current.GetComponent<CharacterController>() != null ||
                current.GetComponent<UnityEngine.AI.NavMeshAgent>() != null)
            {
                return current.gameObject;
            }

            current = current.parent;
        }

        return selected;
    }

    public static void BuildCharacter(GameObject selectedObject, int appearanceSeed)
    {
        GameObject characterRoot = ResolveCharacterRoot(selectedObject);
        if (characterRoot == null)
        {
            EditorUtility.DisplayDialog(
                "Boomtown Character Builder",
                "Select Bill, Ted, or another character root first.",
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

        // The prototype face was authored toward local -Z. Rotate the entire visual so
        // its face points along the character controller's normal +Z forward direction.
        generatedRoot.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        generatedRoot.transform.localScale = Vector3.one;

        System.Random random = new(appearanceSeed);
        Appearance appearance = CreateAppearance(random);
        Materials materials = LoadMaterials(characterRoot.name, appearance);

        BuildBody(generatedRoot.transform, materials, appearance);
        BuildSimpleGear(generatedRoot.transform, materials, appearance);
        AlignFeetToCharacterRoot(generatedRoot.transform, characterRoot.transform);

        Selection.activeGameObject = characterRoot;
        EditorUtility.SetDirty(characterRoot);
        AssetDatabase.SaveAssets();

        Debug.Log(
            $"[Boomtown] Built {characterRoot.name} prototype with seed {appearanceSeed}. " +
            "Facing corrected, boots aligned to root, and gameplay components preserved.");
    }

    private static Appearance CreateAppearance(System.Random random)
    {
        return new Appearance
        {
            Skin = Pick(random, SkinColors),
            Shirt = Pick(random, ShirtColors),
            Pants = Pick(random, PantsColors),
            Leather = Pick(random, LeatherColors),
            Hat = Pick(random, HatColors),
            Beard = Pick(random, BeardColors),
            HasBeard = random.NextDouble() > 0.22,
            HasBackpack = random.NextDouble() > 0.35,
            HasVest = random.NextDouble() > 0.20
        };
    }

    private static Color Pick(System.Random random, Color[] colors)
    {
        return colors[random.Next(colors.Length)];
    }

    private static void AlignFeetToCharacterRoot(Transform generatedRoot, Transform characterRoot)
    {
        Renderer[] renderers = generatedRoot.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            return;
        }

        float lowestWorldY = float.PositiveInfinity;
        foreach (Renderer renderer in renderers)
        {
            lowestWorldY = Mathf.Min(lowestWorldY, renderer.bounds.min.y);
        }

        float correction = characterRoot.position.y - lowestWorldY;
        generatedRoot.position += Vector3.up * correction;
    }

    private static void DisableGameplayCapsuleRenderer(GameObject characterRoot)
    {
        MeshFilter rootMeshFilter = characterRoot.GetComponent<MeshFilter>();
        MeshRenderer rootMeshRenderer = characterRoot.GetComponent<MeshRenderer>();

        if (rootMeshFilter != null && rootMeshRenderer != null &&
            rootMeshFilter.sharedMesh != null &&
            rootMeshFilter.sharedMesh.name.ToLowerInvariant().Contains("capsule"))
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

    private static void BuildBody(Transform root, Materials m, Appearance appearance)
    {
        Transform pelvis = CreatePivot(root, "Pelvis", new Vector3(0f, 0.91f, 0f));

        CreatePrimitive(pelvis, "Hips", PrimitiveType.Cube,
            new Vector3(0f, 0.03f, 0f), new Vector3(0.56f, 0.26f, 0.34f), m.Pants);

        CreatePrimitive(pelvis, "Waist", PrimitiveType.Cube,
            new Vector3(0f, 0.28f, 0f), new Vector3(0.52f, 0.34f, 0.33f), m.Shirt);

        CreatePrimitive(pelvis, "Chest", PrimitiveType.Cube,
            new Vector3(0f, 0.58f, 0f), new Vector3(0.68f, 0.42f, 0.38f), m.Shirt);

        if (appearance.HasVest)
        {
            CreatePrimitive(pelvis, "Vest_Left", PrimitiveType.Cube,
                new Vector3(-0.18f, 0.56f, -0.205f), new Vector3(0.25f, 0.38f, 0.035f), m.Leather);

            CreatePrimitive(pelvis, "Vest_Right", PrimitiveType.Cube,
                new Vector3(0.18f, 0.56f, -0.205f), new Vector3(0.25f, 0.38f, 0.035f), m.Leather);
        }

        CreatePrimitive(pelvis, "Belt", PrimitiveType.Cube,
            new Vector3(0f, 0.10f, 0f), new Vector3(0.59f, 0.075f, 0.36f), m.Leather);

        CreatePrimitive(pelvis, "Buckle", PrimitiveType.Cube,
            new Vector3(0f, 0.10f, -0.195f), new Vector3(0.10f, 0.085f, 0.035f), m.Metal);

        BuildLeg(pelvis, "LeftLeg", -0.17f, m);
        BuildLeg(pelvis, "RightLeg", 0.17f, m);
        BuildArm(pelvis, "LeftArm", -0.43f, m);
        BuildArm(pelvis, "RightArm", 0.43f, m);
        BuildHead(pelvis, m, appearance);
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

    private static void BuildHead(Transform pelvis, Materials m, Appearance appearance)
    {
        Transform neckPivot = CreatePivot(pelvis, "NeckPivot", new Vector3(0f, 0.88f, 0f));
        CreatePrimitive(neckPivot, "Neck", PrimitiveType.Cylinder,
            new Vector3(0f, 0.05f, 0f), new Vector3(0.13f, 0.10f, 0.13f), m.Skin);

        Transform headPivot = CreatePivot(neckPivot, "HeadPivot", new Vector3(0f, 0.25f, 0f));
        CreatePrimitive(headPivot, "Head", PrimitiveType.Sphere,
            Vector3.zero, new Vector3(0.43f, 0.47f, 0.40f), m.Skin);

        CreatePrimitive(headPivot, "Nose", PrimitiveType.Sphere,
            new Vector3(0f, -0.015f, -0.205f), new Vector3(0.10f, 0.11f, 0.13f), m.Skin);

        if (appearance.HasBeard)
        {
            CreatePrimitive(headPivot, "Beard", PrimitiveType.Sphere,
                new Vector3(0f, -0.16f, -0.15f), new Vector3(0.34f, 0.27f, 0.20f), m.Beard);
        }

        CreatePrimitive(headPivot, "Hat_Brim", PrimitiveType.Cylinder,
            new Vector3(0f, 0.265f, 0f), new Vector3(0.39f, 0.025f, 0.39f), m.Hat);

        CreatePrimitive(headPivot, "Hat_Crown", PrimitiveType.Cylinder,
            new Vector3(0f, 0.39f, 0f), new Vector3(0.255f, 0.12f, 0.255f), m.Hat);

        CreatePrimitive(headPivot, "Hat_Band", PrimitiveType.Cylinder,
            new Vector3(0f, 0.285f, 0f), new Vector3(0.27f, 0.035f, 0.27f), m.Leather);
    }

    private static void BuildSimpleGear(Transform root, Materials m, Appearance appearance)
    {
        if (!appearance.HasBackpack)
        {
            return;
        }

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
            UnityEngine.Object.DestroyImmediate(collider);
        }

        return part;
    }

    private static Materials LoadMaterials(string characterName, Appearance appearance)
    {
        EnsureFolderExists(MaterialFolder);
        string safeName = MakeSafeFileName(characterName);

        return new Materials
        {
            Skin = LoadOrCreateMaterial($"{safeName}_Skin", appearance.Skin),
            Shirt = LoadOrCreateMaterial($"{safeName}_Shirt", appearance.Shirt),
            Pants = LoadOrCreateMaterial($"{safeName}_Pants", appearance.Pants),
            Boot = LoadOrCreateMaterial($"{safeName}_Boots", BootColor),
            Leather = LoadOrCreateMaterial($"{safeName}_Leather", appearance.Leather),
            Hat = LoadOrCreateMaterial($"{safeName}_Hat", appearance.Hat),
            Beard = LoadOrCreateMaterial($"{safeName}_Beard", appearance.Beard),
            Metal = LoadOrCreateMaterial($"{safeName}_Metal", MetalColor)
        };
    }

    private static Material LoadOrCreateMaterial(string materialName, Color color)
    {
        string path = $"{MaterialFolder}/{materialName}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material = new Material(shader) { name = materialName };
            AssetDatabase.CreateAsset(material, path);
        }

        material.color = color;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static string MakeSafeFileName(string value)
    {
        foreach (char invalidCharacter in Path.GetInvalidFileNameChars())
        {
            value = value.Replace(invalidCharacter, '_');
        }

        return value.Replace(' ', '_');
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
    }

    private sealed class Appearance
    {
        public Color Skin;
        public Color Shirt;
        public Color Pants;
        public Color Leather;
        public Color Hat;
        public Color Beard;
        public bool HasBeard;
        public bool HasBackpack;
        public bool HasVest;
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
