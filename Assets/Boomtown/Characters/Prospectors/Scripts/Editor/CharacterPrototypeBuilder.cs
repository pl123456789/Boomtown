using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

public sealed class CharacterPrototypeBuilderWindow : EditorWindow
{
    private GameObject characterRoot;
    private int appearanceSeed = 1;
    private float targetHeight = 1.80f;
    private float groundOffset = -0.03f;

    [MenuItem("Boomtown/Characters/Build Character Prototype", priority = 100)]
    private static void OpenWindow()
    {
        CharacterPrototypeBuilderWindow window = GetWindow<CharacterPrototypeBuilderWindow>();
        window.titleContent = new GUIContent("Character Builder");
        window.minSize = new Vector2(360f, 300f);
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
        targetHeight = EditorGUILayout.Slider("Character Height", targetHeight, 1.55f, 2.05f);
        groundOffset = EditorGUILayout.Slider("Ground Offset", groundOffset, -0.15f, 0.05f);

        EditorGUILayout.HelpBox(
            "Builds a cleaner low-poly frontier character, faces it forward, scales it to real-world height, " +
            "places the boots on the ground, and fits the gameplay capsule to the character.",
            MessageType.Info);

        using (new EditorGUI.DisabledScope(characterRoot == null))
        {
            if (GUILayout.Button("Build / Rebuild Character", GUILayout.Height(34f)))
            {
                CharacterBuildPrototyper.BuildCharacter(
                    characterRoot, appearanceSeed, targetHeight, groundOffset);
            }

            if (GUILayout.Button("Randomize Appearance", GUILayout.Height(30f)))
            {
                appearanceSeed = UnityEngine.Random.Range(1, int.MaxValue);
                CharacterBuildPrototyper.BuildCharacter(
                    characterRoot, appearanceSeed, targetHeight, groundOffset);
            }
        }

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField(
            "Recommended scale: 1.75–1.85 m. Keep a fixed seed for Bill and Ted.",
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
        new(0.20f, 0.30f, 0.40f),
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
    private static readonly Color EyeColor = new(0.055f, 0.045f, 0.035f);

    public static GameObject ResolveCharacterRoot(GameObject selected)
    {
        if (selected == null) return null;

        Transform current = selected.transform;
        while (current != null)
        {
            if (current.GetComponent<CharacterController>() != null ||
                current.GetComponent<NavMeshAgent>() != null ||
                current.GetComponent<CapsuleCollider>() != null)
            {
                return current.gameObject;
            }

            current = current.parent;
        }

        return selected;
    }

    public static void BuildCharacter(
        GameObject selectedObject,
        int appearanceSeed,
        float targetHeight = 1.80f,
        float groundOffset = -0.03f)
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

        GameObject generatedObject = new(GeneratedRootName);
        Undo.RegisterCreatedObjectUndo(generatedObject, "Create Character Prototype");
        Transform generatedRoot = generatedObject.transform;
        generatedRoot.SetParent(visual, false);
        generatedRoot.localPosition = Vector3.zero;
        generatedRoot.localRotation = Quaternion.Euler(0f, 180f, 0f);
        generatedRoot.localScale = Vector3.one;

        System.Random random = new(appearanceSeed);
        Appearance appearance = CreateAppearance(random);
        Materials materials = LoadMaterials(characterRoot.name, appearance);

        BuildBody(generatedRoot, materials, appearance);
        BuildGear(generatedRoot, materials, appearance);

        ScaleToHeight(generatedRoot, targetHeight);
        AlignFeetToRoot(generatedRoot, characterRoot.transform, groundOffset);
        FitGameplayCapsules(characterRoot, targetHeight);

        Selection.activeGameObject = characterRoot;
        EditorUtility.SetDirty(characterRoot);
        AssetDatabase.SaveAssets();

        Debug.Log(
            $"[Boomtown] Built {characterRoot.name}: seed {appearanceSeed}, " +
            $"height {targetHeight:0.00} m, ground offset {groundOffset:0.00} m.");
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
            HasBeard = random.NextDouble() > 0.18,
            HasMoustache = random.NextDouble() > 0.30,
            HasBackpack = random.NextDouble() > 0.45,
            HasVest = random.NextDouble() > 0.15,
            HasNeckerchief = random.NextDouble() > 0.45,
            BroadBuild = random.NextDouble() > 0.58
        };
    }

    private static Color Pick(System.Random random, Color[] colors) =>
        colors[random.Next(colors.Length)];

