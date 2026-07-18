using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

public sealed class CharacterPrototypeBuilderWindow : EditorWindow
{
    private GameObject characterRoot;
    private int appearanceSeed = 1;
    private float targetHeight = 1.80f;

    [MenuItem("Boomtown/Characters/Build Character Prototype", priority = 100)]
    private static void OpenWindow()
    {
        var window = GetWindow<CharacterPrototypeBuilderWindow>();
        window.titleContent = new GUIContent("Character Builder");
        window.minSize = new Vector2(360f, 270f);
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
        targetHeight = EditorGUILayout.Slider("Character Height", targetHeight, 1.60f, 1.95f);

        EditorGUILayout.HelpBox(
            "Builds a compact, stylized frontier character with grounded boots, forward-facing visuals, " +
            "historical color variation, and a gameplay capsule fitted to the selected height.",
            MessageType.Info);

        using (new EditorGUI.DisabledScope(characterRoot == null))
        {
            if (GUILayout.Button("Build / Rebuild Character", GUILayout.Height(34f)))
                CharacterBuildPrototyper.BuildCharacter(characterRoot, appearanceSeed, targetHeight);

            if (GUILayout.Button("Randomize Appearance", GUILayout.Height(30f)))
            {
                appearanceSeed = UnityEngine.Random.Range(1, int.MaxValue);
                CharacterBuildPrototyper.BuildCharacter(characterRoot, appearanceSeed, targetHeight);
            }
        }

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField(
            "Recommended height: Bill 1.80 m, Ted 1.76–1.80 m.",
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
        new(0.78f, 0.58f, 0.42f), new(0.67f, 0.45f, 0.30f),
        new(0.54f, 0.34f, 0.22f), new(0.40f, 0.25f, 0.17f)
    };

    private static readonly Color[] ShirtColors =
    {
        new(0.74f, 0.69f, 0.55f), new(0.20f, 0.31f, 0.43f),
        new(0.32f, 0.42f, 0.29f), new(0.42f, 0.24f, 0.18f),
        new(0.39f, 0.39f, 0.36f)
    };

    private static readonly Color[] PantsColors =
    {
        new(0.15f, 0.14f, 0.13f), new(0.18f, 0.22f, 0.28f),
        new(0.24f, 0.20f, 0.16f), new(0.25f, 0.26f, 0.25f)
    };

    private static readonly Color[] LeatherColors =
    {
        new(0.24f, 0.12f, 0.055f), new(0.15f, 0.085f, 0.04f),
        new(0.32f, 0.19f, 0.09f), new(0.11f, 0.105f, 0.09f)
    };

    private static readonly Color[] HatColors =
    {
        new(0.18f, 0.10f, 0.045f), new(0.09f, 0.085f, 0.07f),
        new(0.27f, 0.25f, 0.21f), new(0.24f, 0.16f, 0.08f)
    };

    private static readonly Color[] BeardColors =
    {
        new(0.08f, 0.05f, 0.03f), new(0.19f, 0.10f, 0.05f),
        new(0.31f, 0.23f, 0.16f), new(0.17f, 0.17f, 0.16f)
    };

    public static GameObject ResolveCharacterRoot(GameObject selected)
    {
        if (selected == null) return null;

        Transform current = selected.transform;
        while (current != null)
        {
            if (current.GetComponent<CharacterController>() != null ||
                current.GetComponent<NavMeshAgent>() != null ||
                current.GetComponent<CapsuleCollider>() != null)
                return current.gameObject;

            current = current.parent;
        }

        return selected;
    }

    public static void BuildCharacter(GameObject selectedObject, int seed, float targetHeight = 1.80f)
    {
        GameObject root = ResolveCharacterRoot(selectedObject);
        if (root == null)
        {
            EditorUtility.DisplayDialog("Boomtown Character Builder",
                "Select Bill, Ted, or another character root first.", "OK");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(root, "Build Character Prototype");
        DisableGameplayCapsuleRenderer(root);

        Transform visual = FindOrCreateChild(root.transform, VisualName);
        Transform existing = visual.Find(GeneratedRootName);
        if (existing != null) Undo.DestroyObjectImmediate(existing.gameObject);

        Transform generated = new GameObject(GeneratedRootName).transform;
        Undo.RegisterCreatedObjectUndo(generated.gameObject, "Create Character Prototype");
        generated.SetParent(visual, false);
        generated.localPosition = Vector3.zero;
        generated.localRotation = Quaternion.Euler(0f, 180f, 0f);

        var random = new System.Random(seed);
        Appearance a = CreateAppearance(random);
        Materials m = LoadMaterials(root.name, a);

        BuildBody(generated, m, a);
        BuildGear(generated, m, a);
        ScaleAndGround(generated, root.transform, targetHeight);
        FitGameplayCapsules(root, targetHeight);

        Selection.activeGameObject = root;
        EditorUtility.SetDirty(root);
        AssetDatabase.SaveAssets();

        Debug.Log($"[Boomtown] Built {root.name}: seed {seed}, height {targetHeight:0.00} m, ground offset 0.");
    }

    private static Appearance CreateAppearance(System.Random random) => new()
    {
        Skin = Pick(random, SkinColors),
        Shirt = Pick(random, ShirtColors),
        Pants = Pick(random, PantsColors),
        Leather = Pick(random, LeatherColors),
        Hat = Pick(random, HatColors),
        Beard = Pick(random, BeardColors),
        HasBeard = random.NextDouble() > 0.18,
        HasMoustache = random.NextDouble() > 0.32,
        HasBackpack = random.NextDouble() > 0.48,
        HasVest = random.NextDouble() > 0.12,
        HasScarf = random.NextDouble() > 0.48,
        Broad = random.NextDouble() > 0.60
    };

    private static Color Pick(System.Random random, Color[] colors) => colors[random.Next(colors.Length)];

    private static void BuildBody(Transform root, Materials m, Appearance a)
    {
        float width = a.Broad ? 1.06f : 1f;
        Transform pelvis = Pivot(root, "Pelvis", new Vector3(0f, 0.82f, 0f));

        Part(pelvis, "Hips", PrimitiveType.Capsule, new(0f, 0.02f, 0f),
            new(0.38f * width, 0.19f, 0.31f), m.Pants);

        Part(pelvis, "Torso", PrimitiveType.Capsule, new(0f, 0.43f, 0f),
            new(0.48f * width, 0.39f, 0.34f), m.Shirt);

        Part(pelvis, "Shoulders", PrimitiveType.Capsule, new(0f, 0.62f, 0f),
            new(0.57f * width, 0.15f, 0.31f), m.Shirt, new(0f, 0f, 90f));

        if (a.HasVest)
        {
            Part(pelvis, "VestLeft", PrimitiveType.Cube, new(-0.12f, 0.43f, -0.19f),
                new(0.20f * width, 0.43f, 0.045f), m.Leather);
            Part(pelvis, "VestRight", PrimitiveType.Cube, new(0.12f, 0.43f, -0.19f),
                new(0.20f * width, 0.43f, 0.045f), m.Leather);
        }

        Part(pelvis, "Belt", PrimitiveType.Cylinder, new(0f, 0.10f, 0f),
            new(0.25f * width, 0.035f, 0.25f * width), m.Leather);
        Part(pelvis, "Buckle", PrimitiveType.Cube, new(0f, 0.10f, -0.19f),
            new(0.08f, 0.065f, 0.03f), m.Metal);

        BuildLeg(pelvis, "Left", -0.14f * width, m);
        BuildLeg(pelvis, "Right", 0.14f * width, m);
        BuildArm(pelvis, "Left", -0.34f * width, m);
        BuildArm(pelvis, "Right", 0.34f * width, m);
        BuildHead(pelvis, m, a);

        if (a.HasScarf)
            Part(pelvis, "Scarf", PrimitiveType.Cube, new(0f, 0.73f, -0.20f),
                new(0.20f, 0.075f, 0.04f), m.Scarf);
    }

    private static void BuildLeg(Transform pelvis, string side, float x, Materials m)
    {
        Transform hip = Pivot(pelvis, side + "Hip", new(x, -0.12f, 0f));
        Part(hip, side + "Thigh", PrimitiveType.Capsule, new(0f, -0.20f, 0f),
            new(0.135f, 0.23f, 0.135f), m.Pants);

        Transform knee = Pivot(hip, side + "Knee", new(0f, -0.40f, 0f));
        Part(knee, side + "Shin", PrimitiveType.Capsule, new(0f, -0.16f, 0f),
            new(0.115f, 0.19f, 0.115f), m.Pants);

        Part(knee, side + "Boot", PrimitiveType.Cube, new(0f, -0.34f, -0.045f),
            new(0.21f, 0.15f, 0.30f), m.Boot);
        Part(knee, side + "Toe", PrimitiveType.Sphere, new(0f, -0.36f, -0.16f),
            new(0.19f, 0.11f, 0.19f), m.Boot);
    }

    private static void BuildArm(Transform pelvis, string side, float x, Materials m)
    {
        Transform shoulder = Pivot(pelvis, side + "Shoulder", new(x, 0.61f, 0f));
        shoulder.localRotation = Quaternion.Euler(0f, 0f, x < 0f ? -6f : 6f);

        Part(shoulder, side + "UpperArm", PrimitiveType.Capsule, new(0f, -0.16f, 0f),
            new(0.105f, 0.19f, 0.105f), m.Shirt);

        Transform elbow = Pivot(shoulder, side + "Elbow", new(0f, -0.32f, 0f));
        Part(elbow, side + "Forearm", PrimitiveType.Capsule, new(0f, -0.14f, 0f),
            new(0.09f, 0.16f, 0.09f), m.Skin);
        Part(elbow, side + "Hand", PrimitiveType.Sphere, new(0f, -0.29f, 0f),
            new(0.125f, 0.14f, 0.11f), m.Skin);
    }

    private static void BuildHead(Transform pelvis, Materials m, Appearance a)
    {
        Transform neck = Pivot(pelvis, "Neck", new(0f, 0.79f, 0f));
        Part(neck, "NeckMesh", PrimitiveType.Cylinder, new(0f, 0.035f, 0f),
            new(0.095f, 0.08f, 0.095f), m.Skin);

        Transform head = Pivot(neck, "Head", new(0f, 0.21f, 0f));
        Part(head, "HeadMesh", PrimitiveType.Sphere, Vector3.zero,
            new(0.30f, 0.34f, 0.29f), m.Skin);

        Part(head, "Nose", PrimitiveType.Sphere, new(0f, -0.01f, -0.155f),
            new(0.065f, 0.08f, 0.09f), m.Skin);

        Part(head, "EyeL", PrimitiveType.Sphere, new(-0.065f, 0.05f, -0.145f),
            new(0.027f, 0.027f, 0.018f), m.Eye);
        Part(head, "EyeR", PrimitiveType.Sphere, new(0.065f, 0.05f, -0.145f),
            new(0.027f, 0.027f, 0.018f), m.Eye);

        if (a.HasMoustache)
            Part(head, "Moustache", PrimitiveType.Capsule, new(0f, -0.065f, -0.15f),
                new(0.115f, 0.032f, 0.032f), m.Beard, new(0f, 0f, 90f));

        if (a.HasBeard)
            Part(head, "Beard", PrimitiveType.Sphere, new(0f, -0.13f, -0.10f),
                new(0.23f, 0.18f, 0.14f), m.Beard);

        Part(head, "HatBrim", PrimitiveType.Cylinder, new(0f, 0.205f, 0f),
            new(0.29f, 0.018f, 0.29f), m.Hat);
        Part(head, "HatCrown", PrimitiveType.Cylinder, new(0f, 0.285f, 0f),
            new(0.17f, 0.085f, 0.17f), m.Hat);
        Part(head, "HatBand", PrimitiveType.Cylinder, new(0f, 0.22f, 0f),
            new(0.18f, 0.022f, 0.18f), m.Leather);
    }

    private static void BuildGear(Transform root, Materials m, Appearance a)
    {
        if (!a.HasBackpack) return;

        Part(root, "Backpack", PrimitiveType.Cube, new(0f, 1.13f, 0.22f),
            new(0.32f, 0.35f, 0.14f), m.Leather);
        Part(root, "Bedroll", PrimitiveType.Cylinder, new(0f, 1.36f, 0.23f),
            new(0.12f, 0.19f, 0.12f), m.Shirt, new(0f, 0f, 90f));
    }

    private static void ScaleAndGround(Transform generated, Transform root, float height)
    {
        Renderer[] renderers = generated.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

        generated.localScale = Vector3.one * (height / bounds.size.y);

        renderers = generated.GetComponentsInChildren<Renderer>();
        float lowest = float.PositiveInfinity;
        foreach (Renderer renderer in renderers) lowest = Mathf.Min(lowest, renderer.bounds.min.y);
        generated.position += Vector3.up * (root.position.y - lowest);
    }

    private static void FitGameplayCapsules(GameObject root, float height)
    {
        float radius = Mathf.Clamp(height * 0.16f, 0.25f, 0.33f);
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
            controller.stepOffset = Mathf.Min(0.30f, height * 0.18f);
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

    private static Transform FindOrCreateChild(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null) return existing;

        Transform child = new GameObject(name).transform;
        Undo.RegisterCreatedObjectUndo(child.gameObject, $"Create {name}");
        child.SetParent(parent, false);
        return child;
    }

    private static Transform Pivot(Transform parent, string name, Vector3 position)
    {
        Transform pivot = new GameObject(name).transform;
        pivot.SetParent(parent, false);
        pivot.localPosition = position;
        return pivot;
    }

    private static GameObject Part(Transform parent, string name, PrimitiveType type,
        Vector3 position, Vector3 scale, Material material, Vector3? rotation = null)
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

    private static Materials LoadMaterials(string characterName, Appearance a)
    {
        EnsureFolderExists(MaterialFolder);
        string safe = Sanitize(characterName);

        return new Materials
        {
            Skin = Material($"Proto_{safe}_Skin", a.Skin),
            Shirt = Material($"Proto_{safe}_Shirt", a.Shirt),
            Pants = Material($"Proto_{safe}_Pants", a.Pants),
            Boot = Material($"Proto_{safe}_Boot", new(0.09f, 0.065f, 0.045f)),
            Leather = Material($"Proto_{safe}_Leather", a.Leather),
            Hat = Material($"Proto_{safe}_Hat", a.Hat),
            Beard = Material($"Proto_{safe}_Beard", a.Beard),
            Metal = Material($"Proto_{safe}_Metal", new(0.30f, 0.32f, 0.33f)),
            Eye = Material($"Proto_{safe}_Eye", new(0.035f, 0.028f, 0.022f)),
            Scarf = Material($"Proto_{safe}_Scarf", new(0.52f, 0.07f, 0.05f))
        };
    }

    private static Material Material(string name, Color color)
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
        public Color Skin, Shirt, Pants, Leather, Hat, Beard;
        public bool HasBeard, HasMoustache, HasBackpack, HasVest, HasScarf, Broad;
    }

    private sealed class Materials
    {
        public Material Skin, Shirt, Pants, Boot, Leather, Hat, Beard, Metal, Eye, Scarf;
    }
}
