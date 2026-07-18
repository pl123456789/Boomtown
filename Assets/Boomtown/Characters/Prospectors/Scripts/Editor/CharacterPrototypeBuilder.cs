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
        CharacterPrototypeBuilderWindow window = GetWindow<CharacterPrototypeBuilderWindow>();
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
        EditorGUILayout.LabelField("Boomtown Character Prototype 2.0", EditorStyles.boldLabel);
        EditorGUILayout.Space(4f);

        characterRoot = (GameObject)EditorGUILayout.ObjectField(
            "Character Root", characterRoot, typeof(GameObject), true);
        appearanceSeed = EditorGUILayout.IntField("Appearance Seed", appearanceSeed);
        targetHeight = EditorGUILayout.Slider("Character Height", targetHeight, 1.60f, 1.95f);

        EditorGUILayout.HelpBox(
            "Builds a connected, stylized frontier character with fitted shoulders, seated hat, " +
            "grounded boots, appearance variation, and gameplay capsule fitting.",
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
        new Color(0.78f, 0.58f, 0.42f), new Color(0.67f, 0.45f, 0.30f),
        new Color(0.54f, 0.34f, 0.22f), new Color(0.40f, 0.25f, 0.17f)
    };

    private static readonly Color[] ShirtColors =
    {
        new Color(0.74f, 0.69f, 0.55f), new Color(0.20f, 0.31f, 0.43f),
        new Color(0.32f, 0.42f, 0.29f), new Color(0.42f, 0.24f, 0.18f),
        new Color(0.39f, 0.39f, 0.36f)
    };

    private static readonly Color[] PantsColors =
    {
        new Color(0.15f, 0.14f, 0.13f), new Color(0.18f, 0.22f, 0.28f),
        new Color(0.24f, 0.20f, 0.16f), new Color(0.25f, 0.26f, 0.25f)
    };

    private static readonly Color[] LeatherColors =
    {
        new Color(0.24f, 0.12f, 0.055f), new Color(0.15f, 0.085f, 0.04f),
        new Color(0.32f, 0.19f, 0.09f), new Color(0.11f, 0.105f, 0.09f)
    };

    private static readonly Color[] HatColors =
    {
        new Color(0.18f, 0.10f, 0.045f), new Color(0.09f, 0.085f, 0.07f),
        new Color(0.27f, 0.25f, 0.21f), new Color(0.24f, 0.16f, 0.08f)
    };

    private static readonly Color[] BeardColors =
    {
        new Color(0.08f, 0.05f, 0.03f), new Color(0.19f, 0.10f, 0.05f),
        new Color(0.31f, 0.23f, 0.16f), new Color(0.17f, 0.17f, 0.16f)
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
            EditorUtility.DisplayDialog(
                "Boomtown Character Builder",
                "Select Bill, Ted, or another character root first.",
                "OK");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(root, "Build Character Prototype");
        DisableGameplayCapsuleRenderer(root);

        Transform visual = FindOrCreateChild(root.transform, VisualName);
        Transform oldCharacter = visual.Find(GeneratedRootName);
        if (oldCharacter != null)
            Undo.DestroyObjectImmediate(oldCharacter.gameObject);

        Transform generated = new GameObject(GeneratedRootName).transform;
        Undo.RegisterCreatedObjectUndo(generated.gameObject, "Create Character Prototype");
        generated.SetParent(visual, false);
        generated.localPosition = Vector3.zero;
        generated.localRotation = Quaternion.Euler(0f, 180f, 0f);
        generated.localScale = Vector3.one;

        System.Random random = new System.Random(seed);
        Appearance appearance = CreateAppearance(random);
        Materials materials = LoadMaterials(root.name, appearance);

        BuildCharacterGeometry(generated, materials, appearance);
        ScaleAndGround(generated, root.transform, targetHeight);
        FitGameplayCapsules(root, targetHeight);

        Selection.activeGameObject = root;
        EditorUtility.SetDirty(root);
        AssetDatabase.SaveAssets();

        Debug.Log($"[Boomtown] Built {root.name} prototype 2.0: seed {seed}, height {targetHeight:0.00} m.");
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
            HasBeard = random.NextDouble() > 0.20,
            HasMoustache = random.NextDouble() > 0.28,
            HasBackpack = random.NextDouble() > 0.50,
            HasVest = random.NextDouble() > 0.12,
            HasScarf = random.NextDouble() > 0.52,
            Broad = random.NextDouble() > 0.60
        };
    }

    private static Color Pick(System.Random random, Color[] colors)
    {
        return colors[random.Next(colors.Length)];
    }

    private static void BuildCharacterGeometry(Transform root, Materials m, Appearance a)
    {
        float width = a.Broad ? 1.06f : 1f;

        BuildTorso(root, m, a, width);
        BuildLeg(root, "Left", -0.145f * width, m);
        BuildLeg(root, "Right", 0.145f * width, m);
        BuildArm(root, "Left", -0.355f * width, m);
        BuildArm(root, "Right", 0.355f * width, m);
        BuildHead(root, m, a);
        BuildGear(root, m, a, width);
    }

    private static void BuildTorso(Transform root, Materials m, Appearance a, float width)
    {
        Part(root, "Pelvis", PrimitiveType.Cube,
            new Vector3(0f, 0.77f, 0f),
            new Vector3(0.43f * width, 0.22f, 0.31f), m.Pants);

        Part(root, "Waist", PrimitiveType.Cube,
            new Vector3(0f, 0.98f, 0f),
            new Vector3(0.44f * width, 0.25f, 0.30f), m.Shirt);

        Part(root, "Chest", PrimitiveType.Cube,
            new Vector3(0f, 1.25f, 0f),
            new Vector3(0.58f * width, 0.34f, 0.34f), m.Shirt);

        Part(root, "ShoulderBar", PrimitiveType.Capsule,
            new Vector3(0f, 1.43f, 0f),
            new Vector3(0.63f * width, 0.12f, 0.27f), m.Shirt,
            new Vector3(0f, 0f, 90f));

        if (a.HasVest)
        {
            Part(root, "VestLeft", PrimitiveType.Cube,
                new Vector3(-0.145f, 1.22f, -0.185f),
                new Vector3(0.24f * width, 0.46f, 0.045f), m.Leather);
            Part(root, "VestRight", PrimitiveType.Cube,
                new Vector3(0.145f, 1.22f, -0.185f),
                new Vector3(0.24f * width, 0.46f, 0.045f), m.Leather);
            Part(root, "VestBottom", PrimitiveType.Cube,
                new Vector3(0f, 1.00f, -0.18f),
                new Vector3(0.48f * width, 0.07f, 0.05f), m.Leather);
        }

        Part(root, "Belt", PrimitiveType.Cube,
            new Vector3(0f, 0.88f, -0.005f),
            new Vector3(0.47f * width, 0.07f, 0.33f), m.Leather);
        Part(root, "Buckle", PrimitiveType.Cube,
            new Vector3(0f, 0.88f, -0.185f),
            new Vector3(0.085f, 0.07f, 0.025f), m.Metal);

        if (a.HasScarf)
        {
            Part(root, "Scarf", PrimitiveType.Cube,
                new Vector3(0f, 1.48f, -0.17f),
                new Vector3(0.22f, 0.08f, 0.04f), m.Scarf);
        }
    }

    private static void BuildLeg(Transform root, string side, float x, Materials m)
    {
        Transform hip = Pivot(root, side + "Hip", new Vector3(x, 0.72f, 0f));

        Part(hip, side + "Thigh", PrimitiveType.Capsule,
            new Vector3(0f, -0.19f, 0f),
            new Vector3(0.14f, 0.23f, 0.14f), m.Pants);

        Part(hip, side + "KneeCap", PrimitiveType.Sphere,
            new Vector3(0f, -0.39f, -0.005f),
            new Vector3(0.14f, 0.12f, 0.14f), m.Pants);

        Transform knee = Pivot(hip, side + "Knee", new Vector3(0f, -0.39f, 0f));
        Part(knee, side + "Shin", PrimitiveType.Capsule,
            new Vector3(0f, -0.17f, 0f),
            new Vector3(0.115f, 0.20f, 0.115f), m.Pants);

        Part(knee, side + "BootShaft", PrimitiveType.Cube,
            new Vector3(0f, -0.31f, 0f),
            new Vector3(0.16f, 0.18f, 0.18f), m.Boot);
        Part(knee, side + "BootFoot", PrimitiveType.Cube,
            new Vector3(0f, -0.40f, -0.085f),
            new Vector3(0.20f, 0.12f, 0.31f), m.Boot);
        Part(knee, side + "BootToe", PrimitiveType.Sphere,
            new Vector3(0f, -0.405f, -0.225f),
            new Vector3(0.18f, 0.105f, 0.17f), m.Boot);
    }

    private static void BuildArm(Transform root, string side, float x, Materials m)
    {
        float sign = x < 0f ? -1f : 1f;
        Transform shoulder = Pivot(root, side + "Shoulder", new Vector3(x, 1.42f, 0f));
        shoulder.localRotation = Quaternion.Euler(0f, 0f, sign * 4f);

        Part(shoulder, side + "ShoulderCap", PrimitiveType.Sphere,
            Vector3.zero,
            new Vector3(0.18f, 0.18f, 0.18f), m.Shirt);

        Part(shoulder, side + "Sleeve", PrimitiveType.Capsule,
            new Vector3(sign * 0.005f, -0.18f, 0f),
            new Vector3(0.13f, 0.21f, 0.13f), m.Shirt);

        Part(shoulder, side + "ElbowCap", PrimitiveType.Sphere,
            new Vector3(0f, -0.36f, 0f),
            new Vector3(0.12f, 0.11f, 0.12f), m.Skin);

        Transform elbow = Pivot(shoulder, side + "Elbow", new Vector3(0f, -0.36f, 0f));
        Part(elbow, side + "Forearm", PrimitiveType.Capsule,
            new Vector3(0f, -0.15f, 0f),
            new Vector3(0.10f, 0.18f, 0.10f), m.Skin);
        Part(elbow, side + "Hand", PrimitiveType.Sphere,
            new Vector3(0f, -0.31f, -0.01f),
            new Vector3(0.12f, 0.14f, 0.105f), m.Skin);
    }

    private static void BuildHead(Transform root, Materials m, Appearance a)
    {
        Part(root, "Neck", PrimitiveType.Cylinder,
            new Vector3(0f, 1.55f, 0f),
            new Vector3(0.10f, 0.10f, 0.10f), m.Skin);

        Transform head = Pivot(root, "Head", new Vector3(0f, 1.72f, 0f));
        Part(head, "HeadMesh", PrimitiveType.Sphere,
            Vector3.zero,
            new Vector3(0.28f, 0.31f, 0.27f), m.Skin);

        Part(head, "EarLeft", PrimitiveType.Sphere,
            new Vector3(-0.145f, 0f, 0f),
            new Vector3(0.055f, 0.075f, 0.05f), m.Skin);
        Part(head, "EarRight", PrimitiveType.Sphere,
            new Vector3(0.145f, 0f, 0f),
            new Vector3(0.055f, 0.075f, 0.05f), m.Skin);

        Part(head, "Nose", PrimitiveType.Sphere,
            new Vector3(0f, -0.01f, -0.145f),
            new Vector3(0.06f, 0.075f, 0.085f), m.Skin);
        Part(head, "EyeLeft", PrimitiveType.Sphere,
            new Vector3(-0.075f, 0.055f, -0.142f),
            new Vector3(0.025f, 0.025f, 0.018f), m.Eye);
        Part(head, "EyeRight", PrimitiveType.Sphere,
            new Vector3(0.075f, 0.055f, -0.142f),
            new Vector3(0.025f, 0.025f, 0.018f), m.Eye);

        if (a.HasMoustache)
        {
            Part(head, "MoustacheLeft", PrimitiveType.Capsule,
                new Vector3(-0.055f, -0.075f, -0.145f),
                new Vector3(0.075f, 0.025f, 0.025f), m.Beard,
                new Vector3(0f, 0f, 78f));
            Part(head, "MoustacheRight", PrimitiveType.Capsule,
                new Vector3(0.055f, -0.075f, -0.145f),
                new Vector3(0.075f, 0.025f, 0.025f), m.Beard,
                new Vector3(0f, 0f, 102f));
        }

        if (a.HasBeard)
        {
            Part(head, "BeardJaw", PrimitiveType.Cube,
                new Vector3(0f, -0.13f, -0.105f),
                new Vector3(0.22f, 0.18f, 0.11f), m.Beard);
            Part(head, "BeardChin", PrimitiveType.Sphere,
                new Vector3(0f, -0.22f, -0.08f),
                new Vector3(0.18f, 0.13f, 0.11f), m.Beard);
        }

        // Brim sits directly on the crown of the head: no floating hat gap.
        Part(head, "HatBrim", PrimitiveType.Cylinder,
            new Vector3(0f, 0.285f, 0f),
            new Vector3(0.34f, 0.018f, 0.34f), m.Hat);
        Part(head, "HatCrown", PrimitiveType.Cylinder,
            new Vector3(0f, 0.395f, 0f),
            new Vector3(0.205f, 0.11f, 0.205f), m.Hat);
        Part(head, "HatBand", PrimitiveType.Cylinder,
            new Vector3(0f, 0.305f, 0f),
            new Vector3(0.215f, 0.025f, 0.215f), m.Leather);
    }

    private static void BuildGear(Transform root, Materials m, Appearance a, float width)
    {
        if (!a.HasBackpack) return;

        Part(root, "Backpack", PrimitiveType.Cube,
            new Vector3(0f, 1.18f, 0.24f),
            new Vector3(0.38f * width, 0.40f, 0.16f), m.Leather);
        Part(root, "Bedroll", PrimitiveType.Cylinder,
            new Vector3(0f, 1.45f, 0.25f),
            new Vector3(0.13f, 0.22f, 0.13f), m.Shirt,
            new Vector3(0f, 0f, 90f));
        Part(root, "LeftStrap", PrimitiveType.Cube,
            new Vector3(-0.19f * width, 1.22f, -0.175f),
            new Vector3(0.035f, 0.45f, 0.025f), m.Leather);
        Part(root, "RightStrap", PrimitiveType.Cube,
            new Vector3(0.19f * width, 1.22f, -0.175f),
            new Vector3(0.035f, 0.45f, 0.025f), m.Leather);
    }

    private static void ScaleAndGround(Transform generated, Transform root, float targetHeight)
    {
        Renderer[] renderers = generated.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        if (bounds.size.y > 0.001f)
            generated.localScale = Vector3.one * (targetHeight / bounds.size.y);

        renderers = generated.GetComponentsInChildren<Renderer>();
        float lowest = float.PositiveInfinity;
        foreach (Renderer renderer in renderers)
            lowest = Mathf.Min(lowest, renderer.bounds.min.y);

        generated.position += Vector3.up * (root.position.y - lowest);
    }

    private static void FitGameplayCapsules(GameObject root, float height)
    {
        float radius = Mathf.Clamp(height * 0.16f, 0.25f, 0.34f);
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
            controller.stepOffset = Mathf.Min(controller.stepOffset, height * 0.22f);
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

        GameObject child = new GameObject(childName);
        Undo.RegisterCreatedObjectUndo(child, "Create " + childName);
        child.transform.SetParent(parent, false);
        return child.transform;
    }

    private static Transform Pivot(Transform parent, string name, Vector3 localPosition)
    {
        GameObject pivot = new GameObject(name);
        pivot.transform.SetParent(parent, false);
        pivot.transform.localPosition = localPosition;
        pivot.transform.localRotation = Quaternion.identity;
        pivot.transform.localScale = Vector3.one;
        return pivot.transform;
    }

    private static GameObject Part(
        Transform parent,
        string name,
        PrimitiveType type,
        Vector3 localPosition,
        Vector3 localScale,
        Material material,
        Vector3? localEuler = null)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localRotation = Quaternion.Euler(localEuler ?? Vector3.zero);
        part.transform.localScale = localScale;

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
            Skin = LoadOrCreateMaterial("Proto_" + safeName + "_Skin", appearance.Skin),
            Shirt = LoadOrCreateMaterial("Proto_" + safeName + "_Shirt", appearance.Shirt),
            Pants = LoadOrCreateMaterial("Proto_" + safeName + "_Pants", appearance.Pants),
            Boot = LoadOrCreateMaterial("Proto_" + safeName + "_Boot", new Color(0.09f, 0.065f, 0.045f)),
            Leather = LoadOrCreateMaterial("Proto_" + safeName + "_Leather", appearance.Leather),
            Hat = LoadOrCreateMaterial("Proto_" + safeName + "_Hat", appearance.Hat),
            Beard = LoadOrCreateMaterial("Proto_" + safeName + "_Beard", appearance.Beard),
            Metal = LoadOrCreateMaterial("Proto_" + safeName + "_Metal", new Color(0.30f, 0.32f, 0.33f)),
            Eye = LoadOrCreateMaterial("Proto_" + safeName + "_Eye", new Color(0.045f, 0.04f, 0.035f)),
            Scarf = LoadOrCreateMaterial("Proto_" + safeName + "_Scarf", new Color(0.48f, 0.08f, 0.06f))
        };
    }

    private static Material LoadOrCreateMaterial(string name, Color color)
    {
        string path = MaterialFolder + "/" + name + ".mat";
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
            string next = current + "/" + parts[i];
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
        public bool HasScarf;
        public bool Broad;
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