    private static void BuildBody(Transform root, Materials m, Appearance a)
    {
        float bodyWidth = a.BroadBuild ? 1.06f : 1f;
        Transform pelvis = CreatePivot(root, "Pelvis", new Vector3(0f, 0.88f, 0f));

        CreatePart(pelvis, "Hips", PrimitiveType.Capsule,
            new Vector3(0f, 0.02f, 0f),
            new Vector3(0.50f * bodyWidth, 0.28f, 0.34f), m.Pants);

        CreatePart(pelvis, "Torso", PrimitiveType.Capsule,
            new Vector3(0f, 0.48f, 0f),
            new Vector3(0.60f * bodyWidth, 0.46f, 0.40f), m.Shirt);

        CreatePart(pelvis, "Shoulders", PrimitiveType.Capsule,
            new Vector3(0f, 0.66f, 0f),
            new Vector3(0.69f * bodyWidth, 0.22f, 0.38f), m.Shirt,
            new Vector3(0f, 0f, 90f));

        if (a.HasVest)
        {
            CreatePart(pelvis, "Vest", PrimitiveType.Cube,
                new Vector3(0f, 0.48f, -0.205f),
                new Vector3(0.47f * bodyWidth, 0.52f, 0.055f), m.Leather);

            CreatePart(pelvis, "VestOpening", PrimitiveType.Cube,
                new Vector3(0f, 0.55f, -0.238f),
                new Vector3(0.055f, 0.42f, 0.018f), m.Shirt);
        }

        CreatePart(pelvis, "Belt", PrimitiveType.Cylinder,
            new Vector3(0f, 0.10f, 0f),
            new Vector3(0.31f * bodyWidth, 0.045f, 0.31f * bodyWidth), m.Leather);

        CreatePart(pelvis, "Buckle", PrimitiveType.Cube,
            new Vector3(0f, 0.10f, -0.205f),
            new Vector3(0.10f, 0.08f, 0.035f), m.Metal);

        BuildLeg(pelvis, "LeftLeg", -0.17f * bodyWidth, m);
        BuildLeg(pelvis, "RightLeg", 0.17f * bodyWidth, m);
        BuildArm(pelvis, "LeftArm", -0.40f * bodyWidth, m);
        BuildArm(pelvis, "RightArm", 0.40f * bodyWidth, m);
        BuildHead(pelvis, m, a);

        if (a.HasNeckerchief)
        {
            CreatePart(pelvis, "Neckerchief", PrimitiveType.Cube,
                new Vector3(0f, 0.78f, -0.225f),
                new Vector3(0.24f, 0.10f, 0.045f), m.Scarf);
        }
    }

    private static void BuildLeg(Transform pelvis, string name, float x, Materials m)
    {
        Transform upper = CreatePivot(pelvis, name + "_Hip", new Vector3(x, -0.13f, 0f));

        CreatePart(upper, name + "_Upper", PrimitiveType.Capsule,
            new Vector3(0f, -0.23f, 0f),
            new Vector3(0.17f, 0.28f, 0.17f), m.Pants);

        Transform knee = CreatePivot(upper, name + "_Knee", new Vector3(0f, -0.47f, 0f));

        CreatePart(knee, name + "_Lower", PrimitiveType.Capsule,
            new Vector3(0f, -0.19f, 0f),
            new Vector3(0.145f, 0.22f, 0.145f), m.Pants);

        CreatePart(knee, name + "_Boot", PrimitiveType.Cube,
            new Vector3(0f, -0.405f, -0.055f),
            new Vector3(0.25f, 0.18f, 0.36f), m.Boot);

        CreatePart(knee, name + "_Toe", PrimitiveType.Sphere,
            new Vector3(0f, -0.42f, -0.19f),
            new Vector3(0.23f, 0.14f, 0.25f), m.Boot);
    }

    private static void BuildArm(Transform pelvis, string name, float x, Materials m)
    {
        Transform shoulder = CreatePivot(pelvis, name + "_Shoulder", new Vector3(x, 0.66f, 0f));
        shoulder.localRotation = Quaternion.Euler(0f, 0f, x < 0f ? -5f : 5f);

        CreatePart(shoulder, name + "_Upper", PrimitiveType.Capsule,
            new Vector3(0f, -0.19f, 0f),
            new Vector3(0.14f, 0.23f, 0.14f), m.Shirt);

        Transform elbow = CreatePivot(shoulder, name + "_Elbow", new Vector3(0f, -0.39f, 0f));

        CreatePart(elbow, name + "_Forearm", PrimitiveType.Capsule,
            new Vector3(0f, -0.17f, 0f),
            new Vector3(0.115f, 0.20f, 0.115f), m.Skin);

        CreatePart(elbow, name + "_Hand", PrimitiveType.Sphere,
            new Vector3(0f, -0.37f, 0f),
            new Vector3(0.16f, 0.18f, 0.14f), m.Skin);
    }

    private static void BuildHead(Transform pelvis, Materials m, Appearance a)
    {
        Transform neck = CreatePivot(pelvis, "Neck", new Vector3(0f, 0.87f, 0f));

        CreatePart(neck, "NeckMesh", PrimitiveType.Cylinder,
            new Vector3(0f, 0.04f, 0f),
            new Vector3(0.12f, 0.10f, 0.12f), m.Skin);

        Transform head = CreatePivot(neck, "Head", new Vector3(0f, 0.24f, 0f));

        CreatePart(head, "HeadMesh", PrimitiveType.Sphere,
            Vector3.zero, new Vector3(0.38f, 0.43f, 0.36f), m.Skin);

        CreatePart(head, "Nose", PrimitiveType.Sphere,
            new Vector3(0f, -0.01f, -0.19f),
            new Vector3(0.085f, 0.10f, 0.12f), m.Skin);

        CreatePart(head, "Eye_L", PrimitiveType.Sphere,
            new Vector3(-0.085f, 0.06f, -0.175f),
            new Vector3(0.035f, 0.035f, 0.025f), m.Eye);

        CreatePart(head, "Eye_R", PrimitiveType.Sphere,
            new Vector3(0.085f, 0.06f, -0.175f),
            new Vector3(0.035f, 0.035f, 0.025f), m.Eye);

        if (a.HasMoustache)
        {
            CreatePart(head, "Moustache", PrimitiveType.Capsule,
                new Vector3(0f, -0.08f, -0.18f),
                new Vector3(0.16f, 0.045f, 0.045f), m.Beard,
                new Vector3(0f, 0f, 90f));
        }

        if (a.HasBeard)
        {
            CreatePart(head, "Beard", PrimitiveType.Sphere,
                new Vector3(0f, -0.16f, -0.13f),
                new Vector3(0.30f, 0.24f, 0.18f), m.Beard);
        }

        CreatePart(head, "Hat_Brim", PrimitiveType.Cylinder,
            new Vector3(0f, 0.245f, 0f),
            new Vector3(0.36f, 0.025f, 0.36f), m.Hat);

        CreatePart(head, "Hat_Crown", PrimitiveType.Cylinder,
            new Vector3(0f, 0.355f, 0f),
            new Vector3(0.22f, 0.11f, 0.22f), m.Hat);

        CreatePart(head, "Hat_Band", PrimitiveType.Cylinder,
            new Vector3(0f, 0.268f, 0f),
            new Vector3(0.235f, 0.028f, 0.235f), m.Leather);
    }

    private static void BuildGear(Transform root, Materials m, Appearance a)
    {
        if (!a.HasBackpack) return;

        CreatePart(root, "Backpack", PrimitiveType.Cube,
            new Vector3(0f, 1.25f, 0.26f),
            new Vector3(0.40f, 0.42f, 0.17f), m.Leather);

        CreatePart(root, "Bedroll", PrimitiveType.Cylinder,
            new Vector3(0f, 1.53f, 0.28f),
            new Vector3(0.15f, 0.24f, 0.15f), m.Shirt,
            new Vector3(0f, 0f, 90f));
    }

    private static void ScaleToHeight(Transform generatedRoot, float targetHeight)
    {
        Renderer[] renderers = generatedRoot.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

        if (bounds.size.y <= 0.001f) return;

        float scale = targetHeight / bounds.size.y;
        generatedRoot.localScale = Vector3.one * scale;
    }

    private static void AlignFeetToRoot(Transform generatedRoot, Transform characterRoot, float groundOffset)
    {
        Renderer[] renderers = generatedRoot.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        float lowestWorldY = float.PositiveInfinity;
        foreach (Renderer renderer in renderers)
            lowestWorldY = Mathf.Min(lowestWorldY, renderer.bounds.min.y);

        float targetWorldY = characterRoot.position.y + groundOffset;
        generatedRoot.position += Vector3.up * (targetWorldY - lowestWorldY);
    }

    private static void FitGameplayCapsules(GameObject root, float height)
    {
        float radius = Mathf.Clamp(height * 0.18f, 0.26f, 0.38f);
        float centerY = height * 0.5f;

        CapsuleCollider capsule = root.GetComponent<CapsuleCollider>();
        if (capsule != null)
        {
            Undo.RecordObject(capsule, "Fit Capsule Collider");
            capsule.direction = 1;
            capsule.height = height;
            capsule.radius = radius;
            capsule.center = new Vector3(0f, centerY, 0f);
            EditorUtility.SetDirty(capsule);
        }

        CharacterController controller = root.GetComponent<CharacterController>();
        if (controller != null)
        {
            Undo.RecordObject(controller, "Fit Character Controller");
            controller.height = height;
            controller.radius = radius;
            controller.center = new Vector3(0f, centerY, 0f);
            controller.stepOffset = Mathf.Min(controller.stepOffset, height * 0.25f);
            EditorUtility.SetDirty(controller);
        }

        NavMeshAgent agent = root.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            Undo.RecordObject(agent, "Fit NavMesh Agent");
            agent.height = height;
            agent.radius = radius;
            agent.baseOffset = 0f;
            EditorUtility.SetDirty(agent);
        }
    }

    private static void DisableGameplayCapsuleRenderer(GameObject root)
    {
        MeshFilter filter = root.GetComponent<MeshFilter>();
        MeshRenderer renderer = root.GetComponent<MeshRenderer>();

        if (filter != null && renderer != null && filter.sharedMesh != null &&
            filter.sharedMesh.name.ToLowerInvariant().Contains("capsule"))
        {
            Undo.RecordObject(renderer, "Hide Gameplay Capsule");
            renderer.enabled = false;
            EditorUtility.SetDirty(renderer);
        }
    }

    private static Transform FindOrCreateChild(Transform parent, string childName)
    {
        Transform existing = parent.Find(childName);
        if (existing != null) return existing;

        GameObject child = new(childName);
        Undo.RegisterCreatedObjectUndo(child, $"Create {childName}");
        child.transform.SetParent(parent, false);
        return child.transform;
    }

    private static void ClearGeneratedVisual(Transform visual)
    {
        Transform generated = visual.Find(GeneratedRootName);
        if (generated != null) Undo.DestroyObjectImmediate(generated.gameObject);

        string[] legacyNames =
        {
            "Torso_Shirt", "Vest", "Head", "Beard", "Neckerchief",
            "Arm_L", "Arm_R", "Leg_L", "Leg_R", "Boot_L", "Boot_R",
            "Belt", "Belt_Pouch", "Hat_Brim", "Hat_Crown", "Backpack", "GoldPan"
        };

        foreach (string legacyName in legacyNames)
        {
            Transform legacy = visual.Find(legacyName);
            if (legacy != null) Undo.DestroyObjectImmediate(legacy.gameObject);
        }
    }

    private static Transform CreatePivot(Transform parent, string name, Vector3 position)
    {
        GameObject pivot = new(name);
        pivot.transform.SetParent(parent, false);
        pivot.transform.localPosition = position;
        pivot.transform.localRotation = Quaternion.identity;
        pivot.transform.localScale = Vector3.one;
        return pivot.transform;
    }

    private static GameObject CreatePart(
        Transform parent,
        string name,
        PrimitiveType type,
        Vector3 position,
        Vector3 scale,
        Material material,
        Vector3? rotation = null)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localRotation = Quaternion.Euler(rotation ?? Vector3.zero);
        part.transform.localScale = scale;

        Renderer renderer = part.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = material;

        Collider collider = part.GetComponent<Collider>();
        if (collider != null) UnityEngine.Object.DestroyImmediate(collider);

        return part;
    }

    private static Materials LoadMaterials(string characterName, Appearance appearance)
    {
        EnsureFolderExists(MaterialFolder);
        string safeName = Sanitize(characterName);

        return new Materials
        {
            Skin = LoadOrCreateMaterial($"Proto_{safeName}_Skin", appearance.Skin),
            Shirt = LoadOrCreateMaterial($"Proto_{safeName}_Shirt", appearance.Shirt),
            Pants = LoadOrCreateMaterial($"Proto_{safeName}_Pants", appearance.Pants),
            Boot = LoadOrCreateMaterial($"Proto_{safeName}_Boot", BootColor),
            Leather = LoadOrCreateMaterial($"Proto_{safeName}_Leather", appearance.Leather),
            Hat = LoadOrCreateMaterial($"Proto_{safeName}_Hat", appearance.Hat),
            Beard = LoadOrCreateMaterial($"Proto_{safeName}_Beard", appearance.Beard),
            Metal = LoadOrCreateMaterial($"Proto_{safeName}_Metal", MetalColor),
            Eye = LoadOrCreateMaterial($"Proto_{safeName}_Eye", EyeColor),
            Scarf = LoadOrCreateMaterial($"Proto_{safeName}_Scarf", new Color(0.48f, 0.08f, 0.06f))
        };
    }

    private static Material LoadOrCreateMaterial(string name, Color color)
    {
        string path = $"{MaterialFolder}/{name}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }

        material.color = color;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void EnsureFolderExists(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private static string Sanitize(string value)
    {
        foreach (char invalid in System.IO.Path.GetInvalidFileNameChars())
            value = value.Replace(invalid, '_');
        return value.Replace(' ', '_');
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
        public bool HasMoustache;
        public bool HasBackpack;
        public bool HasVest;
        public bool HasNeckerchief;
        public bool BroadBuild;
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
        public Material Eye;
        public Material Scarf;
    }
}
